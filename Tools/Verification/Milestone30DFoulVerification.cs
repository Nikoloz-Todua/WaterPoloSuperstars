using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Milestone30DFoulVerification
{
    public static async Task<string> Run()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || HubMatchBridge.Active) throw new InvalidOperationException("Use the isolated ordinary foul lab.");
        await Until(() => match.Rules.OrdinaryFreeThrow && ball.Owner == match.Rules.Taker.Interaction, 18f, "Ordinary ready state");
        var taker = match.Rules.Taker;
        var outlet = roster.FieldPlayers.First(p => p.Team == taker.Team && p != taker);
        var offender = roster.FieldPlayers.First(p => p.Team != taker.Team);
        var players = roster.FieldPlayers.Where(p => p != taker).ToArray();
        var visual = taker.GetComponentInChildren<RiggedSwimmerVisual>();
        float matchClock = match.Clock.Remaining, shotClock = match.ShotClock.Remaining;
        Vector3 mark = taker.Motor.Position;
        var initial = players.Select(p => p.Motor.Position).ToArray();
        await Capture("01-Ready");
        float minClearance = float.PositiveInfinity;
        for (int i = 0; i < 20; i++)
        {
            await Awaitable.WaitForSecondsAsync(.055f);
            minClearance = Mathf.Min(minClearance, players.Where(p => p.Team != taker.Team)
                .Min(p => Vector3.ProjectOnPlane(p.Motor.Position - mark, Vector3.up).magnitude));
            Require(ball.Owner == taker.Interaction && taker.Motor.Position == mark && match.Clock.Remaining == matchClock &&
                match.ShotClock.Remaining == shotClock, "Ready phase holds possession, mark and both clocks.");
        }
        int support = players.Where((p, i) => p.Team == taker.Team && Vector3.Distance(initial[i], p.Motor.Position) > .35f).Count();
        int defense = players.Where((p, i) => p.Team != taker.Team && Vector3.Distance(initial[i], p.Motor.Position) > .35f).Count();
        Require(support > 0 && defense > 0 && minClearance >= 2.18f, "Both sides moved within the protected envelope.");
        await Capture("02-Reposition");
        var anchor = camera.viewpoints[2];
        Vector3 focus = taker.Motor.Position + Vector3.up * .10f + taker.Motor.Facing * .33f;
        anchor.position = focus + Vector3.Cross(Vector3.up, taker.Motor.Facing) * 1.95f + Vector3.up * .85f;
        anchor.LookAt(focus);
        camera.SetView(2);
        await Capture("03-Palm");
        float palmGap = Mathf.Abs(Vector3.Distance(visual.EvaluatedPalm, ball.PhysicsPosition) - ball.Radius);
        Require(palmGap < .015f && visual.EvaluatedPalm.y > ball.PhysicsPosition.y + ball.Radius * .48f, "Palm-over-ball restart grip.");
        Require(taker.GetComponent<SwimmerPassing>().TryPass(), "Intentional pass accepted.");
        await Until(() => match.IsPlaying && ball.Owner != taker.Interaction, 2f, "Physical pass resumed play");
        Require(ball.LastReleaseSource == BallActionSource.HumanInput, "Restart provenance.");

        var goal = Object.FindObjectsByType<GoalMouth>().Single(g => g.ScoringTeam == taker.Team);
        Vector3 spot = goal.transform.position - goal.transform.forward * 5f;
        spot.y = mark.y;
        ball.ResetForRestart(spot);
        taker.Motor.ResetPose(spot, Quaternion.LookRotation(goal.transform.forward));
        Require(taker.Interaction.TryCollect(), "Penalty fixture claim.");
        offender.Motor.ResetPose(spot - goal.transform.forward * .82f, Quaternion.LookRotation(goal.transform.forward));
        Require(taker.GetComponent<ShotCharge>().BeginCharge() &&
            match.Rules.ReportContact(offender, taker, IllegalContact.Push), "Qualifying shooting foul.");
        await Until(() => match.Phase == MatchPhase.PenaltyReady, 4f, "Penalty ready");
        Vector3 penaltyOutlet = outlet.Motor.Position;
        await Awaitable.WaitForSecondsAsync(.32f);
        Require(!match.Rules.OrdinaryFreeThrow && !match.Rules.CanMove(outlet) && outlet.Motor.Position == penaltyOutlet &&
            ball.Owner == taker.Interaction, "Penalty retains stopped off-ball movement.");
        return $"ordinary: {support} teammates and {defense} defenders repositioned; min defender root clearance {minClearance:F3} m; palm gap {palmGap:F4} m; clocks held, human pass resumed, penalty still restricted";
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }

    private static async Task Until(Func<bool> condition, float seconds, string name)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(name);
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Directory.CreateDirectory("Logs/Milestone30D/Foul");
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"Logs/Milestone30D/Foul/{name}.png", texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
    }
}
