using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

public static class BallReplayVerification
{
    const string Output="Logs/BallAndGoalReplays/";
    static T One<T>() where T:Object => Object.FindAnyObjectByType<T>();
    static void Check(bool condition,string message) { if (!condition) throw new Exception(message); }
    static async Task Until(Func<bool> condition, string message, float timeout=10f)
    {
        double deadline=EditorApplication.timeSinceStartup+timeout;
        while (!condition())
        {
            Check(EditorApplication.timeSinceStartup<deadline,message);
            await Task.Delay(12);
        }
    }
    static async Task Shot(string name)
    {
        ScreenCapture.CaptureScreenshot(Output+name+".png");
        int frame=Time.frameCount;
        await Until(()=>Time.frameCount>frame+2,"Screenshot frame");
    }
    public static object Configure()
    {
        Directory.CreateDirectory(Output);
        string path=HubApplication.SavePath;
        var hash=File.Exists(path)?Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path))):"absent";
        File.WriteAllText(Output+"ProfileBaseline.txt",hash);
        SessionState.SetInt("M23Background",(int)InputSystem.settings.backgroundBehavior);
        SessionState.SetInt("M23Input",(int)InputSystem.settings.editorInputBehaviorInPlayMode);
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/OlympicMatchArena.unity");
        return new {profileBaseline=true};
    }
    static async Task<PrototypeMatch> Start()
    {
        Check(Application.isPlaying,"Start advancing Play Mode first.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var match=One<PrototypeMatch>();
        foreach (var ai in Object.FindObjectsByType<PrototypeSwimmerAI>()) ai.enabled=true;
        foreach (var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>()) keeper.enabled=true;
        match.RestartMatch();
        await Until(()=>match.IsPlaying&&!match.AwaitingOpeningPossession,"Sprint acquisition");
        foreach (var ai in Object.FindObjectsByType<PrototypeSwimmerAI>()) ai.enabled=false;
        foreach (var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>()) keeper.enabled=false;
        foreach(var p in One<PrototypeRoster>().Members) p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8f:8f,0f,-8+p.CapNumber),Quaternion.identity);
        return match;
    }
    static (SwimmerIdentity, SwimmerIdentity) PassSetup(float distance, PrototypeTeam team=PrototypeTeam.Blue)
    {
        var pair=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==team).Take(2).ToArray();
        pair[0].Motor.ResetPose(new Vector3(0,0,-6),Quaternion.identity);
        pair[1].Motor.ResetPose(new Vector3(0,0,-6+distance),Quaternion.LookRotation(Vector3.back));
        var ball=One<BallController>();ball.ResetForRestart(pair[0].Motor.Position);
        Check(pair[0].Interaction.TryCollect(),"Fixture possession");
        One<PlayerSelection>().Select(pair[0]);
        return (pair[0],pair[1]);
    }
    public static async Task<object> Baseline()
    {
        var match=await Start(); var (thrower,receiver)=PassSetup(7f);
        var ball=One<BallController>();
        match.GameplayView.SetView(MatchCameraView.SidelineBroadcast);
        Check(thrower.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Baseline pass release");
        await Task.Delay(220);
        await Shot("Before-Pass");
        return new { isAirborne=ball.IsAirborne, altitude=ball.PhysicsPosition.y, surface=ball.SurfaceHeight, speed=ball.Velocity.magnitude };
    }
    public static async Task<object> Passes()
    {
        var match=await Start();
        var keyboard=InputSystem.AddDevice<Keyboard>();
        var ball=One<BallController>();
        var selection=One<PlayerSelection>();
        match.GameplayView.SetView(MatchCameraView.SidelineBroadcast);
        match.GameplayView.SetUserPreferences(.93f,1f);
        var results=new System.Collections.Generic.List<object>();
        try
        {
            foreach(var sample in new[]{(3.5f,"Short"),(7f,"Medium"),(11.2f,"Long")})
            {
                var (thrower,receiver)=PassSetup(sample.Item1);
                await Task.Delay(120);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));
                await Until(()=>ball.IsAirborne&&ball.LastReleaser==thrower.Interaction,"Physical dry pass "+sample.Item2);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                int ownershipChanges=0;
                void Received(SwimmerBallInteraction p,SwimmerBallInteraction n){if(n!=null)ownershipChanges++;}
                ball.PossessionChanged+=Received;
                try
                {
                    Check(ball.Owner==null&&selection.ActivePlayer==receiver,"Handoff before physical catch");
                    float peak=ball.PhysicsPosition.y;
                    float t=Time.time;
                    bool captured=false;
                    while(ball.Owner==null && ball.IsAirborne && Time.time-t<2f)
                    {
                        peak=Mathf.Max(peak,ball.PhysicsPosition.y);
                        if(!captured && Time.time-t > sample.Item1*.042f)
                        {
                            captured=true;
                            await Shot(sample.Item2+"-Dry");
                        }
                        await Task.Delay(12);
                    }
                    Check(ball.Owner==receiver.Interaction,$"{sample.Item2} pass catch. Owner {ball.Owner?.name}, peak {peak}, point {ball.PhysicsPosition}, receiver {receiver.Motor.Position}");
                    Check(ownershipChanges==1,"Single ownership transition");
                    if(sample.Item2=="Medium")await Shot("One-Hand-Catch");
                    results.Add(new{distance=sample.Item1, peak, ballAt=ball.PhysicsPosition.ToString(),ownershipChanges});
                }
                finally{ball.PossessionChanged-=Received;}
                await Task.Delay(90);
            }
            return results;
        }
        finally{InputSystem.RemoveDevice(keyboard);}
    }

    public static async Task<object> WetMissAndLob()
    {
        var match=await Start();
        var ball=One<BallController>();
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            var (thrower,receiver)=PassSetup(6f);
            await Task.Delay(120);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q,Key.B));
            await Until(()=>ball.IsAirborne&&ball.Delivery==PassDelivery.Wet,"Queue wet pass");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            await Until(()=>!ball.IsAirborne,"Wet landing");
            Check(ball.Owner==null&&ball.Velocity.magnitude<=2.9f,"Wet landing leaves collectable ball on water");
            await Shot("Wet-Pass");
            (thrower,receiver)=PassSetup(7f);
            Check(thrower.GetComponent<LobPassing>().TryPass(),"Lob release");
            Check(ball.Delivery==PassDelivery.Lob&&ball.IsAirborne,"Lob flight");
            receiver.Motor.ResetPose(new Vector3(4,0,1),Quaternion.identity);
            await Shot("Lob-Pass");
            await Until(()=>!ball.IsAirborne,"Missed lob splashes down");
            Check(ball.Owner==null && ball.State==BallState.Free,"Missed catch leaves a loose ball");
            return new {wet=true,lob=true,miss=true};
        }
        finally{InputSystem.RemoveDevice(keyboard);}
    }
    static async Task Click(Mouse mouse, Vector2 position)
    {
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position});
        await Task.Delay(65);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position,buttons=1});
        await Task.Delay(75);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position});
        await Task.Delay(50);
    }

    static Vector2 Centre(Transform transform)
    {
        var rect=(RectTransform)transform;
        return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
    }

    static async Task<GoalReplayDirector> Score(PrototypeMatch match, bool blue)
    {
        var ball=One<BallController>();
        var (passer,shooter)=PassSetup(7f,blue?PrototypeTeam.Blue:PrototypeTeam.White);
        var team=shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam;
        var goal=team.AttackingGoal;
        var forward=goal.transform.forward;
        Quaternion facing=Quaternion.LookRotation(forward);
        var mark=goal.transform.position-forward*4.8f+goal.transform.right*.56f;
        passer.Motor.ResetPose(mark-forward*6f,facing);
        shooter.Motor.ResetPose(mark,facing);
        var keeper=Object.FindObjectsByType<PrototypeGoalkeeper>().Single(k=>k.Identity.Team!=shooter.Team);
        var d=new SerializedObject(keeper);
        d.FindProperty("saveChance").floatValue=0;
        d.ApplyModifiedPropertiesWithoutUndo();
        keeper.enabled=true;
        var director=match.GoalPresentation;
        await Task.Delay(3300);
        Check(passer.GetComponent<SwimmerPassing>().TryPassTo(shooter),"Replay build-up pass");
        await Until(()=>ball.Owner==shooter.Interaction,"Replay build-up catch");
        await Task.Delay(220);
        Check(shooter.Interaction.TryShoot(20f,ShotType.Powerful),"Replay physical goal shot");
        await Shot("Airborne-Shot");
        await Until(()=>director.Phase==GoalPresentationPhase.Celebration,"Goal celebration begins");
        return director;
    }

    public static async Task<object> Replay()
    {
        var match=await Start();
        match.GameplayView.SetView(MatchCameraView.SidelineBroadcast);
        var mouse=InputSystem.AddDevice<Mouse>();
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            var director=await Score(match,true);
            var info=director.CurrentGoal;
            Check(info.Team==PrototypeTeam.Blue&&info.ScorerCap>0&&info.ClipSeconds>=4f,"Recorded goal metadata and lead-up");
            var ball=One<BallController>();
            float clock=match.Clock.Remaining, shotClock=match.ShotClock.Remaining;
            await Task.Delay(150);
            await Shot("Goal-Confirmation");
            await Task.Delay(495);
            await Shot("Scorer-Celebration");
            await Click(mouse,new Vector2(Screen.width*.45f,Screen.height*.44f));
            await Until(()=>director.Phase==GoalPresentationPhase.Broadcast,"One click advances celebration");
            float firstTime=director.PlaybackTime;
            var firstBall=director.PlaybackBall;
            var firstCamera=match.GameplayView.transform.position;
            await Task.Delay(420);
            Check(director.Phase==GoalPresentationPhase.Broadcast&&director.PlaybackTime>firstTime+.2f,"One phase per click and interpolating playback");
            Check(Vector3.Distance(director.PlaybackBall,firstBall)>.005f || director.PlaybackTime-firstTime>.3f,"Replay time advances");
            await Until(()=>director.PlaybackTime>=director.ClipEndTime-.17f,"First angle shows shot and goal");
            await Shot("Replay-Sideline");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
            await Until(()=>director.Phase==GoalPresentationPhase.BehindGoal,"Keyboard advances next angle");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Check(director.CurrentGoal.Id==info.Id&&director.CurrentGoal.MatchId==info.MatchId,"Same recorded goal");
            await Task.Delay(185);
            Check(Vector3.Distance(firstCamera,match.GameplayView.transform.position)>2f,"Different camera positions");
            match.SetPaused(true);
            float playhead=director.PlaybackTime;
            await Task.Delay(250);
            Check(director.PlaybackTime==playhead&&match.Clock.Remaining==clock,"Pause freezes presentation and match");
            match.SetPaused(false);
            await Until(()=>director.PlaybackTime>=director.ClipEndTime-.17f,"Second angle shows same shot and goal");
            await Shot("Replay-Goal-Line");
            Check(match.BlueScore==1&&match.Clock.Remaining==clock&&match.ShotClock.Remaining==shotClock&&ball.Owner==null,"No replay scoring, ownership, or clock progression");
            var overlay=One<Canvas>().transform.Find("MatchSafeArea/GoalPresentation");
            await Click(mouse,Centre(overlay.Find("SkipAll")));
            await Until(()=>match.IsCentreRestart,"Skip All returns to centre setup");
            Check(!director.Active&&match.GameplayView.View==MatchCameraView.SidelineBroadcast&&!match.GameplayView.CinematicOverride,"Restored gameplay camera");
            await Shot("Centre-After-Replay");
            await Until(()=>match.IsPlaying,"Conceding White genuine centre pass");
            await Shot("Sideline-After-Replay");
            return new{info.Id,info.ScorerCap,info.ClipSeconds,director.RecordingBytes,match.BlueScore,replayPause=true,mouseAdvance=true,skipAll=true,centrePass=true};
        }
        finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);}
    }

    public static async Task<object> Automatic()
    {
        var match=await Start();
        var director=await Score(match,false);
        Check(director.CurrentGoal.Team==PrototypeTeam.White&&match.WhiteScore==1,"White goal recorded");
        await Until(()=>director.Phase==GoalPresentationPhase.Broadcast,"Automatic first angle");
        await Until(()=>director.Phase==GoalPresentationPhase.BehindGoal,"Automatic second angle");
        await Until(()=>match.Phase==MatchPhase.CentreRestartReady,"Automatic centre readiness");
        Check(match.CentreTaker.Team==PrototypeTeam.Blue&&match.WhiteScore==1,"Conceding team retains centre pass");
        return new{whiteGoal=true, automaticSequence=true,waitsForHumanCentre=true};
    }
    public static async Task<object> Contest()
    {
        var match=await Start();
        var ball=One<BallController>();
        var (thrower,receiver)=PassSetup(7);
        var defender=One<PrototypeRoster>().FieldPlayers.First(p=>p.Team==PrototypeTeam.White);
        defender.Motor.ResetPose(new Vector3(.1f,0,-.18f),Quaternion.LookRotation(Vector3.back));
        Check(thrower.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Contested pass");
        await Until(()=>ball.Owner!=null||!ball.IsAirborne,"Contested pass settled");
        Check(ball.Owner==defender.Interaction,$"Opponent makes swept hand interception. owner {ball.Owner?.name} ball {ball.PhysicsPosition}");

        (thrower,receiver)=PassSetup(7,PrototypeTeam.White);
        receiver.Motor.SetMoveInput(Vector2.right*.43f);
        await Until(()=>receiver.Motor.Velocity.x>1.55f,"Moving receiver");
        float initialX=receiver.Motor.Position.x;
        Check(thrower.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Lead pass");
        await Until(()=>ball.Owner!=null||!ball.IsAirborne,"Moving catch");
        Check(ball.Owner==receiver.Interaction&&receiver.Motor.Position.x>initialX+.4f,"Moving receiver catches in hand");
        receiver.Motor.StopMotion();
        await Task.Delay(220);
        Check(Vector3.Distance(ball.transform.position,receiver.Interaction.HeldBallPoint)<.12f,"Ball follows raised grip");
        await Shot("Moving-Catch");
        return new{interception=true, movingCatch=true, raisedGrip=true};
    }

    public static async Task<object> Distribution()
    {
        var match=await Start();var ball=One<BallController>();
        var keeper=Object.FindObjectsByType<PrototypeGoalkeeper>().First(k=>k.Identity.Team==PrototypeTeam.White);
        var outlet=One<PrototypeRoster>().FieldPlayers.First(p=>p.Team==keeper.Identity.Team);
        var goal=keeper.DefendedGoal;
        var dir=-goal.transform.forward;
        keeper.Identity.Motor.ResetPose(goal.transform.position+dir*1.4f,Quaternion.LookRotation(dir));
        outlet.Motor.ResetPose(goal.transform.position+dir*7.5f,Quaternion.LookRotation(-dir));
        keeper.enabled=true;
        ball.ResetForRestart(keeper.Identity.Motor.Position);
        Check(keeper.Identity.Interaction.TryCollect(),"Keeper holds the ball");
        await Until(()=>ball.LastReleaser==keeper.Identity.Interaction && ball.IsAirborne,"Keeper AI distributes a dry pass");
        Check(ball.Delivery==PassDelivery.Dry&&ball.ShotOrigin.y>ball.SurfaceHeight+.3f,"Keeper launched from raised hand");
        await Until(()=>ball.Owner!=null||!ball.IsAirborne,"Keeper pass complete");
        Check(ball.Owner!=null&&ball.Owner.GetComponent<SwimmerIdentity>().Team==keeper.Identity.Team,"Outlet caught keeper delivery");
        return new{keeperDryDistribution=true};
    }

    public static async Task<object> Shots()
    {
        var match=await Start();
        var ball=One<BallController>();
        var (shooter,receiver)=PassSetup(5);
        var goal=shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
        var forward=goal.transform.forward;
        shooter.Motor.ResetPose(goal.transform.position-forward*5.8f+goal.transform.right*.4f,Quaternion.LookRotation(forward));
        Check(shooter.Interaction.TryShoot(18f,ShotType.Skip),"Skip launch");
        await Until(()=>ball.WaterBounces>0||!ball.IsAirborne,"Actual skip response");
        Check(ball.WaterBounces==1&&ball.IsAirborne,"Skip bounces once and rises");
        await Shot("Skip-Shot");
        await Until(()=>match.GoalPresentationActive||(!ball.IsAirborne&&ball.Velocity.magnitude<1.5f),"Skip goal or miss");
        bool skipReplay=match.GoalPresentationActive;
        if(skipReplay)match.GoalPresentation.SkipAll();
        return new {usedBallPhysics=true, bounced=true, replayedSkip=skipReplay};
    }
    public static async Task<object> TouchPass()
    {
        var match=await Start();
        var ball=One<BallController>();
        var touch=InputSystem.AddDevice<Touchscreen>();
        try
        {
            for(int index=0;index<2;index++)
            {
                var (p,r)=PassSetup(6);
                await Task.Delay(140);
                var button=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Pass);
                var position=Centre(button.transform);
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=20+index,position=position,phase=UnityEngine.InputSystem.TouchPhase.Began});
                await Task.Delay(index==0?95:420);
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=20+index,position=position,phase=UnityEngine.InputSystem.TouchPhase.Ended});
                await Until(()=>ball.IsAirborne,"Touch releases PASS");
                Check(ball.Delivery==(index==0?PassDelivery.Dry:PassDelivery.Wet),"Tap dry, hold wet");
                await Until(()=>!ball.IsAirborne,"Touch pass outcome");
            }
            return new{touchDry=true,touchWet=true};
        }
        finally{InputSystem.RemoveDevice(touch);}
    }

    public static async Task<object> WhiteAI()
    {
        var match=await Start();
        var ball=One<BallController>();
        var (passer,outlet)=PassSetup(7,PrototypeTeam.White);
        var ai=passer.GetComponent<PrototypeSwimmerAI>();
        Vector3 attack=ai.TacticalTeam.AttackDirection;
        passer.Motor.ResetPose(-attack*7,Quaternion.LookRotation(attack));
        outlet.Motor.ResetPose(attack*1.5f,Quaternion.LookRotation(-attack));
        var plan=new SerializedObject(ai.TacticalTeam);
        plan.FindProperty("shotQualityThreshold").floatValue=.99f;
        plan.ApplyModifiedPropertiesWithoutUndo();
        var number=ai.PassesExecuted;
        ai.enabled=true;
        await Until(()=>ai.PassesExecuted>number,"White AI passes on its own");
        Check(ball.LastReleaser==passer.Interaction&&ball.IsAirborne&&ball.Delivery==PassDelivery.Dry,"AI uses same dry pass");
        await Until(()=>ball.Owner!=null||!ball.IsAirborne,"White dry pass settled");
        return new{whiteAiDryPass=true,caught=ball.Owner!=null};
    }

    public static async Task<object> ReplayControls()
    {
        var match=await Start();
        var director=await Score(match,true);
        var touch=InputSystem.AddDevice<Touchscreen>();
        var pad=InputSystem.AddDevice<Gamepad>();
        try
        {
            var point=new Vector2(Screen.width*.42f,Screen.height*.57f);
            await Task.Delay(400);
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=49,position=point,phase=UnityEngine.InputSystem.TouchPhase.Began});
            await Task.Delay(90);
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=49,position=point,phase=UnityEngine.InputSystem.TouchPhase.Ended});
            await Until(()=>director.Phase==GoalPresentationPhase.Broadcast,"Touch advances");
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=50,position=point,phase=UnityEngine.InputSystem.TouchPhase.Began});
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=50,position=point,phase=UnityEngine.InputSystem.TouchPhase.Ended});
            await Task.Delay(325);
            Check(director.Phase==GoalPresentationPhase.Broadcast,"Debounce prevents rapid advance");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.RightShoulder));
            await Until(()=>director.Phase==GoalPresentationPhase.BehindGoal,"Gamepad advances phase");
            InputSystem.QueueStateEvent(pad,new GamepadState());
            match.RestartMatch();
            Check(!director.Active&&director.RecordedFrames==0&&match.BlueScore==0&&!match.GameplayView.CinematicOverride,"Restart clears visuals");
            return new {touchAdvance=true,gamepadAdvance=true,debounce=true,restartCleanup=true};
        }
        finally{InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(pad);}
    }
 static void Resize(int width,bool restore=false)
 {
  var flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var win=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",flags).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(win)});int added=SessionState.GetInt("M23Resize.Added",-1),old=SessionState.GetInt("M23Resize.Size",-1);
  if(added>=0){if(old>=0)selected.SetValue(win,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("M23Resize.Added",-1);}
  if(restore){SessionState.SetInt("M23Resize.Size",-1);return;}
  if(old<0)SessionState.SetInt("M23Resize.Size",(int)selected.GetValue(win));var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M23 temporary verification"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;SessionState.SetInt("M23Resize.Added",added);selected.SetValue(win,added);win.Focus();
 }
    public static async Task<object> SafeAreaViews()
    {
        var match=await Start();
        var director=await Score(match,true);
        await Task.Delay(350);
        director.Advance();
        try
        {
            foreach(int width in new[]{1920,2340,2400})
            {
                Resize(width);
                await Until(()=>Screen.width==width && Screen.height==1080,"Game view fixed resolution");
                var root=One<Canvas>().transform.Find("MatchSafeArea/GoalPresentation");
                foreach(var panel in new[]{"TitleCard","ReplayPause","SkipAll","AdvancePlate"})
                {
                    var rect=(RectTransform)root.Find(panel);
                    var corners=new Vector3[4];
                    rect.GetWorldCorners(corners);
                    Check(corners.All(p=>p.x>=-.5f && p.x<=width+.5f && p.y>=-.5f && p.y<=1080.5f),"Landscape bounds "+panel);
                    Check(rect.GetComponent<CanvasRenderer>()!=null,"Panel has CanvasRenderer");
                }
                await Shot("Replay-"+width+"x1080");
            }
            return new {wideLandscapeLayouts=true, count=3};
        }
        finally{director.SkipAll();Resize(0,true);}
    }

    public static object RestoreEditor()
    {
        Resize(0,true);
        int background=SessionState.GetInt("M23Background",-1),input=SessionState.GetInt("M23Input",-1);
        if(background>=0)InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)background;
        if(input>=0)InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)input;
        SessionState.EraseInt("M23Background");SessionState.EraseInt("M23Input");
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
        string path=HubApplication.SavePath;
        string hash=File.Exists(path)?Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path))):"absent";
        Check(hash==File.ReadAllText(Output+"ProfileBaseline.txt"),"Real progression file hash unchanged");
        return new {profileUnchanged=true,bootRestored=true,play=Application.isPlaying};
    }
    public static async Task<object> LateDeflection()
    {
        var match=await Start();
        var ball=One<BallController>();
        var (shooter,receiver)=PassSetup(6);
        var goal=shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
        var forward=goal.transform.forward;
        var defender=Object.FindObjectsByType<PrototypeGoalkeeper>().Single(k=>k.Identity.Team!=shooter.Team);
        shooter.Motor.ResetPose(goal.transform.position-forward*5.5f,Quaternion.LookRotation(forward));
        defender.Identity.Motor.ResetPose(goal.transform.position-forward*1.5f+goal.transform.right*1.25f,Quaternion.LookRotation(-forward));
        match.Clock.Tick(match.Clock.Remaining-.68f);
        Check(shooter.Interaction.TryShoot(20f,ShotType.Powerful),"Late physical shot");
        await Until(()=>goal.transform.InverseTransformPoint(ball.PhysicsPosition).z>=-1.55f,"Shot enters keeper reach");
        var point=ball.PhysicsPosition;
        Vector3 target=goal.transform.position+forward*.6f+Vector3.up*.42f;
        Check(ball.TryDeflectContact(defender.Identity.Interaction,point,BallTrajectory.Velocity(point,target,.14f)),"Staged keeper deflection");
        await Until(()=>match.GoalPresentationActive,"Deflected ball scores physically");
        var director=match.GoalPresentation;
        Check(director.CurrentGoal.ScorerCap==shooter.CapNumber&&match.BlueScore==1&&match.Clock.Remaining>0f,"Last-second deflection retains shooter and stops clock");
        await Shot("Late-Deflected-Goal");
        director.SkipAll();
        await Until(()=>match.IsPlaying || match.Phase==MatchPhase.QuarterBreak,"Centre or period after late goal");
        Check(match.BlueScore==1,"No duplicate score");
        return new {deflectionGoal=true,latePeriod=true,correctScorer=true};
    }

    public static async Task<object> HandView()
    {
        var match=await Start();
        var ball=One<BallController>();
        var (passer,receiver)=PassSetup(7);
        var inspection=match.GameplayView.GetComponent<ToduStudios.WaterPolo.WaterVisuals.ArenaDeveloperCamera>();
        var waypoint=inspection.viewpoints[2];
        var previous=waypoint.position;
        var rotation=waypoint.rotation;
        var lens=match.GameplayView.GetComponent<Camera>();
        float fov=lens.fieldOfView;
        try
        {
            Vector3 target=receiver.Motor.Position+Vector3.up*.42f-Vector3.forward*.75f;
            Vector3 eye=target+new Vector3(3.75f,1.75f,-1.3f);
            waypoint.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
            inspection.SetView(2);lens.fieldOfView=43f;
            Check(passer.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Dry pass for hand close-up");
            await Until(()=>ball.IsAirborne&&ball.PhysicsPosition.z>receiver.Motor.Position.z-1.55f&&ball.Owner==null,"Actual approach to receiver");
            await Shot("Approaching-Hand");
            await Until(()=>ball.Owner==receiver.Interaction,"Actual catch");
            await Task.Delay(340);
            var rig=receiver.GetComponentInChildren<ToduStudios.WaterPolo.WaterVisuals.RiggedSwimmerVisual>();
            float distance=Vector3.Distance(rig.RightHand.position,ball.transform.position);
            Check(distance<.34f,$"Raised grip remains near rendered hand ({distance:F2} m)");
            await Shot("One-Hand-Catch");
            return new{distanceToRightHand=distance,ballAltitude=ball.transform.position.y};
        }
        finally{waypoint.SetPositionAndRotation(previous,rotation);inspection.SetView(-1);lens.fieldOfView=fov;}
    }
    public static async Task<object> ReplayOff()
    {
        var match=await Start();
        var prefs=One<GamePreferences>();
        bool wasSaved=PlayerPrefs.HasKey(GamePreferences.StorageKey);
        string saved=PlayerPrefs.GetString(GamePreferences.StorageKey);
        var pointer=InputSystem.AddDevice<Mouse>();
        try
        {
            var menus=One<MatchMenuController>();
            menus.OpenPause();
            menus.OpenSettings();
            var button=One<Canvas>().transform.Find("MenuOverlay/MenuSafeArea/SettingsPanel/AutoReplay");
            if(prefs.AutomaticGoalReplays)await Click(pointer,Centre(button));
            Check(!prefs.AutomaticGoalReplays,"Auto replay settings pointer");
            prefs.Save();
            prefs.Reload();
            Check(!prefs.AutomaticGoalReplays,"Auto replay preference persists");
            menus.Cancel();menus.Resume();
            var director=await Score(match,true);
            Check(director.Phase==GoalPresentationPhase.Celebration,"Celebration retained while replays off");
            while(!match.IsCentreRestart)
            {
                Check(director.Phase!=GoalPresentationPhase.Broadcast&&director.Phase!=GoalPresentationPhase.BehindGoal,"Replay disabled");
                await Task.Delay(20);
            }
            return new {autoReplayOff=true, persisted=true, celebrationRetained=true};
        }
        finally
        {
            if(wasSaved)PlayerPrefs.SetString(GamePreferences.StorageKey,saved);else PlayerPrefs.DeleteKey(GamePreferences.StorageKey);
            PlayerPrefs.Save();
            prefs.Reload();
            InputSystem.RemoveDevice(pointer);
        }
    }
    public static async Task<object> CameraReturn()
    {
        var match=await Start();
        var prefs=One<GamePreferences>();
        string saved=PlayerPrefs.GetString(GamePreferences.StorageKey);
        bool existed=PlayerPrefs.HasKey(GamePreferences.StorageKey);
        var keys=InputSystem.AddDevice<Keyboard>();
        var inspection=match.GameplayView.GetComponent<ToduStudios.WaterPolo.WaterVisuals.ArenaDeveloperCamera>();
        async Task KeyDown(Key key)
        {
            InputSystem.QueueStateEvent(keys,new KeyboardState(key));
            await Task.Delay(65);
            InputSystem.QueueStateEvent(keys,new KeyboardState());
            await Task.Delay(65);
        }
        try
        {
            prefs.SetCameraView(0);
            await KeyDown(Key.F6);
            Check(match.GameplayView.View==MatchCameraView.SidelineBroadcast,"F6 switches to Sideline");
            await KeyDown(Key.F8);
            Check(inspection.ViewIndex>=0,"F8 inspection remains available");
            inspection.SetView(-1);
            await KeyDown(Key.F6);
            Check(match.GameplayView.View==MatchCameraView.Lengthwise,"F6 returns to Lengthwise");
            var director=await Score(match,true);
            await KeyDown(Key.F6);
            await KeyDown(Key.F8);
            Check(match.GameplayView.View==MatchCameraView.Lengthwise&&inspection.ViewIndex<0,"Replay camera owns lens and preserves preference");
            director.SkipAll();
            await Until(()=>match.IsCentreRestart,"Lengthwise centre restart");
            Check(match.GameplayView.View==MatchCameraView.Lengthwise&&!match.GameplayView.CinematicOverride,"Lengthwise restored");
            return new {F6=true,F8=true, restoredLengthwise=true};
        }
        finally
        {
            if(existed)PlayerPrefs.SetString(GamePreferences.StorageKey,saved);else PlayerPrefs.DeleteKey(GamePreferences.StorageKey);
            PlayerPrefs.Save();
            prefs.Reload();
            InputSystem.RemoveDevice(keys);
        }
    }
    public static async Task<object> HighGoal()
    {
        var match=await Start();
        var (shooter,receiver)=PassSetup(5);
        var ball=One<BallController>();
        var goal=shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
        var dir=goal.transform.forward;
        shooter.Motor.ResetPose(goal.transform.position-dir*7f+goal.transform.right*.35f,Quaternion.LookRotation(dir));
        Check(shooter.Interaction.TryShoot(16f,ShotType.High),"High shot");
        Check(ball.IsAirborne&&ball.Velocity.y>1f&&ball.Velocity.magnitude<16f,"High arc with reduced horizontal speed");
        await Until(()=>match.GoalPresentationActive,"High shot scores and records");
        Check(match.BlueScore==1&&match.GoalPresentation.CurrentGoal.Id==ball.ShotSequence,"High goal metadata");
        match.GoalPresentation.SkipAll();
        return new{highGoal=true,replay=true};
    }
}

