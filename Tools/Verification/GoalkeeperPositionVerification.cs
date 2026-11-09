using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;

public static class GoalkeeperPositionVerification
{
    public static async Task<object> Run()
    {
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while (match.IsRestarting) await Advance(0.05f);
        var keepers = UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var controllers = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(a => a.enabled).ToArray();
        foreach (var ai in controllers) ai.enabled = false;
        try
        {
            Require(roster.Members.Count == 6 && roster.FieldPlayers.Count == 4 && keepers.Length == 2, "Six athletes, four field players, two keepers.");
            roster.ResetPositions(); selection.Select(roster.FieldPlayers[0]);
            foreach (var keeper in keepers)
            {
                Require(keeper.Identity.Team != keeper.DefendedGoal.ScoringTeam && !keeper.Identity.IsFieldPlayer && !selection.Select(keeper.Identity), "Correct goal/team and keeper cannot be human selected.");
                var attacker = roster.FieldPlayers.First(p => p.Team != keeper.Identity.Team);
                foreach (float x in new[] { 5f, -5f })
                {
                    var point = keeper.DefendedGoal.transform.TransformPoint(new Vector3(x, 0f, -7f)); point.y = 0f;
                    attacker.Motor.ResetPose(point, Quaternion.LookRotation(keeper.DefendedGoal.transform.forward));
                    ball.ResetForRestart(point + keeper.DefendedGoal.transform.forward * 1.65f);
                    Require(attacker.Interaction.TryCollect(), "Attacker pickup for tracking fixture.");
                    Vector3 before = keeper.Identity.Motor.Position;
                    await Advance(0.12f);
                    Require(Vector3.Distance(before, keeper.Identity.Motor.Position) < 0.4f, "Tracking cannot teleport post to post.");
                    await Advance(1.5f);
                    var local = keeper.DefendedGoal.transform.InverseTransformPoint(keeper.Identity.Motor.Position);
                    Require(Mathf.Sign(local.x) == Mathf.Sign(x) && Mathf.Abs(local.x) <= keeper.LateralLimit + 0.001f, "Smooth lateral tracking stays inside mouth.");
                    Require(Vector3.Distance(keeper.Identity.Motor.Position, keeper.ClampMovement(keeper.Identity.Motor.Position)) < 0.001f && Mathf.Abs(keeper.Identity.Motor.Position.y) < 0.001f, "Goal-local envelope and waterline always enforced.");
                }
                ball.ResetForRestart(Vector3.zero); await Advance(2f);
                Require(Mathf.Abs(keeper.DefendedGoal.transform.InverseTransformPoint(keeper.Identity.Motor.Position).x) < 0.15f, "Distant harmless ball returns keeper centrally.");
            }
            return new { athletes = 6, fieldPlayers = 4, keepers = 2, goalLimits = true, smoothTracking = true, selectionExclusion = true };
        }
        finally
        {
            roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(roster.FieldPlayers[0]);
            foreach (var ai in controllers) ai.enabled = true;
        }
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var watch = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds) { if (watch.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped."); await Task.Yield(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
