using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class MatchPolishVerification
{
    const string Output="Logs/MatchTechnicalPolish/";
    static T One<T>() where T:Object => Object.FindAnyObjectByType<T>();

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static async Task Until(Func<bool> predicate,string message,float seconds=12)
    {
        double deadline=EditorApplication.timeSinceStartup+seconds;
        while(!predicate())
        {
            Check(EditorApplication.timeSinceStartup<deadline,message);
            await Task.Delay(12);
        }
    }

    static async Task Shot(string name)
    {
        Directory.CreateDirectory(Output);
        ScreenCapture.CaptureScreenshot(Output+name+".png");
        int captured=Time.frameCount;
        await Until(()=>Time.frameCount>captured+2,"Game view capture did not advance");
    }

    static async Task<PrototypeMatch> Start()
    {
        Check(Application.isPlaying,"Start advancing Play Mode in OlympicMatchArena.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var match=One<PrototypeMatch>();
        foreach(var ai in Object.FindObjectsByType<PrototypeSwimmerAI>()) ai.enabled=true;
        foreach(var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>()) keeper.enabled=true;
        match.RestartMatch();
        await Until(()=>match.IsPlaying&&!match.AwaitingOpeningPossession,"Real sprint possession");
        foreach(var ai in Object.FindObjectsByType<PrototypeSwimmerAI>()) ai.enabled=false;
        foreach(var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>()) keeper.enabled=false;
        Park();
        return match;
    }

    static void Park()
    {
        foreach(var player in One<PrototypeRoster>().Members)
            player.Motor.ResetPose(new Vector3(player.Team==PrototypeTeam.Blue?-8.6f:8.6f,0,-9+player.CapNumber),Quaternion.identity);
    }

    static void Claim(SwimmerIdentity player)
    {
        var ball=One<BallController>();
        ball.ResetForRestart(player.Motor.Position);
        Check(player.Interaction.TryCollect(),"Actual nearby possession");
        One<PlayerSelection>().Select(player);
    }

    public static async Task<object> KeyboardMovement()
    {
        var match=await Start();
        var prefs=One<GamePreferences>();
        string stored=PlayerPrefs.GetString(GamePreferences.StorageKey);
        bool existed=PlayerPrefs.HasKey(GamePreferences.StorageKey);
        var keyboard=InputSystem.AddDevice<Keyboard>();
        var player=One<PlayerSelection>().ActivePlayer;
        var checks=new List<object>();
        async Task<Vector3> Move(bool carrying, params Key[] keys)
        {
            player.Motor.ResetPose(Vector3.zero,Quaternion.identity);
            if(carrying) Claim(player);
            else One<BallController>().ResetForRestart(new Vector3(0,0,-11));
            await Task.Delay(75);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            await Task.Delay(560);
            Vector3 velocity=player.Motor.Velocity;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            await Task.Delay(190);
            return velocity;
        }
        async Task Compare(Key[] letters, Key[] arrows, bool carrying)
        {
            Vector3 a=await Move(carrying,letters);
            Vector3 b=await Move(carrying,arrows);
            Check(a.magnitude>1f&&b.magnitude>1f&&Vector3.Dot(a.normalized,b.normalized)>.98f&&
                Mathf.Abs(a.magnitude-b.magnitude)<.24f,$"Movement mismatch {letters[0]} {a} versus {arrows[0]} {b}");
            checks.Add(new{camera=match.GameplayView.View.ToString(),letter=letters[0].ToString(),carrying,a=a.magnitude,b=b.magnitude});
        }
        try
        {
            prefs.SetCameraView(0);
            for(int view=0;view<2;view++)
            {
                match.GameplayView.SnapToTarget();
                await Compare(new[]{Key.W},new[]{Key.UpArrow},false);
                await Compare(new[]{Key.A},new[]{Key.LeftArrow},false);
                await Compare(new[]{Key.S},new[]{Key.DownArrow},false);
                await Compare(new[]{Key.D},new[]{Key.RightArrow},false);
                await Compare(new[]{Key.W,Key.D},new[]{Key.UpArrow,Key.RightArrow},true);
                await Compare(new[]{Key.W,Key.LeftShift},new[]{Key.UpArrow,Key.LeftShift},false);
                Check(!EventSystem.current.sendNavigationEvents,"Live UI navigation should be off");
                if(view==0)
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F6));
                    await Task.Delay(90);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    await Until(()=>match.GameplayView.View==MatchCameraView.SidelineBroadcast,"F6 switches viewing axis");
                }
            }
            match.SetPaused(true);
            Vector3 frozen=player.Motor.Position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.RightArrow,Key.LeftShift));
            await Task.Delay(250);
            Check(player.Motor.Position==frozen&&EventSystem.current.sendNavigationEvents,"Pause owns menu navigation");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            match.SetPaused(false);
            return new {checks, keyboardLayout=keyboard.keyboardLayout};
        }
        finally
        {
            match.SetPaused(false);
            InputSystem.RemoveDevice(keyboard);
            if(existed)PlayerPrefs.SetString(GamePreferences.StorageKey,stored); else PlayerPrefs.DeleteKey(GamePreferences.StorageKey);
            PlayerPrefs.Save();
            prefs.Reload();
        }
    }

    public static Task<object> GoalCases() => VerifyGoals(false);
    public static Task<object> UnknownGoal() => VerifyGoals(true);

    private static async Task<object> VerifyGoals(bool unknownOnly)
    {
        var match=await Start();
        var ball=One<BallController>();
        var roster=One<PrototypeRoster>();
        var teams=Object.FindObjectsByType<PrototypeTeamTactics>();
        var records=new List<object>();
        var director=match.GoalPresentation;
        var cases=unknownOnly?new[]{"Unknown pass"}:new[]{"Blue shot","White shot","Blue own pass","White own pass","Keeper deflection","Field deflection","Unknown pass"};
        foreach(var label in cases)
        {
            Park();
            bool white=label.StartsWith("White");
            bool own=label=="Blue own pass"||label=="White own pass";
            bool pass=own||label=="Unknown pass";
            bool deflection=label.Contains("deflection");
            var sourceTeam=white?PrototypeTeam.White:PrototypeTeam.Blue;
            var team=teams.Single(t=>t.Team==sourceTeam);
            var goal=own?team.DefendedGoal:team.AttackingGoal;
            var player=roster.Members.First(p=>p.Team==sourceTeam&&p.IsFieldPlayer==(!own||white));
            Vector3 direction=goal.transform.forward;
            float depth=pass?3.2f:5.5f;
            player.Motor.ResetPose(goal.transform.position-direction*depth,Quaternion.LookRotation(direction));
            Claim(player);
            int oldBlue=match.BlueScore, oldWhite=match.WhiteScore;

            CapsuleCollider blocker=null;
            if(label=="Field deflection")
            {
                var defender=roster.FieldPlayers.First(p=>p.Team!=sourceTeam);
                Vector3 position=goal.transform.position-direction*1.25f+goal.transform.right*.73f;
                defender.Motor.ResetPose(position,Quaternion.identity);
                blocker=defender.GetComponent<CapsuleCollider>();
                blocker.direction=1;
                blocker.height=1.45f;
                blocker.radius=.43f;
                blocker.center=new Vector3(0,.35f,0);
                Physics.SyncTransforms();
            }

            bool released=pass?ball.TryPass(player.Interaction,direction,9f,null,.08f,1f):
                player.Interaction.TryShoot(21,ShotType.Powerful);
            Check(released,"Physical action released for "+label);
            if(label=="Keeper deflection")
            {
                var defender=roster.Members.First(p=>p.Team!=sourceTeam&&!p.IsFieldPlayer);
                await Until(()=>goal.transform.InverseTransformPoint(ball.PhysicsPosition).z>-1.7f,"Position for keeper contact");
                var position=ball.PhysicsPosition;
                Vector3 target=goal.transform.position+direction*.55f+Vector3.up*.43f;
                Check(ball.TryDeflectContact(defender.Interaction,position,BallTrajectory.Velocity(position,target,.13f)),"Keeper deflection applied");
            }
            await Until(()=>director.Active,$"A goal should be recorded for {label} at {ball.PhysicsPosition}",3);
            var info=director.CurrentGoal;
            var kind=own?GoalAttributionKind.OwnGoal:deflection?GoalAttributionKind.DefensiveDeflection:
                pass?GoalAttributionKind.Unknown:GoalAttributionKind.AttackingShot;
            Check(info.Team==goal.ScoringTeam&&info.Attribution.Kind==kind,$"{label} attributed as {info.Attribution.Kind}");
            Check(match.BlueScore==oldBlue+(info.Team==PrototypeTeam.Blue?1:0)&&match.WhiteScore==oldWhite+(info.Team==PrototypeTeam.White?1:0),"Correct score exactly once");
            Check(info.ScorerCap==(own||pass?0:player.CapNumber)&&director.OwnGoalReaction==own,"Actor credits and reactions");
            if(own)Check(!string.IsNullOrEmpty(info.Attribution.PlayerId)&&info.Attribution.CapNumber==player.CapNumber,"Responsible athlete frozen");
            await Task.Delay(660);
            if(own)await Shot(white?"White-Own-Goal":"Blue-Own-Goal");
            else if(label=="Blue shot")
            {
                director.Advance();
                await Task.Delay(300);
                director.Advance();
                await Until(()=>director.PlaybackTime>director.GoalTime+.26f,"Actual net tail is on the second angle");
                var local=goal.transform.InverseTransformPoint(director.PlaybackBall);
                Check(local.z>ball.Radius+.05f&&local.z<goal.Depth,"Replay ball visibly in the net");
                var viewport=match.GameplayView.GetComponent<Camera>().WorldToViewportPoint(director.PlaybackBall);
                Check(viewport.z>0&&viewport.x>.12f&&viewport.x<.88f&&viewport.y>.12f&&viewport.y<.88f,"Net ball framed");
                await Shot("After-Replay-Net");
            }
            director.SkipAll();
            await Until(()=>match.Phase==MatchPhase.CentreRestartReady,"Centre ready");
            Check(match.CentreTaker.Team!=info.Team,"Conceding team takes centre");
            if(match.Phase==MatchPhase.CentreRestartReady) Check(match.TryCentrePass(match.CentreTaker),"Genuine centre release");
            await Until(()=>match.IsPlaying,"Resume live play");
            if(blocker!=null)blocker.enabled=false;
            records.Add(new{label,kind=kind.ToString(),team=info.Team.ToString(),info.ScorerCap,match.BlueScore,match.WhiteScore});
        }
        return records;
    }

    public static Task<object> ObserveBlue() => ObserveExclusion("Blue");
    public static Task<object> ObserveWhite() => ObserveExclusion("White");
    public static Task<object> ObserveEarly() => ObserveExclusion("Early");
    public static Task<object> ObserveHalftime() => ObserveExclusion("Halftime");
    private static async Task<object> ObserveExclusion(string name)
    {
        var match=One<PrototypeMatch>();
        var members=One<PrototypeRoster>().FieldPlayers;
        await Until(()=>members.Any(p=>p.ExclusionRemaining>0),"Exclusion menu affected the match");
        var player=members.First(p=>p.ExclusionRemaining>0);
        float before=player.ExclusionRemaining;
        var board=One<ToduStudios.WaterPolo.WaterVisuals.ArenaEventDisplay>();
        await Until(()=>board.ActiveExclusions==1,"Venue board shows one exclusion");
        Check(match.Rules.ActiveFields(player.Team)==5,"Five eligible field players during exclusion");
        match.SetPaused(true);
        before=player.ExclusionRemaining;
        await Task.Delay(230);
        Check(player.ExclusionRemaining==before&&!player.EligibleForPlay,"Pause freezes actual countdown");
        match.SetPaused(false);
        float clock=match.Clock.Remaining;
        await Task.Delay(1250);
        Check(!player.EligibleForPlay,"No immediate illegal return");
        await Shot("Exclusion-"+name);
        if (name=="White" || name=="Halftime")
        {
            await Until(()=>!player.AwaitingExit&&match.IsPlaying,"Arrival in legal return area");
            match.AdvanceLiveTime(player.ExclusionRemaining+.02f);
        }
        await Until(()=>player.EligibleForPlay,"Legal re-entry",29);
        if (name=="Blue") Check(clock-match.Clock.Remaining>=before-.10f,"Full actual-play duration");
        if (name=="Early") Check(clock-match.Clock.Remaining<before-3f,"Legal early possession release");
        if (name=="Halftime") Check(match.EndsSwitched,"Arrival after side reversal");
        Check(match.Rules.ActiveFields(player.Team)==6&&members.Count==12,"Re-entry restores one roster slot");
        return new {team=player.Team.ToString(),before,player.CapNumber,match.EndsSwitched,pauseFrozen=true};
    }
    public static async Task<object> ObserveReplacement()
    {
        var match=One<PrototypeMatch>();
        var players=One<PrototypeRoster>().FieldPlayers;
        await Until(()=>players.Any(p=>p.PermanentlyRemoved),"Third exclusion sets disqualification",30);
        var player=players.First(p=>p.PermanentlyRemoved);
        int original=player.CapNumber;
        Check(player.PersonalFouls==3&&!player.EligibleForPlay,"Third personal and removal");
        await Shot("Exclusion-Third-Personal");
        await Until(()=>!player.AwaitingExit&&match.IsPlaying,"Replacement waits behind line");
        match.AdvanceLiveTime(player.ExclusionRemaining+.02f);
        await Until(()=>player.EligibleForPlay,"Legal substitute enters");
        Check(player.CapNumber!=original&&player.LastDisqualifiedCap==original&&match.Rules.ActiveFields(player.Team)==6,
            "Permanent removal retained while one replacement fills slot");
        await Shot("Exclusion-Replacement");
        return new{original,replacement=player.CapNumber,active=match.Rules.ActiveFields(player.Team)};
    }
}
