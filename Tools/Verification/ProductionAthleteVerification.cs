using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public static class ProductionAthleteVerification
{
    const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
    const string Single = ProductionAthleteSetup.Root + "/TemporarySingleAthlete.unity";
    const string Logs = "Logs/ProductionAthletes/";
    static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();

    public static string PrepareSingle()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Prepare the fixture in saved Edit Mode.");
        EditorSceneManager.OpenScene(Arena);
        var scene = EditorSceneManager.GetActiveScene();
        var identity = Object.FindObjectsByType<SwimmerIdentity>().Single(p => p.Team == PrototypeTeam.Blue && p.CapNumber == 1);
        ProductionAthleteSetup.Attach(identity.GetComponentInChildren<RiggedSwimmerVisual>());
        if (!EditorSceneManager.SaveScene(scene, Single, true))
            throw new IOException("Could not save isolated arena fixture.");
        EditorSceneManager.OpenScene(Single);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Single);
        return Single;
    }

    public static string RemoveSingle()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Exit the fixture without changes first.");
        EditorSceneManager.OpenScene(Arena);
        Milestone12HubSetup.SetBootEntry();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Single) != null) AssetDatabase.DeleteAsset(Single);
        return AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
    }

    public static string Structure()
    {
        var rigged = Object.FindObjectsByType<RiggedSwimmerVisual>();
        Check(rigged.Length == 14 && Object.FindObjectsByType<AthleteEquipment>().Length == 14, "Fourteen equipped rigs.");
        var covered = new List<SkinnedMeshRenderer>();
        var players = rigged.Select(v => v.identity).ToArray();
        foreach (var team in new[] { PrototypeTeam.Blue, PrototypeTeam.White })
        {
            var side = players.Where(p => p.Team == team).ToArray();
            Check(side.Length == 7 && side.Count(p => !p.IsFieldPlayer) == 1 &&
                side.Select(p => p.CapNumber).Distinct().Count() == 7, "Team identities and unique cap numbers.");
        }
        foreach (var visual in rigged)
        {
            var animator = visual.GetComponent<Animator>();
            Check(animator.avatar.isHuman && animator.avatar.isValid && !animator.applyRootMotion &&
                visual.GetComponentsInChildren<Animator>().Length == 1, "Single existing in-place Humanoid Animator.");
            Check(visual.GetComponentsInChildren<Collider>(true).Length == 0 &&
                visual.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Visual rig owns no physics.");
            var live = visual.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled).ToArray();
            Check(live.Length == 4 && live.All(r => r.sharedMesh != null && r.rootBone != null &&
                r.bones.All(b => b != null) && r.sharedMaterials.All(m => m != null)), "Four complete worn meshes.");
            Check(visual.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r => !r.enabled) == 4, "Four mannequin renderers recoverable.");
            covered.AddRange(live);
        }
        var scene = EditorSceneManager.GetActiveScene();
        Check(scene.path == Arena && scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "Arena has no missing scripts.");
        var materials = covered.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
        var textures = materials.SelectMany(m => m.GetTexturePropertyNames().Select(p => m.GetTexture(p))).Where(t => t != null).Distinct().ToArray();
        return Save("structure", new {
            scene = AssetDatabase.AssetPathToGUID(Arena), roster = players.Select(p => new {team=p.Team.ToString(),p.CapNumber,p.IsFieldPlayer}),
            enabledRenderers=covered.Count, uniqueMaterials=materials.Length, triangles=covered.Sum(r=>Triangles(r.sharedMesh)),
            uniqueMeshes=covered.Select(r=>r.sharedMesh).Distinct().Select(m=>new {m.name,m.vertexCount,triangles=Triangles(m)}),
            editorTextures=textures.Select(t=>new {t.name,t.width,t.height,runtimeBytes=Profiler.GetRuntimeMemorySizeLong(t)}),
            buildScenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path) });
    }

    public static async Task<string> RenderComparison()
    {
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        if (intro.Active) intro.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Live broadcast camera.");
        var meshes=Object.FindObjectsByType<RiggedSwimmerVisual>().SelectMany(v=>v.GetComponentsInChildren<SkinnedMeshRenderer>()).ToArray();
        bool[] enabled=meshes.Select(r=>r.enabled).ToArray();
        match.SetPaused(true);
        try
        {
            await Shot("27-Roster-Broadcast");
            var production=await SampleRendering();
            for (int i=0;i<meshes.Length;i++) meshes[i].enabled=!enabled[i];
            var mannequin=await SampleRendering();
            return Save("render-comparison", new {mode="Parked arena Game View, actors and camera held", width=Screen.width,height=Screen.height,
                SystemInfo.graphicsDeviceName,SystemInfo.graphicsDeviceType,SystemInfo.processorType, production, mannequin});
        }
        finally
        {
            for (int i=0;i<meshes.Length;i++) meshes[i].enabled=enabled[i];
            match.SetPaused(false);
        }
    }

    static async Task<Dictionary<string,double>> SampleRendering()
    {
        var fields = new[] {"batches","drawCalls","triangles","vertices","shadowCasters","mainThreadFrameTime","renderThreadFrameTime"};
        var values=fields.Select(x=>typeof(UnityStats).GetProperty(x)).Where(x=>x!=null)
            .ToDictionary(x=>x,new Func<System.Reflection.PropertyInfo,List<double>>(_=>new()));
        int frame=Time.frameCount;
        await Until(()=>Time.frameCount>=frame+10,"Warm up rendering samples.");
        for (int i=0;i<30;i++)
        {
            frame=Time.frameCount;
            await Until(()=>Time.frameCount>frame,"Render next measurement frame.");
            foreach (var pair in values) pair.Value.Add(Convert.ToDouble(pair.Key.GetValue(null)));
        }
        return values.ToDictionary(pair=>pair.Key.Name,pair=>Math.Round(pair.Value.Average(),3));
    }

    static int Triangles(Mesh mesh) => Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(int)mesh.GetIndexCount(i)/3);

    public static async Task<string> Replacement()
    {
        var load = EditorSceneManager.LoadSceneAsyncInPlayMode(Arena, new UnityEngine.SceneManagement.LoadSceneParameters(
            UnityEngine.SceneManagement.LoadSceneMode.Single));
        await Until(() => load.isDone && One<PreMatchPresentation>() != null && One<PreMatchPresentation>().Active,
            "Reload a fresh direct arena for exclusions.");
        var match = One<PrototypeMatch>();
        var player = One<PrototypeRoster>().FieldPlayers.First(p=>p.Team == PrototypeTeam.White);
        int before = player.CapNumber;
        Check(EditorApplication.ExecuteMenuItem("Tools/Water Polo/Verification/Exclusion Test/White - Third Personal and Replacement"),
            "Started the existing exclusion scenario.");
        await Until(()=>player.EligibleForPlay && player.LastDisqualifiedCap == before && player.CapNumber != before,
            "A replacement officially re-entered after the third personal foul.", 42);
        await Shot("Exclusion-Replacement");
        Check(match.Rules.ActiveFields(PrototypeTeam.White) == 6, "The replacement fills one original field slot.");
        await KitRuntime();
        return Save("replacement", new {outgoing=before,incoming=player.CapNumber, activeFields=6, printedCapVerified=true});
    }

    public static async Task<string> SwitchAndClock()
    {
        var match = One<PrototypeMatch>();
        match.RestartMatch();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Fresh physical opening possession.", 15);
        var selection=One<PlayerSelection>();
        var ball=One<BallController>();
        var player=selection.ActivePlayer;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        var focus=InputSystem.settings.backgroundBehavior;
        var routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        try
        {
            ball.ResetForRestart(new Vector3(8,0,8));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));
            await Until(()=>selection.ActivePlayer!=player,"Contextual C changes the selected player.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            var next=selection.ActivePlayer;
            Check(next.Team==player.Team && next.IsFieldPlayer && next.EligibleForPlay,"Switch follows existing team eligibility.");
            next.Motor.ResetPose(Vector3.zero,Quaternion.identity);
            ball.ResetForRestart(next.Motor.Position);
            Check(next.Interaction.TryCollect(),"Physical possession starts the Blue shot clock.");
            float remaining=match.ShotClock.LiveSecondsRemaining;
            Check(remaining>0f && remaining<=28f,"Legal independent possession clock.");
            match.AdvanceLiveTime(remaining+.01f);
            Check(match.Phase==MatchPhase.TurnoverRestart,"Expired shot clock enters the authoritative turnover.");
            await Until(()=>match.IsPlaying && match.ShotClock.PossessionTeam==PrototypeTeam.White,"White possession after turnover.");
            return Save("switch-and-clock", new {before=player.CapNumber,after=next.CapNumber,remaining,turnover="White"});
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=focus;
            InputSystem.settings.editorInputBehaviorInPlayMode=routing;
        }
    }

    public static async Task<string> KitRuntime()
    {
        var kits = One<PrototypeRoster>().Members.Select(player => player.GetComponentInChildren<AthleteEquipment>()).ToArray();
        Check(kits.Length == 14 && kits.All(kit=>kit != null), "Fourteen kits on authoritative match slots.");
        var numberId = Shader.PropertyToID("_BaseMap_ST");
        var block = new MaterialPropertyBlock();
        int Printed(AthleteEquipment kit)
        {
            kit.NumberRenderer.GetPropertyBlock(block);
            var uv = block.GetVector(numberId);
            Check(Mathf.Abs(uv.x-.125f)<.001f && Mathf.Abs(uv.y-.125f)<.001f, "Shared atlas mapping");
            return Mathf.RoundToInt(uv.z*8) + 8*(7-Mathf.RoundToInt(uv.w*8));
        }
        foreach (var kit in kits)
        {
            var visual = kit.GetComponent<RiggedSwimmerVisual>();
            var id = visual.identity;
            Check(Printed(kit)==id.CapNumber,"Printed cap uses the match number.");
            var cap=kit.transform.Find("WaterPoloCap").GetComponent<Renderer>();
            string expected = visual.useAuthoredWaterline
                ? (id.IsFieldPlayer ? id.Team + " Cap" : "Red Keeper Cap")
                : (id.IsFieldPlayer ? id.Team + " Cap Fabric" : "Keeper Red Fabric");
            Check(cap.sharedMaterial.name==expected,"Cap shell material is the field or red keeper color.");
        }

        var source=kits[0];
        var parent=new GameObject("Equipment recovery fixture");
        parent.SetActive(false);
        try
        {
            var clone=Object.Instantiate(source.gameObject,parent.transform);
            var kit=clone.GetComponent<AthleteEquipment>();
            clone.GetComponent<RiggedSwimmerVisual>().enabled=false;
            var editor=new SerializedObject(kit);
            editor.FindProperty("skin").objectReferenceValue=null;
            editor.ApplyModifiedPropertiesWithoutUndo();
            parent.SetActive(true);
            int start=Time.frameCount;
            await Until(()=>Time.frameCount>start+2,"Fallback activated in Play mode.");
            Check(!clone.transform.Find("AthleteBody").GetComponent<Renderer>().enabled &&
                clone.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r=>r.enabled)==4,
                "Missing authored material uses clothed mannequin fallback.");
            return Save("kit-runtime",new {verifiedNumberCount=kits.Length,roleColors=true,missingMaterialFallback=true});
        }
        finally { Object.Destroy(parent); }
    }

    public static async Task<string> SingleLive()
    {
        Check(Application.isPlaying && EditorSceneManager.GetActiveScene().path == Single, "Play the isolated single-athlete arena.");
        var kits = Object.FindObjectsByType<AthleteEquipment>();
        Check(kits.Length == 1, "Exactly one replacement on the original roster.");
        var visual = kits[0].GetComponent<RiggedSwimmerVisual>();
        var player = visual.identity;
        var match = One<PrototypeMatch>();
        var ball = One<BallController>();
        var roster = One<PrototypeRoster>();
        var selection = One<PlayerSelection>();
        var intro = One<PreMatchPresentation>();
        var ai = Object.FindObjectsByType<PrototypeSwimmerAI>();
        bool[] previous = ai.Select(a => a.enabled).ToArray();
        Check(roster.Members.Count == 14 && visual.Animator.avatar.isHuman && visual.Animator.avatar.isValid, "Original roster and valid Humanoid.");
        Check(visual.GetComponentsInChildren<Collider>(true).Length == 0 && !visual.Animator.applyRootMotion, "Visual contains no collider and runs in place.");
        var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>().Where(mesh => mesh.enabled).ToArray();
        Check(renderers.Length == 4 && renderers.All(r => r.rootBone != null && r.bones.All(b => b != null) && r.sharedMaterials.All(m => m != null)), "Valid renderer, material and bone wiring.");
        var previousFocus = InputSystem.settings.backgroundBehavior;
        var previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            if (intro.Active) intro.SkipAll();
            await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Physical sprint acquisition.", 15);
            Check(match.SprintOff.Acquisitions == 1, "One physical pickup starts the isolated game.");
            foreach (var item in ai) item.enabled = false;
            Arrange(roster, player);
            ball.ResetForRestart(new Vector3(8f, 0, 4f));
            player.Motor.ResetPose(new Vector3(0, 0, -3), Quaternion.identity);
            selection.Select(player);
            Check(selection.ActivePlayer == player, "Existing selection points to the new visual actor.");
            await Until(() => visual.Locomotion < .1f, "Stationary tread after opening.");
            Close(visual);
            await Shot("20-Single-Live-Tread");
            float waterline = One<PoolBounds>().Waterline;
            float head = visual.Animator.GetBoneTransform(HumanBodyBones.Head).position.y;
            Check(Mathf.Abs(head-waterline-visual.headHeight) < .025f, "Head remains at the original waterline offset.");

            Vector3 start = player.Motor.Position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await Until(() => visual.Locomotion > .72f && Vector3.Distance(start, player.Motor.Position) > .80f, "Keyboard and motor drive swimming.");
            Close(visual);
            await Shot("21-Single-Live-Swim");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            await Until(() => visual.Locomotion > 1.38f && player.GetComponent<SwimmerStamina>().IsSprinting, "Keyboard sprint reaches the sprint blend.");
            Close(visual);
            await Shot("22-Single-Live-Sprint");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Until(() => player.Motor.Velocity.magnitude < .3f, "Stop after releasing movement.");

            var receiver = roster.Members.First(p => p != player && p.Team == player.Team && p.IsFieldPlayer);
            player.Motor.ResetPose(new Vector3(0, 0, -3), Quaternion.identity);
            receiver.Motor.ResetPose(new Vector3(0, 0, 2), Quaternion.identity);
            ball.ResetForRestart(player.Motor.Position);
            Check(player.Interaction.TryCollect(), "Original collection succeeds.");
            await Until(() => visual.Animator.GetBool("Hold") && visual.Animator.GetLayerWeight(1) > .9f, "Real possession blends into the hold pose.");
            float handGap = Vector3.Distance(visual.RightHand.position, ball.transform.position);
            Check(handGap < .26f, "Held ball is near visible wrist: " + handGap);
            Close(visual);
            await Shot("23-Single-Live-Hold");

            int releases = visual.ReleaseAnimations;
            Check(player.GetComponent<SwimmerPassing>().TryPassTo(receiver), "Issued physical dry pass.");
            await Until(() => ball.Owner == receiver.Interaction, "Receiver completes the physical catch.");
            Check(visual.ReleaseAnimations == releases+1, "One visual event per real pass.");

            var goal = Object.FindObjectsByType<PrototypeTeamTactics>().Single(t => t.Team == player.Team).AttackingGoal;
            var keeper = Object.FindObjectsByType<PrototypeGoalkeeper>().Single(k => k.DefendedGoal == goal);
            keeper.enabled = false;
            keeper.Identity.Motor.ResetPose(goal.transform.position + Vector3.right*2.6f, Quaternion.identity);
            player.Motor.ResetPose(goal.transform.position-goal.transform.forward*5f, Quaternion.LookRotation(goal.transform.forward));
            selection.Select(player);
            ball.ResetForRestart(player.Motor.Position);
            Check(player.Interaction.TryCollect() && player.Interaction.TryShoot(22, ShotType.Normal), "Issued authoritative shot.");
            Close(visual);
            await Until(() => visual.Animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot") || match.BlueScore == 1, "Shoot motion starts.");
            await Shot("24-Single-Live-Shot");
            await Until(() => match.BlueScore == 1, "Registered exactly one Blue goal.");
            Check(visual.ReleaseAnimations == releases+2, "One animation event per shot.");
            One<ArenaDeveloperCamera>().SetView(-1);
            var replay = One<GoalReplayDirector>();
            await Until(() => replay.Phase == GoalPresentationPhase.Broadcast, "Recorded play reaches the broadcast replay.");
            await Shot("25-Single-Replay");
            await Until(() => replay.Phase == GoalPresentationPhase.BehindGoal, "Recorded play reaches the goal-line replay.");
            await Shot("26-Single-Replay-Goal-Line");
            return Save("single-live", new { roster=roster.Members.Count, player=player.CapNumber, handGap,
                body=renderers.Select(r=>r.name).ToArray(), waterline, head, sprint=true, physicalPass=true,
                physicalGoal=true, match.BlueScore, replay.Phase, replay.RecordingBytes });
        }
        finally
        {
            for (int i=0; i<ai.Length; i++) ai[i].enabled=previous[i];
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=previousFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=previousRouting;
        }
    }

    static void Arrange(PrototypeRoster roster, SwimmerIdentity chosen)
    {
        int blue=0, white=0;
        foreach (var player in roster.Members.Where(p=>p.IsFieldPlayer && p!=chosen))
        {
            bool side=player.Team==PrototypeTeam.Blue;
            player.Motor.ResetPose(new Vector3(side ? -7f : 7f, 0, -9f+3.2f*(side ? blue++ : white++)),Quaternion.identity);
        }
    }

    static void Close(RiggedSwimmerVisual visual)
    {
        var dev=One<ArenaDeveloperCamera>();
        Transform view=dev.viewpoints[2];
        Vector3 focus=visual.motor.Position + Vector3.up*.05f;
        view.position = focus + visual.transform.right*1.75f + visual.transform.forward*2.9f + Vector3.up*1.25f;
        view.LookAt(focus);
        dev.SetView(2);
    }

    static async Task Until(Func<bool> predicate, string message, int seconds=9)
    {
        double limit=EditorApplication.timeSinceStartup+seconds;
        while (!predicate())
        {
            Check(EditorApplication.timeSinceStartup < limit,message);
            await Task.Delay(15);
        }
    }

    static async Task Shot(string name)
    {
        int frame=Time.frameCount;
        await Until(()=>Time.frameCount>frame+2,"Camera view reflects the staged pose.");
        frame=Time.frameCount;
        ScreenCapture.CaptureScreenshot(Logs+name+".png");
        await Until(()=>Time.frameCount>frame+3,"Advancing Game View.");
    }

    static void Check(bool expression,string message)
    {
        if (!expression) throw new InvalidOperationException(message);
    }

    static string Save(string name,object report)
    {
        string path=Logs+name+".json";
        File.WriteAllText(path,JsonConvert.SerializeObject(report,Formatting.Indented));
        return path;
    }
}

