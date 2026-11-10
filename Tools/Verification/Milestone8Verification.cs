using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class Milestone8Verification
{
    public static object Setup()
    {
        Require(!Application.isPlaying,"Setup verification requires Edit Mode.");
        ToduStudios.WaterPolo.Editor.Milestone8PrototypeSetup.SetUp();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        int before=scene.GetRootGameObjects().Sum(go=>go.GetComponentsInChildren<Transform>(true).Length);
        var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();var tuning=new SerializedObject(camera);float original=tuning.FindProperty("distance").floatValue;
        tuning.FindProperty("distance").floatValue=original+0.37f;tuning.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        try
        {
            ToduStudios.WaterPolo.Editor.Milestone8PrototypeSetup.SetUp();
            Require(scene.GetRootGameObjects().Sum(go=>go.GetComponentsInChildren<Transform>(true).Length)==before,"Repeat setup creates no duplicate GameObjects.");
            Require(Mathf.Approximately(new SerializedObject(camera).FindProperty("distance").floatValue,original+0.37f),"Repeat setup preserves camera tuning through YAML float serialization.");
            var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            Require(roster.Members.Count==6 && roster.FieldPlayers.Count==4,"Four field swimmers plus two keepers remain.");
            Require(UnityEngine.Object.FindObjectsByType<ShotPowerBar>().Length==1 && roster.Members.Count(p=>p.GetComponent<ShotCharge>()!=null)==3,"Exactly one charge HUD and one charge per Blue athlete.");
            Require(roster.Members.Where(p=>!p.IsFieldPlayer).All(p=>p.GetComponents<PrototypeGoalkeeper>().Length==1 && p.GetComponents<GoalkeeperDistribution>().Length==1),"No duplicate keeper behavior.");
            Require(roster.Members.Where(p=>p.Team==PrototypeTeam.Blue).All(p=>p.GetComponents<SwimmerInput>().Length==1 && p.GetComponents<BallInteractionInput>().Length==1),"Exactly one human movement/action handler per Blue athlete.");
            Require(UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>().All(k=>k.enabled) && UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().All(a=>a.enabled),"Saved AI components remain enabled.");
            Require(UnityEngine.Object.FindObjectsByType<GoalMouth>().All(g=>g.Width==3f && g.Height==0.9f),"Physical goal mouths remain 3m x 0.9m.");
            return new {passed=true,objects=before,athletes=6,fieldPlayers=4,chargeComponents=3,bars=1,preservedCameraTuning=true};
        }
        finally {tuning.Update();tuning.FindProperty("distance").floatValue=original;tuning.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
    }

    public static async Task<object> Presentation()
    {
        Require(Application.isPlaying,"Presentation verification requires Play Mode.");
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while(match.IsRestarting)await Advance(0.05f);
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(c=>c.enabled).ToArray();foreach(var c in ai)c.enabled=false;
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>().Where(c=>c.enabled).ToArray();foreach(var c in keepers)c.enabled=false;
        var field=roster.FieldPlayers[0];var keeper=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);var lens=Camera.main;var camera=lens.GetComponent<GameplayCamera>();
        bool Visible(Vector3 p){var v=lens.WorldToViewportPoint(p);return v.z>0f && v.x>0.02f && v.x<0.98f && v.y>0.02f && v.y<0.98f;}
        try
        {
            roster.ResetPositions();ball.ResetForRestart(keeper.HandPosition);selection.Select(field);await Advance(0.1f);
            Require(keeper.Identity.Interaction.TryCollect(),"Camera fixture acquires keeper possession through range-checked collection.");
            await Advance(1.8f);Require(camera.Target==keeper.Identity.Motor && Visible(keeper.Identity.Motor.Position),"Keeper control retargets camera and keeps keeper visible.");
            Vector3 before=lens.transform.position;ball.ResetForRestart(Vector3.zero);
            Require(selection.ActivePlayer.IsFieldPlayer && Vector3.Distance(before,lens.transform.position)<0.001f,"Release retargets without an immediate camera jump.");
            await Advance(0.06f);Require(Vector3.Distance(before,lens.transform.position)<3f,"Field-return transition remains gradual.");
            field.Motor.ResetPose(new Vector3(0f,0f,8f),Quaternion.identity);selection.Select(field);
            ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,9.65f));Require(field.Interaction.TryCollect(),"Attacking camera fixture collection.");
            await Advance(1.8f);
            var goal=UnityEngine.Object.FindObjectsByType<GoalMouth>().Single(g=>g.ScoringTeam==PrototypeTeam.Blue);
            Require(Visible(field.Motor.Position) && Visible(ball.PhysicsPosition) && Visible(goal.transform.position+Vector3.up*0.45f),"Attacking swimmer, held ball and attacking goal are visible.");
            Vector3 left=lens.WorldToViewportPoint(goal.transform.TransformPoint(new Vector3(-1.5f,0.45f,0f))),right=lens.WorldToViewportPoint(goal.transform.TransformPoint(new Vector3(1.5f,0.45f,0f)));
            float width=Mathf.Abs(right.x-left.x)*Screen.width;Require(width>60f,$"Real 3m goal has readable projected width: {width}px at {Screen.width}x{Screen.height}.");
            var charge=field.GetComponent<ShotCharge>();Require(charge.BeginCharge(),"Begin preview charge.");await Advance(0.4f);
            ScreenCapture.CaptureScreenshot("Logs/Milestone8PowerBar.png");await Advance(0.15f);charge.CancelCharge();
            return new {passed=true,keeperVisible=true,gradualReturn=true,goalWidthPixels=width,resolution=new[]{Screen.width,Screen.height},goalDimensions=new[]{goal.Width,goal.Height}};
        }
        finally
        {
            field.GetComponent<ShotCharge>().CancelCharge();ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f));roster.ResetPositions();selection.Select(field);
            foreach(var c in keepers)c.enabled=true;foreach(var c in ai)c.enabled=true;
        }
    }
    public static async Task<object> ChargedShooting()
    {
        Require(Application.isPlaying,"Run this verification only in Play Mode.");
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while(match.IsRestarting)await Advance(0.05f);
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(c=>c.enabled).ToArray();foreach(var c in ai)c.enabled=false;
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>().Where(c=>c.enabled).ToArray();foreach(var c in keepers)c.enabled=false;
        var player=roster.FieldPlayers[0];var other=roster.FieldPlayers[1];var charge=player.GetComponent<ShotCharge>();
        var hud=UnityEngine.Object.FindAnyObjectByType<ShotPowerBar>();var panel=hud.transform.Find("ShotPowerBar").gameObject;
        var keyboard=InputSystem.AddDevice<Keyboard>();var passed=new List<string>();
        async Task Claim()
        {
            Press(keyboard);ball.ResetForRestart(new Vector3(1.65f,ball.SurfaceHeight,0f));roster.ResetPositions();
            player.Motor.ResetPose(Vector3.zero,Quaternion.Euler(0f,90f,0f));selection.Select(player);await Advance(0.1f);
            Require(player.Interaction.TryCollect(),"Charge fixture uses legitimate collection.");await Advance(0.08f);
        }
        try
        {
            await Claim();int sequence=ball.ShotSequence;
            Press(keyboard,Key.Space);await Advance(0.4f);
            Require(charge.IsCharging && charge.NormalizedCharge>0.4f && charge.NormalizedCharge<0.85f,"Holding Space accumulates charge.");
            Require(ball.Owner==player.Interaction && ball.ShotSequence==sequence && panel.activeSelf,"Holding preserves ownership and displays one HUD bar without firing.");
            await Advance(0.55f);Require(charge.NormalizedCharge==1f,"Charge clamps at maximum.");
            Press(keyboard);await Advance(0.04f);
            Require(ball.Owner==null && ball.ShotSequence==sequence+1 && ball.Velocity.x>20f,"Release fires one full-power facing shot.");
            Require(!charge.IsCharging && !panel.activeSelf,"Release hides/cancels charge immediately.");
            await Advance(0.15f);Require(ball.ShotSequence==sequence+1,"No duplicate shot on a released/idle button.");
            passed.Add("Injected hold/release, capped charge, physical max-speed shot, facing direction, one release and bar visibility");

            await Claim();sequence=ball.ShotSequence;await Tap(keyboard,Key.Space,0.02f);
            Require(ball.Owner==null && ball.ShotSequence==sequence+1 && ball.Velocity.magnitude>=9.5f && ball.Velocity.magnitude<13f,"Quick tap fires usable minimum-power shot.");
            await Claim();Press(keyboard,Key.Space);await Advance(0.2f);
            ball.ResetForRestart(Vector3.zero);await Advance(0.04f);
            Require(!charge.IsCharging && !panel.activeSelf,"Unexpected possession loss cancels charge/bar.");
            sequence=ball.ShotSequence;Press(keyboard);await Advance(0.05f);Require(ball.ShotSequence==sequence,"Releasing after loss cannot shoot.");
            passed.Add("Quick taps and possession-loss cancellation");

            await Claim();Press(keyboard,Key.Space);await Advance(0.12f);selection.Select(other);await Advance(0.06f);
            Require(!charge.IsCharging && !panel.activeSelf && ball.Owner==player.Interaction,"Manual switching cancels charge without transferring possession.");
            selection.Select(player);await Advance(0.12f);Require(!charge.IsCharging,"Held Space cannot replay on reselected carrier.");
            Press(keyboard);await Advance(0.08f);sequence=ball.ShotSequence;
            Require(ball.ShotSequence==sequence && ball.Owner==player.Interaction,"No stale release after handoff.");
            Press(keyboard,Key.Space);await Advance(0.12f);selection.SetInputLocked(true);await Advance(0.04f);
            Require(!charge.IsCharging && !panel.activeSelf,"Stoppage lock cancels charge.");
            Press(keyboard);selection.SetInputLocked(false);await Advance(0.08f);
            passed.Add("Manual handoff, held-key replay prevention and stoppage cancellation");

            await Claim();Require(charge.BeginCharge(),"Touch-ready charge API begins.");
            await Advance(0.15f);sequence=ball.ShotSequence;Require(charge.ReleaseCharge() && !charge.ReleaseCharge(),"Release API is one-shot.");
            Require(ball.ShotSequence==sequence+1,"Shared charge API releases only once.");
            await Claim();Require(player.Interaction.TryShoot(),"Existing direct AI shooting API remains available.");
            Require(ball.Velocity.magnitude>11f && ball.Velocity.magnitude<=12.01f,"Direct shot retains original 12m/s tuning.");
            passed.Add("Touch-ready begin/release/cancel commands and unchanged immediate AI shot API");
            return new {passed};
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);selection.SetInputLocked(false);ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f));roster.ResetPositions();selection.Select(player);
            foreach(var c in keepers)c.enabled=true;foreach(var c in ai)c.enabled=true;
        }
    }
    public static async Task<object> KeeperControl()
    {
        Require(Application.isPlaying,"Run this verification only in Play Mode.");
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while(match.IsRestarting)await Advance(0.05f);
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(c=>c.enabled).ToArray();foreach(var c in ai)c.enabled=false;
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var blue=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);var white=keepers.Single(k=>k.Identity.Team==PrototypeTeam.White);
        var first=roster.FieldPlayers[0];var second=roster.FieldPlayers[1];var keyboard=InputSystem.AddDevice<Keyboard>();
        var passed=new List<string>();
        async Task CatchBlue()
        {
            ball.ResetForRestart(Vector3.zero);roster.ResetPositions();selection.Select(first);
            second.Motor.ResetPose(new Vector3(0f,0f,-9f),Quaternion.identity);
            // Full-team fixtures must isolate the intended outlet from the added centre-back/flats.
            int parked=0;
            foreach(var field in roster.FieldPlayers)
                if(field.Team==PrototypeTeam.Blue && field!=first && field!=second)
                    field.Motor.ResetPose(new Vector3(8f,0f,-10f+4f*parked++),Quaternion.identity);
            await Advance(0.12f);ball.ResetForRestart(blue.HandPosition);
            await Until(()=>ball.Owner==blue.Identity.Interaction,4f);
            Require(selection.ActivePlayer==blue.Identity && Camera.main.GetComponent<GameplayCamera>().Target==blue.Identity.Motor,"Actual catch selects Blue keeper and camera.");
            Require(blue.transform.Find("SelectionRing").gameObject.activeSelf && blue.GetComponent<SwimmerInput>().enabled,"Keeper ring and human input enabled.");
            Require(roster.Members.Count(p=>p.GetComponent<SwimmerInput>()!=null && p.GetComponent<SwimmerInput>().enabled)==1,"Exactly one movement input owner.");
        }
        try
        {
            await CatchBlue();int distributions=blue.GetComponent<GoalkeeperDistribution>().Distributions;
            await Advance(3.3f);
            Require(ball.Owner==blue.Identity.Interaction && blue.GetComponent<GoalkeeperDistribution>().Distributions==distributions,"Human keeper suppresses queued/timeout AI distribution.");
            Vector3 start=blue.Identity.Motor.Position;Press(keyboard,Key.W);await Advance(0.6f);Press(keyboard);await Advance(0.3f);
            Require(blue.Identity.Motor.Position.z>start.z+0.8f && Mathf.Abs(blue.Identity.Motor.Position.y)<0.001f,"Human keeper swims freely at fixed waterline without AI opposition.");
            var carried=blue.Identity.Motor.Position;
            Press(keyboard,Key.Space);await Advance(0.85f);
            Require(blue.GetComponent<ShotCharge>().IsCharging && blue.GetComponent<ShotCharge>().NormalizedCharge==1f && ball.Owner==blue.Identity.Interaction,"Human keeper can fully charge without AI distribution.");
            Press(keyboard);await Advance(0.04f);
            Require(ball.Owner==null && selection.ActivePlayer==first && ball.Velocity.z>0f,"Keeper shot follows facing and restores field control.");
            Require(Vector3.Distance(carried,blue.Identity.Motor.Position)<0.8f,"Release cannot teleport keeper home.");
            await Advance(2f);Require(blue.AtStation,"Keeper swims back to goal station after release.");
            passed.Add("Confirmed catch selection, one input/ring/camera, AI suspension, manual swimming/shooting and swimming return");

            await CatchBlue();Press(keyboard,Key.D);await Advance(0.45f);Press(keyboard);await Advance(0.2f);await Tap(keyboard,Key.Space);
            Require(ball.Owner==null && ball.Velocity.x>8f && Mathf.Abs(ball.Velocity.z)<1f,"Keeper aims/shoots with lateral input, not fixed opposition-goal aiming.");
            passed.Add("Keeper facing-based lateral aim and tap shot");

            await CatchBlue();await Tap(keyboard,Key.B,0.04f);
            Require(ball.Owner==null && selection.ActivePlayer.IsFieldPlayer,"Immediate keeper pass releases physically and restores a field selection.");
            await Until(()=>ball.Owner==second.Interaction,4f);
            Require(selection.ActivePlayer==second && Camera.main.GetComponent<GameplayCamera>().Target==second.Motor,"Keeper pass reception selects actual Blue field receiver.");
            passed.Add("Immediate physical keeper pass, temporary field fallback and receiver acquisition selection");

            await CatchBlue();await Tap(keyboard,Key.C);
            Require(selection.ActivePlayer.IsFieldPlayer && ball.Owner==blue.Identity.Interaction,"C selects a field player without transferring keeper possession.");
            for(int i=0;i<4;i++){selection.SelectNext();Require(selection.ActivePlayer.IsFieldPlayer,"C never cycles back to holding keeper.");}
            ball.ResetForRestart(Vector3.zero);await CatchBlue();
            selection.SetInputLocked(true);ball.ResetForRestart(Vector3.zero);selection.SetInputLocked(false);
            Require(selection.ActivePlayer.IsFieldPlayer && selection.ActivePlayer.GetComponent<SwimmerInput>().enabled,"Locked possession loss cannot leave invalid keeper control after restart unlock.");
            passed.Add("C remains field-only; locked/unexpected loss restores valid control with no stale keeper selection");

            await CatchBlue();var attacker=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);
            blue.Identity.Motor.ResetPose(new Vector3(0f,0f,-8f),Quaternion.identity);
            attacker.Motor.ResetPose(new Vector3(0f,0f,-6.5f),Quaternion.Euler(0f,180f,0f));await Advance(0.12f);
            Require(blue.GetComponent<ShotCharge>().BeginCharge(),"Begin keeper charge before steal.");
            Require(ball.TrySteal(attacker.Interaction,2f,0.2f,0f,0.1f)==StealResult.Success,"Keeper carrying out of protected goal area can lose possession through normal steal rules.");
            await Advance(0.05f);
            Require(selection.ActivePlayer.IsFieldPlayer && !blue.HumanControlled && !blue.GetComponent<ShotCharge>().IsCharging && ball.Owner==attacker.Interaction,"Keeper possession loss restores human field control and cancels charge.");
            passed.Add("Unexpected opposing steal during keeper charge, exclusive ownership and field fallback");

            roster.ResetPositions();ball.ResetForRestart(white.HandPosition);var active=selection.ActivePlayer;
            await Until(()=>ball.Owner==white.Identity.Interaction,4f);
            Require(selection.ActivePlayer==active && !selection.Select(white.Identity),"White catch never transfers human control.");
            int sent=white.GetComponent<GoalkeeperDistribution>().Distributions;
            await Until(()=>white.GetComponent<GoalkeeperDistribution>().Distributions>sent,6f);
            Require(ball.Owner==null || ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White,"White keeper retains its autonomous distribution behavior.");
            passed.Add("White keeper remains AI-controlled and distributes");
            return new {passed};
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);selection.SetInputLocked(false);ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f));roster.ResetPositions();selection.Select(first);
            foreach(var c in ai)c.enabled=true;
        }
    }
    private static void Press(Keyboard keyboard,params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    private static async Task Tap(Keyboard keyboard,Key key,float seconds=0.08f){Press(keyboard,key);await Advance(seconds);Press(keyboard);await Advance(0.04f);}
    private static async Task Advance(float seconds){float start=Time.fixedTime;var w=Stopwatch.StartNew();while(Time.fixedTime-start<seconds){if(w.Elapsed.TotalSeconds>8f)throw new TimeoutException("Physics stopped.");await Task.Yield();}}
    private static async Task Until(Func<bool> condition,float seconds){var w=Stopwatch.StartNew();while(!condition()){if(w.Elapsed.TotalSeconds>seconds)throw new TimeoutException("Expected control/possession event did not occur.");await Task.Yield();}}
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
