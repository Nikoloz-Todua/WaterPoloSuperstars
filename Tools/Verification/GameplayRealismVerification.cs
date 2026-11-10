using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using ToduStudios.WaterPolo;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class GameplayRealismVerification
{
    public static object Setup()
    {
        Check(!Application.isPlaying,"Setup requires Edit Mode.");var roster=Find<PrototypeRoster>();var ids=roster.Members.Select(p=>p.PlayerId).ToArray();
        int count=Count();var camera=Find<GameplayCamera>();float old=Read(camera,"normalBroadcast.damping");Write(camera,"normalBroadcast.damping",old+0.1f);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(camera.gameObject.scene);
        try
        {
            ToduStudios.WaterPolo.Editor.GameplayRealismSetup.SetUp();
            Check(Count()==count && ids.SequenceEqual(roster.Members.Select(p=>p.PlayerId)) && ids.Length==14,"Repeat setup preserves scene objects and fourteen athlete identities.");
            Check(Mathf.Abs(Read(camera,"normalBroadcast.damping")-(old+0.1f))<0.00001f,"Repeat setup preserves tuning through YAML float serialization.");
            var refs=UnityEngine.Object.FindObjectsByType<PoolsideReferee>();Check(refs.Length==2 && refs.All(r=>r.GetComponentsInChildren<Collider>().Length==0),"Two deck-only referees without colliders.");
            Check(UnityEngine.Object.FindObjectsByType<BallController>().Length==1 && UnityEngine.Object.FindObjectsByType<WaterInteractionEffects>().Length==1 && UnityEngine.Object.FindObjectsByType<GoalNetReaction>().Length==2,"One ball and VFX controller, two net observers.");
            Check(Find<BallController>().GetComponent<Collider>().sharedMaterial!=null,"Ball rebound material wired.");
            return new {passed=true,athletes=14,referees=2,repeatSafe=true,objects=count};
        }
        finally{Write(camera,"normalBroadcast.damping",old);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(camera.gameObject.scene);}
    }
    public static async Task<object> BallPhysics()
    {
        var f=new Fixture();var keyboard=InputSystem.AddDevice<Keyboard>();var results=new List<string>();
        int impacts=0,skips=0,frames=0;Vector3 bounceVelocity=Vector3.zero;
        void Contact(BallImpactEvent e){if(e.Kind==BallImpactKind.Water)impacts++;if(e.Kind==BallImpactKind.Skip){skips++;bounceVelocity=e.Outgoing;}if(e.Kind==BallImpactKind.Frame)frames++;}
        f.Ball.Contact+=Contact;
        try
        {
            f.Reset();f.Park();var player=f.Blue[0];player.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(player);
            Check(player.Interaction.TryShoot(12f,ShotType.Normal) && f.Ball.IsAirborne && f.Ball.PhysicsPosition.y>f.Ball.SurfaceHeight+0.15f,"Normal shot has limited genuine height.");
            await Until(()=>impacts>0);Check(!f.Ball.IsAirborne && f.Ball.Velocity.x>8f,"Normal water impact retains forward momentum and floats.");
            Check(!f.Ball.CanCollect,"Early water contact cannot bypass release protection.");await Frames(0.4f);Check(f.Ball.CanCollect,"Protected normal shot becomes collectible after cooldown.");results.Add("Normal trajectory, water transition and release protection");
            f.Reset();f.Park();player.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(player);await Frames(0.05f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Frames(0.8f);Check(player.GetComponent<ShotCharge>().IsCharging,"Existing Space charge remains active before release.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(0.03f);
            Check(f.Ball.CurrentShot==ShotType.Powerful && f.Ball.Velocity.magnitude>20f && f.Ball.IsAirborne,"Full Space charge produces powerful physical release.");results.Add("Injected charged powerful shot");
            f.Reset();f.Park();var goal=f.Team(PrototypeTeam.Blue).AttackingGoal;
            player.Motor.ResetPose(At(goal,0f,-7f),Quaternion.LookRotation(goal.transform.forward));f.Claim(player);await Frames(0.04f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R,Key.Space));await Frames(0.55f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(0.03f);
            Check(f.Ball.CurrentShot==ShotType.High && f.Ball.IsAirborne && f.Ball.PhysicsPosition.y>f.Ball.SurfaceHeight+0.4f,"R+Space selects real elevated shot.");
            await Until(()=>f.Match.Phase==MatchPhase.GoalRestart);Check(f.Match.BlueScore==1,"High shot below crossbar scores physically once.");results.Add("Injected high shot, height-aware goal");
            f.Reset();f.Park();player.Motor.ResetPose(At(goal,0f,-8f),Quaternion.LookRotation(goal.transform.forward));f.Claim(player);await Frames(0.04f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q,Key.Space));await Frames(0.8f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(0.03f);
            Check(f.Ball.CurrentShot==ShotType.Skip,"Q+Space selects skip profile.");
            await Until(()=>skips>0);Check(bounceVelocity.y>0.5f && f.Ball.WaterBounces==1 && bounceVelocity.magnitude>15f,"Skip responds to actual water crossing with bounded upward rebound and momentum.");
            await Until(()=>f.Match.Phase==MatchPhase.GoalRestart);Check(f.Match.BlueScore==1,"Skipped shot crosses the ordinary goal detector.");
            Vector3 entered=f.Ball.PhysicsPosition;await Frames(0.2f);
            Check(f.Match.BlueScore==1 && f.Ball.Owner==null && !f.Ball.IsScoringShot && goal.transform.InverseTransformPoint(f.Ball.PhysicsPosition).z<=goal.Depth-f.Ball.Radius+0.01f,"Single score and bounded in-net ball.");
            Check(Vector3.Distance(entered,f.Ball.PhysicsPosition)>0.01f && goal.GetComponent<GoalNetReaction>().Reacting,"Ball enters pocket while net reacts.");
            await Until(()=>f.Match.IsPlaying);Check(f.Ball.Owner!=null && f.Ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White,"Conceding restart after skip goal.");results.Add("Injected skip, goal, net motion, exact-once conceding restart");
            f.Reset();f.Park();player.Motor.ResetPose(new Vector3(-5f,0f,0f),Quaternion.LookRotation(Vector3.right));f.Claim(player);
            Check(f.Ball.TryLob(player.Interaction,Vector3.right,7f,1.3f,null),"Lob release.");await Frames(0.5f);
            Check(f.Ball.PhysicsPosition.y>1.5f && !f.Ball.CanCollect && !f.White[0].Interaction.TryCollect(),"High lob cannot be acquired from the water.");
            Vector3 landing=f.Ball.LandingPoint;f.Blue[1].Motor.ResetPose(landing,Quaternion.identity);f.White[0].Motor.ResetPose(landing,Quaternion.identity);
            await Until(()=>f.Ball.CanCollect);
            Check(f.Blue[1].Interaction.TryCollect() && !f.White[0].Interaction.TryCollect() && f.Selection.ActivePlayer==f.Blue[1],"Physical contested low reception remains exclusive and auto-selects real Blue recipient.");results.Add("Lob height, prediction, contested legitimate reception");
            f.Reset();f.Park();player.Motor.ResetPose(new Vector3(-5f,0f,0f),Quaternion.LookRotation(Vector3.right));f.Claim(player);
            f.Ball.TryLob(player.Interaction,Vector3.right,7f,1.3f,null);await Until(()=>!f.Ball.IsAirborne);
            Check(f.Ball.Velocity.magnitude<3f && f.Ball.CanCollect,"Soft lob loses speed and becomes collectible without skip rebound.");results.Add("Soft lob landing distinct from skip");
            // Real collider fixtures: release through the gameplay API, then place a controlled incoming physics pose.
            f.Reset();f.Park();player.Motor.ResetPose(At(goal,goal.Width*0.5f+0.05f,-5f),Quaternion.LookRotation(goal.transform.forward));f.Claim(player);
            int before=frames;player.Interaction.TryShoot(16f,ShotType.Normal);
            await Until(()=>frames>before || Vector3.Dot(f.Ball.Velocity,goal.transform.forward)<-0.1f);
            Check(frames>before && Vector3.Dot(f.Ball.Velocity,goal.transform.forward)<0f && f.Match.BlueScore==0,"Actual post collider rebounds without scoring.");
            await Frames(0.8f);Check(f.Ball.CanCollect,"Post rebound becomes collectible.");results.Add("Physical post rebound and pickup");
            f.Reset();f.Park();player.Motor.ResetPose(At(goal,0f,-5f),Quaternion.LookRotation(goal.transform.forward));f.Claim(player);player.Interaction.TryShoot(12f,ShotType.High);
            var body=f.Ball.GetComponent<Rigidbody>();body.position=goal.transform.TransformPoint(new Vector3(0f,goal.Height+0.05f,-2f));body.linearVelocity=goal.transform.forward*12f;before=frames;
            await Until(()=>frames>before || Vector3.Dot(f.Ball.Velocity,goal.transform.forward)<0f);
            Check(frames>before && f.Match.BlueScore==0,"Real crossbar contact is not a valid goal.");results.Add("Physical crossbar collision");
            f.Reset();f.Park();player.Motor.ResetPose(At(goal,0f,-5f),Quaternion.LookRotation(goal.transform.forward));f.Claim(player);player.Interaction.TryShoot(16f,ShotType.High);
            body.position=goal.transform.TransformPoint(new Vector3(0f,2.5f,-1f));body.linearVelocity=goal.transform.forward*20f;
            await Frames(0.3f);Check(f.Match.BlueScore==0,"Above-bar ball cannot score.");
            Check(f.Roster.Members.Count(p=>p.Interaction.HasBall)<=1,"One authoritative owner.");return new {passed=true,checks=results,waterImpacts=impacts,skipImpacts=skips,frameContacts=frames};
        }
        finally{f.Ball.Contact-=Contact;InputSystem.RemoveDevice(keyboard);f.Dispose();}
    }
    public static async Task<object> CameraRefereesEffects()
    {
        var f=new Fixture();var camera=Find<GameplayCamera>();var lens=camera.GetComponent<Camera>();float aspect=lens.aspect;var mode=camera.Mode;
        var effects=Find<WaterInteractionEffects>();var refs=UnityEngine.Object.FindObjectsByType<PoolsideReferee>();
        try
        {
            f.Reset();f.Park();f.Blue[0].Motor.ResetPose(new Vector3(-3f,0f,-5f),Quaternion.identity);f.White[0].Motor.ResetPose(new Vector3(4f,0f,6f),Quaternion.identity);f.Claim(f.White[0]);f.Selection.Select(f.Blue[0]);
            foreach(float ratio in new[]{16f/9f,19.5f/9f,21f/9f})
            {
                lens.aspect=ratio;await Frames(1.2f);
                foreach(Vector3 point in new[]{f.Blue[0].Motor.Position,f.Ball.PlayPosition}){var v=lens.WorldToViewportPoint(point);Check(v.z>0f && v.x>0.05f && v.x<0.95f && v.y>0.05f && v.y<0.95f,"Broadcast frames controlled player and ball across landscape aspects.");}
            }
            float normal=camera.Zoom;camera.Mode=BroadcastCameraMode.WideTactical;await Frames(1f);Check(camera.ActiveMode==BroadcastCameraMode.WideTactical && camera.Zoom>=normal-0.02f,"Wide tactical profile.");
            Vector3 pose=camera.transform.position;f.Selection.Select(f.Blue[1]);Check(Vector3.Distance(pose,camera.transform.position)<0.001f,"Manual handoff does not snap camera.");
            f.Claim(f.Blue[2]);Check(camera.Target==f.Blue[2].Motor && Vector3.Distance(pose,camera.transform.position)<0.001f,"Automatic handoff retargets without immediate snap.");
            var keeper=f.Keepers.First(k=>k.Identity.Team==PrototypeTeam.Blue);f.Ball.ResetForRestart(keeper.Identity.Motor.Position);Check(keeper.Identity.Interaction.TryCollect(),"Legitimate Blue keeper fixture acquisition.");await Frames(0.6f);
            Check(camera.ActiveMode==BroadcastCameraMode.GoalkeeperPossession && camera.Target==keeper.Identity.Motor,"Keeper profile preserves manual keeper control.");
            f.Reset();f.Ball.ResetForRestart(Vector3.zero);f.Match.ShotClock.ResetPossession();await Frames(0.5f);
            // Referees move toward a sector, stop, and ignore small midfield noise.
            f.Ball.ResetForRestart(new Vector3(0f,f.Ball.SurfaceHeight,11f));await Frames(3f);
            Check(refs.All(r=>Mathf.Abs(r.Speed)<=2.21f && Mathf.Abs(r.transform.position.x)>10f && Mathf.Abs(r.transform.position.z)<=12.91f),"Referee speed and deck boundaries are bounded.");
            Check(refs.Any(r=>r.transform.position.z>2.5f),"Referee moves toward attacking observation sector.");
            Check(refs.All(r=>Vector3.Dot(r.transform.forward,Vector3.ProjectOnPlane(f.Ball.PlayPosition-r.transform.position,Vector3.up).normalized)>0.9f),"Referees turn toward observed play.");
            f.Ball.ResetForRestart(Vector3.zero);await Frames(8f);var positions=refs.Select(r=>r.transform.position).ToArray();
            for(int i=0;i<5;i++){f.Ball.ResetForRestart(new Vector3(0f,f.Ball.SurfaceHeight,(i%2==0?1f:-1f)*0.2f));await Frames(0.1f);}
            Check(refs.All(r=>Mathf.Abs(r.Speed)<0.05f) && refs.Select((r,i)=>Vector3.Distance(r.transform.position,positions[i])).All(d=>d<0.05f),"Dead zone prevents constant referee corrections around stationary play.");
            f.Blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);f.White[0].Motor.ResetPose(Vector3.back*0.9f,Quaternion.identity);f.Claim(f.Blue[0]);
            Check(f.Blue[0].GetComponent<ShotCharge>().BeginCharge(ShotType.High),"High charge before foul.");
            Check(f.Match.Rules.ReportContact(f.White[0],f.Blue[0],IllegalContact.Push),"Real rule contact for presentation.");
            Check(!f.Blue[0].GetComponent<ShotCharge>().IsCharging && f.Ball.ReleaseKind==BallReleaseKind.None,"Foul cancels elevated charge without stale release.");
            Check(refs.All(r=>r.Signal==RefereeSignal.Foul),"Foul signals follow authoritative decision.");await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);
            await Frames(0.2f);Check(camera.ActiveMode==BroadcastCameraMode.SetPiece,"Protected restart camera profile.");
            f.Ball.TryPass(f.Match.Rules.Taker.Interaction,Vector3.right,8f,null,0.08f,2f);
            f.Ball.ResetForRestart(new Vector3(0f,f.Ball.SurfaceHeight,11f));await Frames(0.9f);Check(refs.Any(r=>r.Walking),"Referee repositioning before penalty.");
            f.Park();var penaltyGoal=f.Team(PrototypeTeam.Blue).AttackingGoal;
            f.Blue[0].Motor.ResetPose(At(penaltyGoal,0f,-5f),Quaternion.LookRotation(penaltyGoal.transform.forward));f.White[0].Motor.ResetPose(At(penaltyGoal,0f,-5.9f),Quaternion.LookRotation(penaltyGoal.transform.forward));f.Claim(f.Blue[0]);
            f.Blue[0].GetComponent<ShotCharge>().BeginCharge();Check(f.Match.Rules.ReportContact(f.White[0],f.Blue[0],IllegalContact.Push) && f.Match.Rules.IsPenalty,"Qualifying penalty while referees move.");
            Check(refs.All(r=>r.Signal==RefereeSignal.Penalty && Mathf.Abs(r.Speed)<=2.21f),"Penalty signals interrupt walking without leaving deck.");
            f.Reset();f.Park();
            int objects=Count();f.Blue[0].Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(f.Blue[0]);f.Blue[0].Interaction.TryShoot(12f,ShotType.Normal);await Frames(0.35f);
            Check(effects.ActiveEffects>0 && effects.ActiveEffects<=effects.PoolCapacity,"Real impact triggers bounded pooled effects.");
            // Isolate expiry from the next legitimate wall impact of the still-travelling ball.
            f.Ball.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
            await Frames(1.2f);Check(effects.ActiveEffects==0 && Count()==objects,$"Effects expire with no object spawning (active={effects.ActiveEffects}, objects={Count()}/{objects}).");
            Check(effects.GetComponentsInChildren<Collider>(true).Length==0,"Effects never collide with gameplay.");
            var quality=new SerializedObject(effects);quality.FindProperty("quality").enumValueIndex=(int)WaterEffectsQuality.Off;quality.ApplyModifiedPropertiesWithoutUndo();await Frames(0.05f);
            Check(effects.ActiveEffects==0 && effects.transform.Cast<Transform>().Where(t=>t.name.StartsWith("PooledRipple")).All(t=>!t.gameObject.activeSelf),"Inspector Quality Off clears pooled renderers.");effects.Quality=WaterEffectsQuality.Standard;
            return new {passed=true,aspects=3,manualAndAutomaticCamera=true,keeperProfile=true,setPiece=true,referees=2,boundedSpeed=true,deadZone=true,pooledEffects=true};
        }
        finally{lens.aspect=aspect;camera.Mode=mode;effects.Quality=WaterEffectsQuality.Standard;f.Dispose();}
    }
    public static async Task<object> Integration()
    {
        var f=new Fixture();var camera=Find<GameplayCamera>();var refs=UnityEngine.Object.FindObjectsByType<PoolsideReferee>();
        try
        {
            f.Reset();f.Park();var goal=f.Team(PrototypeTeam.Blue).AttackingGoal;var guard=f.Keepers.Single(k=>k.Identity.Team==PrototypeTeam.White);guard.enabled=true;
            var tune=new SerializedObject(guard);float save=tune.FindProperty("saveChance").floatValue,catchChance=tune.FindProperty("catchChance").floatValue,reaction=tune.FindProperty("reactionDelay").floatValue;
            tune.FindProperty("saveChance").floatValue=1f;tune.FindProperty("catchChance").floatValue=1f;tune.FindProperty("reactionDelay").floatValue=0f;tune.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                f.Blue[0].Motor.ResetPose(At(goal,0f,-8f),Quaternion.LookRotation(goal.transform.forward));f.Claim(f.Blue[0]);
                f.Blue[0].Motor.ResetPose(At(goal,1.2f,-8f),Quaternion.LookRotation(goal.transform.forward));await Frames(0.6f);
                Check(goal.transform.InverseTransformPoint(guard.Identity.Motor.Position).x>0.1f,"Keeper follows a moving held-ball carrier rather than stale ball Rigidbody pose.");
                f.Blue[0].Motor.ResetPose(At(goal,0f,-8f),Quaternion.LookRotation(goal.transform.forward));f.Claim(f.Blue[0]);await Frames(0.2f);int catches=guard.Catches;
                Check(f.Blue[0].Interaction.TryShoot(22f,ShotType.High),"Powerful high shot released.");await Until(()=>guard.Catches>catches || f.Match.IsRestarting);
                Check(guard.Catches>catches && f.Ball.Owner==guard.Identity.Interaction && f.Match.BlueScore==0,"Height-aware real hand contact catches high shot with configured skill endpoint.");
                f.Reset();f.Park();tune.Update();tune.FindProperty("catchChance").floatValue=0f;tune.ApplyModifiedPropertiesWithoutUndo();
                f.Blue[0].Motor.ResetPose(At(goal,0f,-8f),Quaternion.LookRotation(goal.transform.forward));f.Claim(f.Blue[0]);await Frames(0.2f);
                int blocks=guard.Deflections;f.Blue[0].Interaction.TryShoot(22f,ShotType.Skip);await Until(()=>guard.Deflections>blocks || f.Match.IsRestarting);
                Check(guard.Deflections>blocks && f.Ball.WaterBounces==1 && f.Ball.Owner==null && f.Ball.Velocity.magnitude>5f,"Keeper physically deflects powerful skip after its actual water bounce.");
            }
            finally{tune.Update();tune.FindProperty("saveChance").floatValue=save;tune.FindProperty("catchChance").floatValue=catchChance;tune.FindProperty("reactionDelay").floatValue=reaction;tune.ApplyModifiedPropertiesWithoutUndo();guard.enabled=false;}
            // Airborne turnover must clear all flight and pending reception state through existing match transitions.
            f.Reset();f.Park();f.Claim(f.Blue[0]);f.Ball.TryLob(f.Blue[0].Interaction,Vector3.right,6f,1.3f,null);await Frames(0.2f);Check(f.Ball.IsAirborne,"Airborne turnover fixture.");
            f.Match.AdvanceLiveTime(28f);Check(f.Match.Phase==MatchPhase.TurnoverRestart && !f.Ball.IsAirborne && f.Ball.Owner==null,"Shot-clock turnover cancels flight and possession once.");await Until(()=>f.Match.IsPlaying);
            Check(f.Ball.Owner!=null && f.Ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White,"Opposing turnover restart.");
            f.Reset();f.Ball.ResetForRestart(Vector3.zero);f.Match.ShotClock.ResetPossession();Quaternion orientation=camera.transform.rotation;
            f.Match.AdvanceLiveTime(80f);f.Match.ContinueMatch();f.Match.AdvanceLiveTime(80f);Check(f.Match.Phase==MatchPhase.HalfTime,"Q2 break.");
            f.Match.ContinueMatch();Check(f.Match.EndsSwitched && f.Match.EndSwitchCount==1 && Quaternion.Angle(orientation,camera.transform.rotation)<0.01f,"Halftime updates gameplay goals without rotating broadcast orientation.");
            Check(refs.All(r=>r.GetComponentsInChildren<Collider>().Length==0),"Active referee references remain isolated after ends swap.");
            f.Match.AdvanceLiveTime(80f);f.Match.ContinueMatch();f.Match.AdvanceLiveTime(80f);Check(f.Match.Phase==MatchPhase.FullTime,"Four periods and full time preserved.");
            f.Match.RestartMatch();Check(!f.Match.EndsSwitched && f.Roster.Members.Count==14,"New match restores ends and roster.");
            f.EnableAI();float start=Time.fixedTime;while(Time.fixedTime-start<10f){await Frames(0.1f);Check(f.Roster.Members.Count(p=>p.Interaction.HasBall)<=1,"Exclusive possession during integrated AI play.");Check(f.Selection.ActivePlayer.Team==PrototypeTeam.Blue && f.Selection.ActivePlayer.EligibleForPlay,"Eligible Blue control in integration.");}
            return new {passed=true,physicalHighSave=true,physicalSkipDeflection=true,movingCarrierTracking=true,airborneTurnover=true,halftime=true,fullTime=true,AISeconds=10,exclusiveOwnership=true};
        }
        finally{f.Dispose();}
    }
    public static async Task<object> EffectsQualitySwitch()
    {
        var f=new Fixture();var effects=Find<WaterInteractionEffects>();
        try
        {
            f.Reset();f.Park();effects.Quality=WaterEffectsQuality.Standard;
            f.Blue[0].Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(f.Blue[0]);f.Blue[0].Interaction.TryShoot(12f,ShotType.Normal);await Frames(0.3f);
            Check(effects.ActiveEffects>0,"An active impact exists before disabling effects.");
            var data=new SerializedObject(effects);data.FindProperty("quality").enumValueIndex=(int)WaterEffectsQuality.Off;data.ApplyModifiedPropertiesWithoutUndo();await Frames(0.05f);
            Check(effects.ActiveEffects==0 && effects.transform.Cast<Transform>().Where(t=>t.name.StartsWith("PooledRipple")).All(t=>!t.gameObject.activeSelf),"Inspector Off clears an active splash immediately.");
            Check(!f.Ball.GetComponent<TrailRenderer>().emitting,"Off also clears ball trail.");
            return new {passed=true,activeEffectsCleared=true,inspectorChange=true,trailDisabled=true};
        }
        finally{effects.Quality=WaterEffectsQuality.Standard;f.Dispose();}
    }
    private sealed class Fixture:IDisposable
    {
        public PrototypeMatch Match=Find<PrototypeMatch>();public BallController Ball=Find<BallController>();public PrototypeRoster Roster=Find<PrototypeRoster>();public PlayerSelection Selection=Find<PlayerSelection>();
        public PrototypeSwimmerAI[] AI=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();public PrototypeGoalkeeper[] Keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        public SwimmerIdentity[] Blue,White;private bool[] aState,kState;
        public Fixture(){Check(Application.isPlaying,"Runtime test requires Play Mode.");Blue=Roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();White=Roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();aState=AI.Select(a=>a.enabled).ToArray();kState=Keepers.Select(k=>k.enabled).ToArray();foreach(var a in AI)a.enabled=false;foreach(var k in Keepers)k.enabled=false;}
        public PrototypeTeamTactics Team(PrototypeTeam team)=>UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==team);
        public void Reset()=>Match.RestartMatch();
        public void Park(){int b=0,w=0;foreach(var p in Roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8f:8f,0f,-11f+4f*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
        public void Claim(SwimmerIdentity player){Ball.ResetForRestart(player.Motor.Position);Check(player.Interaction.TryCollect(),"Fixture uses range-checked collection.");}
        public void EnableAI(){foreach(var a in AI)a.enabled=true;foreach(var k in Keepers)k.enabled=true;}
        public void Dispose(){Match.RestartMatch();for(int i=0;i<AI.Length;i++)AI[i].enabled=aState[i];for(int i=0;i<Keepers.Length;i++)Keepers[i].enabled=kState[i];}
    }
    private static Vector3 At(GoalMouth goal,float x,float z){var p=goal.transform.TransformPoint(new Vector3(x,0f,z));p.y=0f;return p;}
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    private static int Count()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
    private static float Read(UnityEngine.Object o,string field)=>new SerializedObject(o).FindProperty(field).floatValue;
    private static void Write(UnityEngine.Object o,string field,float value){var d=new SerializedObject(o);d.FindProperty(field).floatValue=value;d.ApplyModifiedPropertiesWithoutUndo();}
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static async Task Frames(float seconds){float start=Time.fixedTime;var watch=Stopwatch.StartNew();while(Time.fixedTime-start<seconds){if(watch.Elapsed.TotalSeconds>15)throw new TimeoutException("Physics not advancing.");await Task.Yield();}}
    private static async Task Until(Func<bool> condition){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>12)throw new TimeoutException("Expected realism event did not occur.");await Task.Yield();}}
}
