using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;

public static class GoalkeeperMatchVerification
{
    public static async Task<object> Run()
    {
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while(match.IsRestarting)await Advance(0.05f);
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var fields=roster.FieldPlayers.ToArray();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();
        var tuning=keepers.Select(k=>new { Keeper=k,Chance=Read(k,"saveChance"),Catch=Read(k,"catchChance"),Reaction=Read(k,"reactionDelay") }).ToArray();
        var passed=new List<string>(); int resets=0;bool invalidReset=false;
        void Changed()
        {
            if(match.IsRestarting)return;
            bool valid=roster.Members.All(p=>Vector3.Distance(p.Motor.Position,p.StartPosition.position)<0.01f && p.Motor.enabled) && ball.Owner==null && ball.IntendedReceiver==null && !ball.IsScoringShot && !selection.InputLocked && selection.ActivePlayer.IsFieldPlayer && selection.ActivePlayer.Team==PrototypeTeam.Blue && keepers.All(k=>!k.Recovering && k.GetComponent<GoalkeeperDistribution>().IntendedOutlet==null);
            if(valid)resets++;else invalidReset=true;
        }
        match.Changed+=Changed;
        try
        {
            foreach(var c in ai)c.enabled=false;
            foreach(var keeper in keepers)
            {
                Write(keeper,"saveChance",1f);Write(keeper,"catchChance",1f);Write(keeper,"reactionDelay",0f);
                roster.ResetPositions();ball.ResetForRestart(keeper.HandPosition);
                await Until(()=>ball.Owner==keeper.Identity.Interaction,4f);
                await Advance(0.3f);
                Require(keeper.Recovering && ball.Owner==keeper.Identity.Interaction,"Exercise recovery and secured possession before restart.");
                // Clear the fixture possession, then launch a new legitimate shot at the other net.
                var goal=UnityEngine.Object.FindObjectsByType<GoalMouth>().Single(g=>g.ScoringTeam==keeper.Identity.Team);
                var defender=keepers.Single(k=>k.DefendedGoal==goal);Write(defender,"saveChance",0f);
                var shooter=fields.First(p=>p.Team==keeper.Identity.Team);
                Vector3 position=goal.transform.TransformPoint(new Vector3(0f,0f,-4f));position.y=0f;
                shooter.Motor.ResetPose(position,goal.transform.rotation);await Advance(0.12f);
                ball.ResetForRestart(position+goal.transform.forward*1.65f);
                Require(shooter.Interaction.TryCollect() && ball.TryShoot(shooter.Interaction,80f),"Real shot starts the next goal.");
                int blue=match.BlueScore,white=match.WhiteScore;
                await Until(()=>match.IsRestarting,5f);
                Require(match.BlueScore==blue+(goal.ScoringTeam==PrototypeTeam.Blue?1:0) && match.WhiteScore==white+(goal.ScoringTeam==PrototypeTeam.White?1:0),"Correct scoring team.");
                Require(roster.Members.All(p=>!p.Motor.enabled) && keepers.All(k=>k.Stopped),"All six freeze, including keepers.");
                await Until(()=>!match.IsRestarting,4f);
                Require(!invalidReset && ball.Owner==null,"All six reset with keeper recovery, possession and pending outlet cleared.");
                int distributions=keepers.Sum(k=>k.GetComponent<GoalkeeperDistribution>().Distributions);
                await Advance(0.25f);
                Require(keepers.Sum(k=>k.GetComponent<GoalkeeperDistribution>().Distributions)==distributions,"No stale post-goal pass.");
            }
            Require(resets==2,"Both goals reset all six correctly.");
            passed.Add("Both teams score physical goals; all six freeze/reset, recovery clears and no stale keeper distribution fires");

            foreach(var t in tuning){Write(t.Keeper,"saveChance",t.Chance);Write(t.Keeper,"catchChance",t.Catch);Write(t.Keeper,"reactionDelay",t.Reaction);}
            roster.ResetPositions();ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f));selection.Select(fields[0]);
            foreach(var c in ai)c.enabled=true;
            int shots=ai.Sum(c=>c.ShotsExecuted);float start=Time.fixedTime;
            while(Time.fixedTime-start<20f)
            {
                await Advance(0.1f);
                Require(selection.ActivePlayer.Team==PrototypeTeam.Blue && (selection.ActivePlayer.IsFieldPlayer || ball.Owner==selection.ActivePlayer.Interaction || match.IsRestarting) && Camera.main.GetComponent<GameplayCamera>().Target==selection.ActivePlayer.Motor,"Live play keeps valid Blue selection/camera; keeper selection requires actual possession.");
                Require(roster.Members.Count(p=>p.Interaction.HasBall)<=1,"Live keeper/field possession remains exclusive.");
                foreach(var keeper in keepers)Require(Vector3.Distance(keeper.Identity.Motor.Position,keeper.ClampMovement(keeper.Identity.Motor.Position))<0.001f,"Live keeper remains in its goal envelope.");
                if(ball.Owner!=null && !ball.Owner.GetComponent<SwimmerIdentity>().IsFieldPlayer && selection.ActivePlayer.Interaction!=ball.Owner)Require(Time.time-ball.OwnershipStartedAt<5f,"AI keeper cannot stall past secure/aim/distribution timeout; human holding is deliberate.");
                foreach(var team in UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>())
                {
                    Require(team.Presser==null || team.Presser.IsFieldPlayer,"Keeper excluded from pressing assignment.");
                    Require(team.LoosePursuer==null || team.LoosePursuer.IsFieldPlayer,"Keeper excluded from field-ball pursuit assignment.");
                }
            }
            Require(ai.Sum(c=>c.ShotsExecuted)>shots,"White offense still shoots in full six-athlete play.");
            passed.Add("20 seconds of live six-athlete play: bounded keepers, non-stalling AI keeper possession, field-only tactics, valid field/possessing-keeper selection and physical attacking shots");
            return new {passed,completedRestarts=resets};
        }
        finally
        {
            match.Changed-=Changed;
            foreach(var t in tuning){Write(t.Keeper,"saveChance",t.Chance);Write(t.Keeper,"catchChance",t.Catch);Write(t.Keeper,"reactionDelay",t.Reaction);}
            if(!match.IsRestarting){roster.ResetPositions();ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f));selection.Select(fields[0]);}
            foreach(var c in ai)c.enabled=true;
        }
    }
    private static float Read(UnityEngine.Object obj,string field)=>new SerializedObject(obj).FindProperty(field).floatValue;
    private static void Write(UnityEngine.Object obj,string field,float value){var d=new SerializedObject(obj);d.FindProperty(field).floatValue=value;d.ApplyModifiedPropertiesWithoutUndo();}
    private static async Task Advance(float seconds){float start=Time.fixedTime;var w=Stopwatch.StartNew();while(Time.fixedTime-start<seconds){if(w.Elapsed.TotalSeconds>8f)throw new TimeoutException("Physics stopped.");await Task.Yield();}}
    private static async Task Until(Func<bool> condition,float seconds){var w=Stopwatch.StartNew();while(!condition()){if(w.Elapsed.TotalSeconds>seconds)throw new TimeoutException("Expected keeper/goal event did not occur.");await Task.Yield();}}
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
