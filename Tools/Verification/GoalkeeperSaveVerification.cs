using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;

public static class GoalkeeperSaveVerification
{
    public static async Task<object> Run()
    {
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while (match.IsRestarting) await Advance(0.05f);
        var keepers = UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var ai = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(c => c.enabled).ToArray();
        foreach (var c in ai) c.enabled = false;
        var tuning = keepers.Select(k => new { Keeper = k, Save = Read(k,"saveChance"), Catch = Read(k,"catchChance"), Reaction = Read(k,"reactionDelay"), Speed = Read(k.Identity.Motor,"maxSpeed") }).ToArray();
        var passed = new List<string>();
        int Score() => match.BlueScore + match.WhiteScore;
        async Task Prepare(PrototypeGoalkeeper keeper, float x, float distance = 9f)
        {
            ball.ResetForRestart(Vector3.zero); roster.ResetPositions(); selection.Select(roster.FieldPlayers[1]);
            foreach (var k in keepers) k.enabled = k == keeper;
            keeper.Identity.Motor.ResetPose(keeper.Identity.StartPosition.position, keeper.Identity.StartPosition.rotation);
            var shooter = roster.FieldPlayers.First(p => p.Team != keeper.Identity.Team);
            Vector3 point = keeper.DefendedGoal.transform.TransformPoint(new Vector3(x, 0f, -distance)); point.y = 0f;
            shooter.Motor.ResetPose(point, keeper.DefendedGoal.transform.rotation);
            await Advance(0.12f);
            ball.ResetForRestart(point + keeper.DefendedGoal.transform.forward * 1.65f);
            Require(shooter.Interaction.TryCollect(), "Shot fixture uses legitimate pickup."); await Advance(0.04f);
        }
        SwimmerBallInteraction Shooter(PrototypeGoalkeeper keeper) => roster.FieldPlayers.First(p => p.Team != keeper.Identity.Team).Interaction;
        async Task Restarted() { await Until(() => match.IsRestarting, 5f); await Until(() => !match.IsRestarting, 4f); }
        try
        {
            foreach (var k in keepers) { Write(k,"saveChance",1f); Write(k,"catchChance",1f); Write(k,"reactionDelay",0f); Write(k.Identity.Motor,"maxSpeed",0f); }
            foreach (var keeper in keepers)
            {
                await Prepare(keeper, 0f); int count = keeper.Catches, score = Score(); var selected = selection.ActivePlayer;
                Require(ball.TryShoot(Shooter(keeper), 80f), "Launch high-speed physical shot.");
                await Until(() => ball.Owner == keeper.Identity.Interaction, 4f);
                Require(keeper.Catches == count + 1 && ball.State == BallState.Held && !ball.IsScoringShot && roster.Members.Count(p => p.Interaction.HasBall) == 1, "Swept high-speed catch assigns one authoritative owner and clears scoring eligibility.");
                var expected = keeper.Identity.Team == selection.HumanTeam ? keeper.Identity : selected;
                Require(selection.ActivePlayer == expected && Camera.main.GetComponent<GameplayCamera>().Target == expected.Motor &&
                    (keeper.Identity.Team == selection.HumanTeam ? keeper.HumanControlled : !selection.Select(keeper.Identity)), "Confirmed Blue catch selects keeper; White catch preserves Blue control.");
                Require(!Shooter(keeper).TryCollect(), "Opponent cannot take an already held ball.");
                await Advance(0.2f); Require(keeper.Catches == count + 1 && Score() == score, "Same contact cannot duplicate a save or score.");
                Vector3 anchor = keeper.Identity.Interaction.CarryAnchor.localPosition;
                keeper.Identity.Interaction.CarryAnchor.position = keeper.DefendedGoal.transform.TransformPoint(new Vector3(0f, 0.15f, 1f));
                await Advance(0.3f); Require(Score() == score && ball.Owner == keeper.Identity.Interaction, "Keeper-held ball beyond goal plane cannot score.");
                keeper.Identity.Interaction.CarryAnchor.localPosition = anchor;
                passed.Add(keeper.Identity.Team + " high-speed catch, exclusive ownership, held-ball rejection and team-specific control policy");
            }
            var guard = keepers[0];
            await Prepare(guard, 1.25f); int beforeScore = Score(), catches = guard.Catches;
            Require(Shooter(guard).TryShoot(), "Wide shot launch."); await Restarted();
            Require(Score() == beforeScore + 1 && guard.Catches == catches, "Inside-post shot outside stationary reach legitimately scores.");
            passed.Add("Reach-limited shot can beat keeper and score exactly once");

            Write(guard,"catchChance",0f); await Prepare(guard,0f);
            int deflections = guard.Deflections; beforeScore = Score();
            Require(Shooter(guard).TryShoot(), "Block fixture shot.");
            await Until(() => guard.Deflections > deflections, 4f);
            Require(ball.Owner == null && ball.IsScoringShot && guard.DefendedGoal.transform.InverseTransformDirection(ball.Velocity).z < 0f, "Central contact physically reflects ball toward pool and retains scoring eligibility.");
            await Advance(0.25f); Require(ball.CanCollect && Score() == beforeScore && guard.Deflections == deflections + 1, "Deflection becomes collectible with no repeated same-contact save.");
            passed.Add("Physical block/reflection, short release protection and loose-ball continuation");

            await Prepare(guard, (guard.SaveReach + ball.Radius) * 0.999f);
            deflections = guard.Deflections; beforeScore = Score();
            Require(Shooter(guard).TryShoot(), "Glancing fixture shot.");
            await Until(() => guard.Deflections > deflections, 4f);
            Require(ball.IsScoringShot && guard.DefendedGoal.transform.InverseTransformDirection(ball.Velocity).z > 0f, "Glancing contact may continue toward net.");
            await Restarted(); Require(Score() == beforeScore + 1, "Glancing deflection entering net scores through original detector.");
            passed.Add("Deflected ball entering net still scores and restarts all athletes");

            Write(guard,"saveChance",0f); await Prepare(guard,0f); beforeScore = Score();
            int attempts = guard.SaveAttempts; Require(Shooter(guard).TryShoot(), "Zero-skill fixture.");
            await Restarted(); Require(Score() == beforeScore + 1 && guard.SaveAttempts == attempts + 1, "Configured failed save is not a physical wall or repeated random reroll.");
            passed.Add("Deterministic save skill endpoints and no goal-wide blocking wall");
            Write(guard,"saveChance",1f); Write(guard,"catchChance",1f); Write(guard,"reactionDelay",2f);
            await Prepare(guard,0f,5f); beforeScore = Score(); catches = guard.Catches;
            Require(ball.TryShoot(Shooter(guard),80f), "Point-blank reaction fixture shot.");
            await Restarted(); Require(Score()==beforeScore+1 && guard.Catches==catches, "Reaction delay cannot be bypassed by a high-speed point-blank shot between Update calls.");
            passed.Add("Configured reaction delay permits a legitimate point-blank goal");
            return new { passed };
        }
        finally
        {
            foreach (var t in tuning) { Write(t.Keeper,"saveChance",t.Save); Write(t.Keeper,"catchChance",t.Catch); Write(t.Keeper,"reactionDelay",t.Reaction); Write(t.Keeper.Identity.Motor,"maxSpeed",t.Speed); t.Keeper.enabled = true; }
            if (!match.IsRestarting) { roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(roster.FieldPlayers[0]); }
            foreach (var c in ai) c.enabled = true;
        }
    }
    private static float Read(UnityEngine.Object obj,string field) => new SerializedObject(obj).FindProperty(field).floatValue;
    private static void Write(UnityEngine.Object obj,string field,float value) { var data = new SerializedObject(obj); data.FindProperty(field).floatValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static async Task Advance(float seconds) { float start=Time.fixedTime; var w=Stopwatch.StartNew(); while(Time.fixedTime-start<seconds) { if(w.Elapsed.TotalSeconds>8f) throw new TimeoutException("Physics stopped."); await Task.Yield(); } }
    private static async Task Until(Func<bool> condition,float seconds) { var w=Stopwatch.StartNew(); while(!condition()) { if(w.Elapsed.TotalSeconds>seconds) throw new TimeoutException("Expected save/goal event did not occur."); await Task.Yield(); } }
    private static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
}
