using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;

public static class GoalkeeperDistributionVerification
{
    public static async Task<object> Run()
    {
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while (match.IsRestarting) await Advance(0.05f);
        var keepers = UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var ai = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(c => c.enabled).ToArray();
        foreach (var c in ai) c.enabled = false;
        var passed = new List<string>(); int deliveries = 0;
        try
        {
            foreach (var keeper in keepers)
            {
                var teammates = roster.FieldPlayers.Where(p => p.Team == keeper.Identity.Team).ToArray();
                var receiver = teammates[1]; var distribution = keeper.GetComponent<GoalkeeperDistribution>();
                for (int round = 0; round < 2; round++)
                {
                    roster.ResetPositions(); ball.ResetForRestart(Vector3.zero);
                    foreach (var field in roster.FieldPlayers) field.Motor.ResetPose(new Vector3(field.Team == PrototypeTeam.Blue ? 8f : -8f, 0f, 0f), Quaternion.identity);
                    var outlet = keeper.DefendedGoal.transform.TransformPoint(new Vector3(0f, 0f, -6f)); outlet.y = 0f;
                    receiver.Motor.ResetPose(outlet, Quaternion.LookRotation(-keeper.DefendedGoal.transform.forward));
                    var selected = roster.FieldPlayers.First(p => p.Team == PrototypeTeam.Blue && p != receiver); selection.Select(selected);
                    await Advance(0.12f); ball.ResetForRestart(keeper.HandPosition);
                    int catches = keeper.Catches, sent = distribution.Distributions;
                    await Until(() => ball.Owner == keeper.Identity.Interaction, 4f);
                    Require(keeper.Catches == catches + 1 && selection.ActivePlayer == selected, "Physical loose-ball collection gives keeper possession without selecting it.");
                    var ownTeam = UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t => t.Team == keeper.Identity.Team);
                    Require(WaterPoloDecision.Evaluate(receiver,ownTeam).Behaviour == SwimmerAIBehaviour.Support, "Field tactics recognize keeper team possession and offer outlet support.");
                    await Advance(0.25f); Require(ball.Owner == keeper.Identity.Interaction, "Keeper secures hold before distribution.");
                    await Until(() => distribution.Distributions > sent, 5f);
                    Require(ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Pass && ball.IntendedReceiver == receiver.Interaction && ball.Velocity.magnitude > 8f, "Keeper releases a real pass to an eligible field outlet.");
                    Vector3 origin = ball.ShotOrigin; await Advance(0.08f);
                    Require(ball.Owner == null && Vector3.Distance(origin,ball.PhysicsPosition)>0.4f, "Pass visibly travels before receiving.");
                    await Until(() => ball.Owner == receiver.Interaction, 5f);
                    Require(roster.Members.Count(p=>p.Interaction.HasBall)==1 && !keeper.Identity.Interaction.HasBall, "Receiving transfers exclusive ownership legitimately.");
                    if (receiver.Team == PrototypeTeam.Blue)
                        Require(selection.ActivePlayer == receiver && Camera.main.GetComponent<GameplayCamera>().Target == receiver.Motor, "Blue outlet reception auto-selects field player and camera.");
                    else Require(selection.ActivePlayer == selected, "White distribution never changes human team control.");
                    deliveries++;
                }
                passed.Add(keeper.Identity.Team + " repeated physical collect/secure/pass/receive with correct human selection");
            }
            var guard = keepers.First(k=>k.Identity.Team==PrototypeTeam.Blue);
            var command = guard.GetComponent<GoalkeeperDistribution>();
            foreach(var field in roster.FieldPlayers) field.Interaction.enabled=false;
            ball.ResetForRestart(guard.HandPosition);
            await Until(()=>ball.Owner==guard.Identity.Interaction,4f);
            int previous = command.Distributions;
            await Until(()=>command.Distributions>previous,6f);
            Require(ball.Owner==null && ball.IntendedReceiver==null && Vector3.Dot(ball.Velocity.normalized,-guard.DefendedGoal.transform.forward)>0.99f, "No eligible outlet causes a timed physical clearance into the pool, not indefinite holding.");
            passed.Add("No-outlet fallback release with no fabricated recipient or possession");
            return new { passed, successfulDeliveries = deliveries };
        }
        finally
        {
            foreach(var field in roster.FieldPlayers) field.Interaction.enabled=true;
            if(!match.IsRestarting) { roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f)); selection.Select(roster.FieldPlayers[0]); }
            foreach(var c in ai) c.enabled=true;
        }
    }
    private static async Task Advance(float seconds) { float start=Time.fixedTime; var w=Stopwatch.StartNew(); while(Time.fixedTime-start<seconds) { if(w.Elapsed.TotalSeconds>8f) throw new TimeoutException("Physics stopped."); await Task.Yield(); } }
    private static async Task Until(Func<bool> condition,float seconds) { var w=Stopwatch.StartNew(); while(!condition()) { if(w.Elapsed.TotalSeconds>seconds) throw new TimeoutException("Expected keeper distribution event did not occur."); await Task.Yield(); } }
    private static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
}
