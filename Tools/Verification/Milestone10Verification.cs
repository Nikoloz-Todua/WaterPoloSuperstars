using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class Milestone10Verification
{
    public static object Setup()
    {
        Check(!Application.isPlaying,"Setup requires Edit Mode.");
        var roster=Find<PrototypeRoster>();var original=roster.Members.Select(p=>p.PlayerId).ToArray();
        int count=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
        ToduStudios.WaterPolo.Editor.Milestone10PrototypeSetup.SetUp();
        Check(original.SequenceEqual(roster.Members.Select(p=>p.PlayerId)) && original.Length==14,"Preserved all fourteen identities in repeat setup.");
        Check(count==UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length),"Repeat setup creates no objects.");
        Check(Find<PrototypeMatch>().Rules==Find<MatchRules>() && UnityEngine.Object.FindObjectsByType<MatchRules>().Length==1,"One authoritative rules component wired to existing match.");
        Check(UnityEngine.Object.FindObjectsByType<MatchAtmosphere>().Length==1 && Find<MatchAtmosphere>().GetComponent<AudioSource>()!=null,"One presentation and audio source.");
        Check(new SerializedObject(Find<PlayerSelection>()).FindProperty("tacticalTeam").objectReferenceValue!=null,"Contextual selection wired.");
        return new {passed=true,athletes=14,repeatSafe=true};
    }
    public static async Task<object> SelectionAndTactics()
    {
        var f=new Fixture();var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            f.Reset();var blue=f.Fields(PrototypeTeam.Blue);var white=f.Fields(PrototypeTeam.White);
            f.Park();white[0].Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.back));f.Claim(white[0]);
            blue[0].Motor.ResetPose(new Vector3(-7f,0f,9f),Quaternion.identity);
            blue[1].Motor.ResetPose(new Vector3(7f,0f,8f),Quaternion.identity);
            blue[2].Motor.ResetPose(new Vector3(0f,0f,-2f),Quaternion.identity);
            f.Selection.Select(blue[0]);await Frames(0.06f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));await Frames(0.08f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(0.04f);
            Check(f.Selection.ActivePlayer==blue[2],"Actual C picks immediate goal-side defender rather than remote roster successor.");
            Check(f.Ball.Owner==white[0].Interaction && f.Selection.ActivePlayer.Team==PrototypeTeam.Blue,"C preserves ownership and team.");
            var previous=f.Selection.ActivePlayer;f.Selection.SelectNext();Check(f.Selection.ActivePlayer!=previous,"C chooses a different useful field.");
            Check(Camera.main.GetComponent<GameplayCamera>().Target==f.Selection.ActivePlayer.Motor,"Camera follows contextual selection.");
            f.Ball.ResetForRestart(Vector3.zero);f.Match.ShotClock.ResetPossession();
            blue[3].Motor.ResetPose(new Vector3(1.5f,0f,0f),Quaternion.identity);f.Selection.Select(blue[0]);f.Blue.ResetDecisions();f.Selection.SelectNext();
            Check(f.Selection.ActivePlayer==blue[3],"Loose-ball switching finds the closest useful route.");
            f.Claim(blue[4]);Check(f.Selection.ActivePlayer==blue[4],"Legitimate friendly acquisition auto-selects carrier.");
            var owner=f.Ball.Owner;f.Selection.SelectNext();Check(f.Ball.Owner==owner && f.Selection.ActivePlayer!=blue[4],"Attacking C supports off-ball control without transferring possession.");
            f.Claim(white[0]);f.Blue.SetDefense(DefenseStrategy.Press);f.Blue.ResetDecisions();
            var presser=f.Blue.Presser;var marks=blue.Where(p=>p!=presser).Select(p=>f.Blue.MarkFor(p)).Where(p=>p!=null).ToArray();
            Check(marks.Length==5 && marks.Distinct().Count()==5,"One presser plus five unique man-oriented assignments.");
            var before=blue.Select(p=>f.Blue.MarkFor(p)).ToArray();await Frames(0.1f);
            Check(before.SequenceEqual(blue.Select(p=>f.Blue.MarkFor(p))),"Assignments remain stable across adjacent decisions.");
            Vector3 press=f.Blue.Defense(blue.First(p=>p!=presser),white[0]);f.Blue.SetDefense(DefenseStrategy.Zone);
            Vector3 zone=f.Blue.Defense(blue.First(p=>p!=f.Blue.Presser),white[0]);Check(Vector3.Distance(press,zone)>0.1f,"Press and Zone retain distinct geometry.");
            Check(!f.Selection.Select(white[0]),"White never selectable.");
            var data=new SerializedObject(f.Selection);data.FindProperty("contextualSwitching").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();
            try{f.Selection.Select(blue[0]);f.Selection.SelectNext();Check(f.Selection.ActivePlayer==blue[1],"Configurable roster-order fallback preserved.");}
            finally{data.Update();data.FindProperty("contextualSwitching").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();}
            return new {passed=true,actualC=true,looseBall=true,offBall=true,uniqueMarks=marks.Length,stableAssignments=true};
        }
        finally{InputSystem.RemoveDevice(keyboard);f.Dispose();}
    }
    public static async Task<object> FoulsAndExclusions()
    {
        var f=new Fixture();
        try
        {
            var blue=f.Fields(PrototypeTeam.Blue);var white=f.Fields(PrototypeTeam.White);
            f.Reset();f.Park();blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);white[0].Motor.ResetPose(new Vector3(0f,0f,-0.9f),Quaternion.identity);
            f.Claim(blue[0]);await Frames(1.05f);
            Check(blue[0].GetComponent<ShotCharge>().BeginCharge(),"Human charge started before foul.");
            float period=f.Match.Clock.Remaining,shot=f.Match.ShotClock.Remaining;
            int decisions=f.Rules.DecisionCount;
            Check(white[0].GetComponent<SwimmerSteal>().TrySteal()==StealResult.Foul,"Real rear body-contact steal command awards ordinary foul.");
            Check(f.Match.Phase==MatchPhase.FreeThrowSetup && f.Ball.Owner==blue[0].Interaction,"Stopped free throw awards existing ball to fouled team.");
            Check(!blue[0].GetComponent<ShotCharge>().IsCharging && white[0].PersonalFouls==0,"Foul cancels charge; ordinary foul adds no personal foul.");
            Check(!f.Rules.ReportContact(white[0],blue[0],IllegalContact.Hold) && f.Rules.DecisionCount==decisions+1,"Repeated contact while stopped cannot duplicate decisions.");
            f.Match.AdvanceLiveTime(50f);Near(f.Match.Clock.Remaining,period,"Period freezes during setup");Near(f.Match.ShotClock.Remaining,shot,"Shot clock freezes during setup");
            await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);
            Check(!white[0].Interaction.TryCollect() && white[0].GetComponent<SwimmerSteal>().TrySteal()==StealResult.Unavailable,"Protected restart rejects collection/steal.");
            Check(!blue[0].Interaction.TryShoot() && !blue[0].GetComponent<ShotCharge>().BeginCharge(),"Pass-only restart blocks direct shot/charge.");
            Check(f.Ball.TryPass(blue[0].Interaction,Vector3.right,8f,null,0.08f,2f) && f.Match.IsPlaying,"Throw through ball API resumes exactly once.");
            Check(!f.Ball.IsScoringShot,"Initial free throw cannot score directly in simplified pass-only profile.");
            // Repeat real off-ball holds; third personal foul cannot expire into eligibility.
            for(int foul=1;foul<=3;foul++)
            {
                f.Park();blue[0].Motor.ResetPose(new Vector3(0f,0f,-4f),Quaternion.identity);blue[1].Motor.ResetPose(Vector3.zero,Quaternion.identity);white[0].Motor.ResetPose(new Vector3(0.8f,0f,0f),Quaternion.identity);f.Claim(blue[0]);
                Check(f.Rules.ReportContact(white[0],blue[1],IllegalContact.Hold),"Off-ball hold yields exclusion.");
                Check(white[0].PersonalFouls==foul && !white[0].EligibleForPlay && !white[0].GetComponent<Collider>().enabled,"Personal foul, ineligibility and non-blocking exclusion.");
                Check(!white[0].Interaction.TryCollect() && !white[0].Interaction.TryShoot() && !f.Selection.IsEligible(white[0]),"Excluded athlete cannot interact or be selected.");
                Check(f.Rules.ActiveFields(PrototypeTeam.White)==5 && f.Blue.FieldAdvantage==1,"Shared man-up/man-down context excludes removed athlete.");
                float remaining=white[0].ExclusionRemaining;f.Match.AdvanceLiveTime(10f);Near(white[0].ExclusionRemaining,remaining,"Exclusion freezes during restart");
                await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);f.Ball.TryPass(f.Rules.Taker.Interaction,Vector3.right,8f,null,0.08f,2f);
                if(foul<3)
                {f.Rules.TickLive(18f);Check(white[0].EligibleForPlay && white[0].GetComponent<Collider>().enabled,"Timed re-entry restores live eligibility/collider.");}
                else{f.Rules.TickLive(100f);Check(white[0].PermanentlyRemoved && !white[0].EligibleForPlay,"Third personal foul remains permanent after time.");}
            }
            f.Match.RestartMatch();Check(f.Roster.Members.All(p=>p.EligibleForPlay && p.PersonalFouls==0 && p.GetComponent<Collider>().enabled),"New match resets eligibility, foul history and colliders.");
            // A nearly expired possession cannot turn over during setup; exactly one turnover follows release.
            f.Park();blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);white[0].Motor.ResetPose(new Vector3(0f,0f,-0.9f),Quaternion.identity);f.Claim(blue[0]);
            f.Match.ShotClock.Tick(f.Match.ShotClock.Remaining-0.01f);
            f.Rules.ReportContact(white[0],blue[0],IllegalContact.Push);f.Match.AdvanceLiveTime(20f);
            Check(f.Match.Phase==MatchPhase.FreeThrowSetup,"Shot clock cannot expire during stopped setup.");
            await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);f.Ball.TryPass(blue[0].Interaction,Vector3.right,8f,null,0.08f,2f);f.Match.AdvanceLiveTime(0.02f);
            Check(f.Match.Phase==MatchPhase.TurnoverRestart,"Expired preserved possession produces one turnover after free-throw release.");
            await Until(()=>f.Match.IsPlaying);Check(f.Ball.Owner!=null && f.Ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White,"One opposing restart after expiry.");
            f.Match.RestartMatch();
            // A Blue exclusion is never a contextual switch candidate, including after halftime.
            f.Park();white[0].Motor.ResetPose(new Vector3(0f,0f,-4f),Quaternion.identity);white[1].Motor.ResetPose(Vector3.zero,Quaternion.identity);blue[1].Motor.ResetPose(new Vector3(0.8f,0f,0f),Quaternion.identity);f.Claim(white[0]);
            f.Rules.ReportContact(blue[1],white[1],IllegalContact.Hold);Check(!f.Selection.IsEligible(blue[1]) && !f.Selection.Select(blue[1]),"Excluded preferred Blue target rejected.");
            await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);
            Check(f.Selection.ActivePlayer!=blue[1] && f.Selection.ActivePlayer.EligibleForPlay,"Opponent free throw replaces excluded human selection and camera target.");
            await Until(()=>f.Match.IsPlaying); // Autonomous White free throw.
            f.Ball.ResetForRestart(Vector3.zero);f.Match.ShotClock.ResetPossession();
            f.Match.Clock.Tick(f.Match.Clock.Remaining-0.001f);f.Match.AdvanceLiveTime(0.002f);f.Match.ContinueMatch();
            f.Match.Clock.Tick(f.Match.Clock.Remaining-0.001f);f.Match.AdvanceLiveTime(0.002f);
            float exclusionAtHalf=blue[1].ExclusionRemaining;Check(f.Match.Phase==MatchPhase.HalfTime,"Halftime while excluded.");f.Match.ContinueMatch();
            Check(f.Match.EndsSwitched && blue[1].PersonalFouls==1 && !blue[1].EligibleForPlay && blue[1].ExclusionRemaining<=exclusionAtHalf,"End swap preserves active exclusion and history.");
            Check(blue[1].Motor.Position.z>0f,"Excluded waiting position follows new defended end.");
            return new {passed=true,contactFoul=true,chargeCancellation=true,clockFreeze=true,freeThrow=true,exclusions=true,reentry=true,thirdPersonal=true,halftime=true};
        }
        finally{f.Dispose();}
    }
    public static async Task<object> PenaltiesAndIntegration()
    {
        var f=new Fixture();
        try
        {
            var blue=f.Fields(PrototypeTeam.Blue);var white=f.Fields(PrototypeTeam.White);
            f.Reset();f.Park();var goal=f.Blue.AttackingGoal;
            Vector3 point=goal.transform.position-goal.transform.forward*5f;point.y=0f;
            blue[0].Motor.ResetPose(point,Quaternion.LookRotation(goal.transform.forward));white[0].Motor.ResetPose(point-goal.transform.forward*0.9f,Quaternion.LookRotation(goal.transform.forward));f.Claim(blue[0]);
            Check(f.Rules.ReportContact(white[0],blue[0],IllegalContact.Push) && !f.Rules.IsPenalty,"Contact near goal alone is an ordinary foul, not penalty.");
            await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);f.Ball.TryPass(blue[0].Interaction,Vector3.right,8f,null,0.08f,2f);
            f.Park();blue[0].Motor.ResetPose(point,Quaternion.LookRotation(goal.transform.forward));white[0].Motor.ResetPose(point-goal.transform.forward*0.9f,Quaternion.LookRotation(goal.transform.forward));f.Claim(blue[0]);
            Check(blue[0].GetComponent<ShotCharge>().BeginCharge(),"Penalty fixture has real human shooting action.");
            Check(f.Rules.ReportContact(white[0],blue[0],IllegalContact.Push) && f.Match.Phase==MatchPhase.PenaltySetup,"Rear foul preventing clear shooting chance awards penalty.");
            Check(white[0].PersonalFouls==1 && !blue[0].GetComponent<ShotCharge>().IsCharging,"Penalty records personal foul and cancels previous charge.");
            float period=f.Match.Clock.Remaining;
            await Until(()=>f.Match.Phase==MatchPhase.PenaltyReady);
            Check(f.Roster.Members.Where(p=>p.IsFieldPlayer && p!=blue[0]).All(p=>goal.transform.InverseTransformPoint(p.Motor.Position).z<=-6f && Vector3.Distance(p.Motor.Position,blue[0].Motor.Position)>=3f),"Other fields are outside 6m and 3m clear.");
            Check(!blue[0].GetComponent<SwimmerPassing>().TryPass(),"Penalty rejects pass.");
            var keeper=f.Keepers.Single(k=>k.Identity.Team==PrototypeTeam.White);keeper.enabled=true;
            var keeperData=new SerializedObject(keeper);float chance=keeperData.FindProperty("saveChance").floatValue,catchChance=keeperData.FindProperty("catchChance").floatValue;
            keeperData.FindProperty("saveChance").floatValue=1f;keeperData.FindProperty("catchChance").floatValue=1f;keeperData.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                blue[0].Motor.ResetPose(blue[0].Motor.Position,Quaternion.LookRotation(goal.transform.forward));
                int saves=keeper.Catches;
                Check(blue[0].Interaction.TryShoot(10f) && f.Match.Phase==MatchPhase.PenaltyFlight,"Penalty uses existing physical shot API.");
                await Until(()=>f.Match.IsPlaying || f.Match.Phase==MatchPhase.GoalRestart);
                Check(keeper.Catches>saves && f.Ball.Owner==keeper.Identity.Interaction && f.Match.BlueScore==0,$"Actual keeper contact saves penalty with exclusive possession and no goal. catches={keeper.Catches-saves}, attempts={keeper.SaveAttempts}, phase={f.Match.Phase}, score={f.Match.BlueScore}, keeper={keeper.Identity.Motor.Position}, hand={keeper.HandPosition}, station={keeper.AtStation}, ball={f.Ball.PhysicsPosition}");
                Near(f.Match.Clock.Remaining,period,"Prototype penalty sequence freezes period clock");
                Check(f.Match.ShotClock.PossessionTeam==PrototypeTeam.White && f.Match.ShotClock.Remaining>27f,"Saved penalty grants defending possession clock once.");
                Vector3 savedAt=keeper.Identity.Motor.Position;await Frames(0.1f);
                Check(Vector3.Distance(savedAt,keeper.Identity.Motor.Position)<0.5f,"Keeper swims from penalty line after save without pool-clamp teleport.");
            }
            finally{keeperData.Update();keeperData.FindProperty("saveChance").floatValue=chance;keeperData.FindProperty("catchChance").floatValue=catchChance;keeperData.ApplyModifiedPropertiesWithoutUndo();}
            // A White AI shooting intent is fouled before release, then its penalty executes autonomously.
            f.Match.RestartMatch();foreach(var k in f.Keepers)k.enabled=false;f.Park();
            var whiteTeam=white[0].GetComponent<PrototypeSwimmerAI>().TacticalTeam;var whiteGoal=whiteTeam.AttackingGoal;
            Vector3 whitePoint=whiteGoal.transform.position-whiteGoal.transform.forward*5f;whitePoint.y=0f;
            white[0].Motor.ResetPose(whitePoint,Quaternion.LookRotation(whiteGoal.transform.forward));
            white[1].Motor.ResetPose(Vector3.zero,Quaternion.identity);blue[1].Motor.ResetPose(new Vector3(0.8f,0f,0f),Quaternion.identity);f.Claim(white[0]);
            Check(f.Rules.ReportContact(blue[1],white[1],IllegalContact.Hold),"Man-up fixture starts from real off-ball exclusion.");await Until(()=>f.Match.IsPlaying);
            white[0].Motor.ResetPose(whitePoint,Quaternion.LookRotation(Quaternion.Euler(0f,30f,0f)*whiteGoal.transform.forward));
            blue[0].Motor.ResetPose(whitePoint-whiteGoal.transform.forward*0.9f,Quaternion.LookRotation(whiteGoal.transform.forward));f.Claim(white[0]);
            await Frames(0.85f);var whiteAI=white[0].GetComponent<PrototypeSwimmerAI>();whiteAI.enabled=true;
            await Until(()=>whiteAI.Behaviour==SwimmerAIBehaviour.Shoot);
            Check(f.Ball.Owner==white[0].Interaction && f.Rules.ReportContact(blue[0],white[0],IllegalContact.Push) && f.Rules.IsPenalty,"Real AI shooting action qualifies for penalty.");whiteAI.enabled=false;
            await Until(()=>f.Match.Phase==MatchPhase.GoalRestart);
            Check(f.Match.WhiteScore==1 && f.Ball.LastReleaser==white[0].Interaction,"AI takes physical penalty through existing ball APIs and scores once.");
            Check(blue[1].EligibleForPlay && blue[1].PersonalFouls==1,"Goal during man-up returns temporary exclusion while retaining history.");
            await Frames(0.08f);Check(f.Match.WhiteScore==1,"No duplicate penalty scoring during goal restart.");
            // One integrated live run, with all AI and genuine physics; no repeated full-suite loops.
            f.Match.RestartMatch();f.EnableAI();int decisions=f.Rules.DecisionCount;float start=Time.fixedTime;
            while(Time.fixedTime-start<12f)
            {
                await Frames(0.1f);
                Check(f.Roster.Members.Count(p=>p.Interaction.HasBall)<=1,"Exclusive possession in fourteen-athlete integration.");
                Check(f.Selection.ActivePlayer.Team==PrototypeTeam.Blue && f.Selection.ActivePlayer.EligibleForPlay,"Selection stays friendly and eligible.");
                Check(f.Roster.Members.All(p=>!p.EligibleForPlay || Mathf.Abs(p.Motor.Position.x)<10f && Mathf.Abs(p.Motor.Position.z)<15f),"Eligible swimmers stay bounded.");
                Check(f.Roster.Members.All(p=>p.GetComponent<SwimmerStamina>().Value>=0f && p.GetComponent<SwimmerStamina>().Value<=1f),"Shared stamina remains bounded.");
            }
            return new {passed=true,penaltyQualified=true,physicalSave=true,autonomousAIPenaltyGoal=true,manUpGoalReentry=true,exclusivePossession=true,integratedSeconds=12,liveFoulDecisions=f.Rules.DecisionCount-decisions};
        }
        finally{f.Dispose();}
    }
    private sealed class Fixture:IDisposable
    {
        public PrototypeMatch Match=Find<PrototypeMatch>();public BallController Ball=Find<BallController>();public PrototypeRoster Roster=Find<PrototypeRoster>();public PlayerSelection Selection=Find<PlayerSelection>();public MatchRules Rules=Find<MatchRules>();
        public PrototypeTeamTactics Blue=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue);
        public PrototypeSwimmerAI[] AI=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();public PrototypeGoalkeeper[] Keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        private bool[] aiEnabled,keeperEnabled;
        public Fixture(){Check(Application.isPlaying,"Runtime verification requires Play Mode.");aiEnabled=AI.Select(a=>a.enabled).ToArray();keeperEnabled=Keepers.Select(k=>k.enabled).ToArray();foreach(var a in AI)a.enabled=false;foreach(var k in Keepers)k.enabled=false;}
        public SwimmerIdentity[] Fields(PrototypeTeam team)=>Roster.FieldPlayers.Where(p=>p.Team==team).ToArray();
        public void Reset()=>Match.RestartMatch();
        public void Park(){int b=0,w=0;foreach(var p in Roster.FieldPlayers)if(p.EligibleForPlay)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8f:8f,0f,-11f+4f*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
        public void Claim(SwimmerIdentity p){Ball.ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Fixture acquires through range-checked ball API.");}
        public void EnableAI(){foreach(var a in AI)a.enabled=true;foreach(var k in Keepers)k.enabled=true;}
        public void Dispose(){Match.RestartMatch();for(int i=0;i<AI.Length;i++)AI[i].enabled=aiEnabled[i];for(int i=0;i<Keepers.Length;i++)Keepers[i].enabled=keeperEnabled[i];}
    }
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Near(float a,float b,string message)=>Check(Mathf.Abs(a-b)<0.08f,message+$" ({a} vs {b})");
    private static async Task Frames(float seconds){float start=Time.fixedTime;var watch=Stopwatch.StartNew();while(Time.fixedTime-start<seconds){if(watch.Elapsed.TotalSeconds>10)throw new TimeoutException("Physics not advancing.");await Task.Yield();}}
    private static async Task Until(Func<bool> condition){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>12)throw new TimeoutException("Expected M10 transition did not occur.");await Task.Yield();}}
}
