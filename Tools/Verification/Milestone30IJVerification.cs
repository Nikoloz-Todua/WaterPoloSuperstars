using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30IJVerification
{
    private static PrototypeMatch Match => Object.FindAnyObjectByType<PrototypeMatch>();
    private static BallController Ball => Object.FindAnyObjectByType<BallController>();
    private static bool LabReady => Match != null && Match.IsPlaying && !Match.AwaitingOpeningPossession &&
        Object.FindAnyObjectByType<ArenaDeveloperCamera>()?.ViewIndex == 2;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<string> Run(string fixture)
    {
        Require(Application.isPlaying && !HubMatchBridge.Active, "Launch the isolated Test Lab first.");
        return fixture switch
        {
            "Sideline" => await Movement(false),
            "Mouth" => await Movement(true),
            "Keeper" => await BehindKeeper(),
            "Partial" => await PartialCrossing(),
            "Wide" => await Boundary(BoundaryRestartKind.GoalThrow, true),
            "High" => await Boundary(BoundaryRestartKind.GoalThrow, false),
            "Corner" => await Boundary(BoundaryRestartKind.CornerThrow, false),
            "Defender" => await Boundary(BoundaryRestartKind.CornerThrow, false),
            "SidelineExit" => await Boundary(BoundaryRestartKind.ThrowIn, false),
            "Post" => await PostRebound(),
            "Foul" => await OrdinaryFoul(),
            "Goal" => await Goal(false, false),
            "Late" => await Goal(true, false),
            "Conceding" => await Goal(false, true),
            "Water" => await WaterMotion(),
            "Drop" => await WaterBall(false),
            "Skip" => await WaterBall(true),
            _ => throw new ArgumentException(fixture)
        };
    }

    private static async Task<string> Movement(bool mouth)
    {
        await Wait(() => LabReady);
        var player = Object.FindAnyObjectByType<PlayerSelection>().ActivePlayer;
        var motor = player.Motor;
        player.GetComponent<SwimmerInput>().enabled = false;
        if (!mouth)
        {
            await Swim(motor, Vector3.right, .96f);
            if (motor.Position.x < 0f) await Swim(motor, -Vector3.right, 1.45f);
            else if (motor.Position.x < 10.5f)
            {
                motor.ResetPose(new Vector3(8.4f, motor.Position.y, 2f), Quaternion.identity);
                await Swim(motor, Vector3.right, 1f);
            }
            float first = Mathf.Abs(motor.Position.x);
            Require(first is > 10.5f and < 10.9f, $"First basin edge {first:F2}.");
            float previousZ = motor.Position.z;
            await Swim(motor, Vector3.right * Mathf.Sign(motor.Position.x) + Vector3.forward, .62f);
            float slide = Mathf.Abs(motor.Position.z - previousZ);
            Require(slide > .65f, "Wall halted tangential movement.");
            await Capture("Boundary-Movement");
            float opposite = -Mathf.Sign(motor.Position.x);
            motor.ResetPose(new Vector3(opposite * 8.4f, motor.Position.y, -2.7f), Quaternion.identity);
            await Swim(motor, Vector3.right * opposite, 1.05f);
            float second = Mathf.Abs(motor.Position.x);
            Require(second is > 10.5f and < 10.9f, $"Opposite basin edge {second:F2}.");
            return $"Closed on both basin edges at |x| {first:F2} and {second:F2} m. Tangential displacement {slide:F2} m.";
        }

        var goals = Object.FindObjectsByType<GoalMouth>().OrderBy(g =>
            (g.transform.position - motor.Position).sqrMagnitude).ToArray();
        var results = new string[goals.Length];
        for (int i = 0; i < goals.Length; i++)
        {
            var goal = goals[i];
            var frame = goal.transform;
            foreach (var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>())
                if (keeper.DefendedGoal == goal)
                    keeper.Identity.Motor.ResetPose(frame.TransformPoint(3.25f, 0f, -2.25f), frame.rotation);
            motor.ResetPose(frame.TransformPoint(0f, 0f, -2.15f), frame.rotation);
            await Swim(motor, frame.forward, 1.19f);
            float recess = frame.InverseTransformPoint(motor.Position).z;
            Require(recess is > .46f and < .53f, $"Mouth depth {recess:F3}.");
            if (i == 0) await Capture("Goal-Mouth-Movement");
            await Swim(motor, -frame.forward, .86f);
            Require(frame.InverseTransformPoint(motor.Position).z < -.55f, "Swimmer could not exit recess.");
            motor.ResetPose(frame.TransformPoint(goal.Width * .5f + .04f, 0f, -1.35f), frame.rotation);
            await Swim(motor, frame.forward, .79f);
            float post = frame.InverseTransformPoint(motor.Position).z;
            Require(post < -.37f, $"Swimmer passed through a post at {post:F2} m.");
            results[i] = $"{goal.name} recess {recess:F2} m, post stop {post:F2} m";
        }
        return string.Join(" | ", results);
    }

    private static async Task<string> BehindKeeper()
    {
        await Wait(() => LabReady);
        var keeper = Object.FindObjectsByType<PrototypeGoalkeeper>().Single(k => k.enabled);
        var goal = keeper.DefendedGoal.transform;
        float startDepth = goal.InverseTransformPoint(keeper.Identity.Motor.Position).z;
        await Wait(() => goal.InverseTransformPoint(Ball.PhysicsPosition).z > 0f);
        await Capture("Keeper-Behind-Before");
        await Wait(() => Ball.Owner == keeper.Identity.Interaction);
        await Capture("Keeper-Behind-After");
        float endDepth = goal.InverseTransformPoint(keeper.Identity.Motor.Position).z;
        Require(endDepth - startDepth > .19f && keeper.Catches == 0 && keeper.SaveAttempts == 0 &&
            Match.BlueScore + Match.WhiteScore == 0, "Keeper recovery changed scoring or did not travel.");
        return $"Keeper moved {endDepth - startDepth:F2} m toward the live ball and took ordinary possession. No save or goal.";
    }

    private static async Task<string> PartialCrossing()
    {
        await Wait(() => Ball != null && Ball.IsScoringShot && LabReady);
        var goal = Object.FindObjectsByType<GoalMouth>().Single(g => g.ScoringTeam ==
            Ball.LastReleaser.GetComponent<SwimmerIdentity>().Team);
        float depth = goal.transform.InverseTransformPoint(Ball.PhysicsPosition).z;
        await Awaitable.WaitForSecondsAsync(.24f);
        Require(depth > 0 && depth < Ball.Radius && Match.BlueScore + Match.WhiteScore == 0 && Match.IsPlaying,
            "Partial crossing was scored or was staged outside the opening.");
        await Capture("Goal-Line-Before");
        var keyboard = Keyboard.current;
        Require(keyboard != null, "The fixture needs the configured Input System keyboard.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.T));
        await Awaitable.NextFrameAsync();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Wait(() => Match.GoalPresentationActive);
        Require(Match.BlueScore + Match.WhiteScore == 1 && !Ball.CanScoreGoal && !Ball.CanCollect &&
            Ball.Owner == null, "Complete goal did not lock scoring and acquisition.");
        Require(!Ball.TryCollect(Object.FindAnyObjectByType<PlayerSelection>().ActivePlayer.Interaction),
            "An athlete claimed a scored ball.");
        var keeper = Object.FindObjectsByType<PrototypeGoalkeeper>().First(k => k.DefendedGoal == goal);
        Require(!Ball.TryCatchContact(keeper.Identity.Interaction, Ball.PhysicsPosition), "A scored ball was turned into a save.");
        await Capture("Goal-Line-After");
        return $"Live at centre depth {depth:F3} m (radius {Ball.Radius:F3} m). One goal after the complete crossing.";
    }

    private static async Task<string> Boundary(BoundaryRestartKind kind, bool capture)
    {
        await Wait(() => Ball != null && Ball.AwaitingReplacement);
        var match = Match;
        var ball = Ball;
        var award = match.Rules.BoundaryRestart.Value;
        float clock = match.Clock.Remaining, shotClock = match.ShotClock.Remaining;
        Vector3 start = ball.PhysicsPosition;
        var renders = ball.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        Require(award.Kind == kind && renders.Length > 0 && renders.All(r => !r.forceRenderingOff) &&
            match.Phase == MatchPhase.FreeThrowSetup && !ball.CanCollect && !ball.CanScoreGoal, "Wrong boundary award or dead-ball gates.");
        await Awaitable.WaitForSecondsAsync(.17f);
        float distance = Vector3.Distance(start, ball.PhysicsPosition);
        Require(distance > .25f && Mathf.Abs(clock - match.Clock.Remaining) < .001f &&
            Mathf.Abs(shotClock - match.ShotClock.Remaining) < .001f && ball.AwaitingReplacement, "Continuation did not advance independently.");
        if (capture) await Capture("Wide-Shot-Continuation");
        await Wait(() => !ball.AwaitingReplacement);
        Require(match.BlueScore + match.WhiteScore == 0 &&
            (ball.Owner?.GetComponent<SwimmerIdentity>().Team == award.Team || match.IsPlaying),
            "Restart possession was not handed to the awarded team.");
        return $"{kind} for {award.Team}. Travel {distance:F2} m after decision; ball visible, clocks held, restart supplied.";
    }

    private static async Task<string> PostRebound()
    {
        await Wait(() => LabReady);
        var ball = Ball;
        var goal = Object.FindObjectsByType<GoalMouth>().Single(g => g.ScoringTeam ==
            Object.FindAnyObjectByType<PlayerSelection>().HumanTeam);
        await Wait(() => ball.IsScoringShot && Vector3.Dot(ball.Velocity, goal.transform.forward) < -.2f);
        float speed = ball.Velocity.magnitude;
        Require(Match.IsPlaying && Match.Rules.BoundaryRestart == null && Match.BlueScore + Match.WhiteScore == 0,
            "Post rebound did not remain live.");
        await Wait(() => !ball.IsAirborne && ball.CanCollect);
        var player = Object.FindAnyObjectByType<PlayerSelection>().ActivePlayer;
        player.Motor.ResetPose(ball.PhysicsPosition - goal.transform.right * .54f, Quaternion.LookRotation(goal.transform.right));
        Require(player.Interaction.TryCollect(), "The live post rebound could not be recovered nearby.");
        return $"Physical post reversed the shot at {speed:F2} m/s. Match remained live and the player collected the rebound.";
    }

    private static async Task<string> OrdinaryFoul()
    {
        await Wait(() => Match != null && Match.Phase == MatchPhase.FreeThrowReady && Match.Rules.OrdinaryFreeThrow);
        var match = Match;
        var taker = match.Rules.Taker;
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var ally = roster.FieldPlayers.Where(p => p != taker && p.Team == taker.Team)
            .OrderBy(p => Vector3.Distance(p.Motor.Position, taker.Motor.Position)).First();
        var opponent = roster.FieldPlayers.Where(p => p.Team != taker.Team)
            .OrderBy(p => Vector3.Distance(p.Motor.Position, taker.Motor.Position)).First();
        Vector3 allyStart = ally.Motor.Position, defenderStart = opponent.Motor.Position, mark = taker.Motor.Position;
        float clock = match.Clock.Remaining, shotClock = match.ShotClock.Remaining;
        Require(Ball.Owner == taker.Interaction && !opponent.Interaction.TryCollect() &&
            !Ball.TryShoot(taker.Interaction, 12f, BallActionSource.HumanInput) &&
            !ally.GetComponent<SwimmerPassing>().TryPassTo(taker), "Protected possession or action gates failed.");
        await Awaitable.WaitForSecondsAsync(.65f);
        float allyTravel = Vector3.Distance(allyStart, ally.Motor.Position);
        float opponentTravel = Vector3.Distance(defenderStart, opponent.Motor.Position);
        Require(allyTravel > .12f && opponentTravel > .12f &&
            Vector3.Distance(opponent.Motor.Position, mark) >= .99f &&
            Vector3.Distance(taker.Motor.Position, mark) < .01f &&
            Mathf.Abs(clock - match.Clock.Remaining) < .001f && Mathf.Abs(shotClock - match.ShotClock.Remaining) < .001f,
            $"Foul motion or clocks failed. Ally {allyTravel:F2}, defender {opponentTravel:F2}.");
        await Capture("Ordinary-Foul-Movement");
        Vector3 look = Vector3.ProjectOnPlane(ally.Motor.Position - mark, Vector3.up);
        taker.Motor.ResetPose(mark, Quaternion.LookRotation(look));
        Require(taker.GetComponent<SwimmerPassing>().TryPassTo(ally, BallActionSource.HumanInput), "Human pass was rejected.");
        await Wait(() => match.IsPlaying && Ball.LastReleaseSource == BallActionSource.HumanInput);
        return $"Ally moved {allyTravel:F2} m, defender {opponentTravel:F2} m, taker held the mark. Both clocks stopped; human pass resumed play.";
    }

    private static async Task<string> Goal(bool late, bool conceding)
    {
        PrototypeGoalkeeper keeper = null;
        RiggedSwimmerVisual visual = null;
        var blend = typeof(RiggedSwimmerVisual).GetField("saveWeight", Private);
        float peakSave = 0f;
        if (conceding)
        {
            await Wait(() => LabReady);
            keeper = Object.FindObjectsByType<PrototypeGoalkeeper>().Single(k => k.enabled);
            visual = keeper.Identity.GetComponentInChildren<RiggedSwimmerVisual>();
        }
        await Wait(() =>
        {
            if (visual != null) peakSave = Mathf.Max(peakSave, (float)blend.GetValue(visual));
            return Match != null && Match.GoalPresentationActive;
        });
        var match = Match;
        var director = match.GoalPresentation;
        var scoring = director.CurrentGoal.Team;
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var fields = roster.FieldPlayers.ToArray();
        var before = fields.Select(a => a.Motor.Position).ToArray();
        float clock = match.Clock.Remaining, shotClock = match.ShotClock.Remaining;
        var gesture = typeof(GoalReplayDirector).GetField("scorerGesture", Private).GetValue(director).ToString();
        await Awaitable.WaitForSecondsAsync(.70f);
        float motion = fields.Select((a, i) => Vector3.Distance(a.Motor.Position, before[i])).Max();
        float save = visual != null ? (float)blend.GetValue(visual) : 0f;
        Require(motion > .12f && match.BlueScore + match.WhiteScore == 1 &&
            Mathf.Abs(clock - match.Clock.Remaining) < .001f && Mathf.Abs(shotClock - match.ShotClock.Remaining) < .001f,
            $"Goal motion, score or clock failed. Max movement {motion:F2} m.");
        if (late)
        {
            Require(gesture == "LateSurge", "Late go-ahead goal chose the wrong gesture.");
            await Capture("Celebration-Late");
        }
        else if (conceding)
            Require(keeper.SaveAttempts > 0 && peakSave > .2f && save < .01f, $"Save blend {peakSave:F2} -> {save:F2}.");
        else await Capture("Celebration-Routine");
        string replay = "";
        if (!late && !conceding)
        {
            director.Advance();
            if (director.Phase == GoalPresentationPhase.Broadcast)
            {
                await Awaitable.WaitForSecondsAsync(.13f);
                float first = director.PlaybackTime;
                await Awaitable.WaitForSecondsAsync(.14f);
                Require(director.PlaybackTime > first && !match.IsPlaying, "Recorded replay did not advance.");
                director.Advance();
                Require(director.Phase == GoalPresentationPhase.BehindGoal, "Second replay angle failed.");
                replay = " Both replay views exercised.";
            }
            else replay = " Automatic replays were off.";
        }
        director.SkipAll();
        await Wait(() => match.Phase == MatchPhase.CentreRestartReady);
        Require(match.CentreTaker.Team != scoring && match.CentreSignals == 1 &&
            match.CentreTaker.Interaction == Ball.Owner, "Centre restart award failed.");
        if (!late && !conceding) await Capture("Goal-Reactions-and-Restart");
        await Wait(() => match.IsPlaying);
        Require(Ball.LastReleaseSource == BallActionSource.RuleRestart && match.BlueScore + match.WhiteScore == 1,
            "The centre release did not resume the match once.");
        return $"{gesture}, max swimmer movement {motion:F2} m, keeper blend {peakSave:F2} -> {save:F2}, one score and one opposing centre restart.{replay}";
    }

    private static async Task<string> WaterMotion()
    {
        await Wait(() => LabReady);
        var effects = Object.FindAnyObjectByType<WaterInteractionEffects>();
        var player = Object.FindAnyObjectByType<PlayerSelection>().ActivePlayer;
        player.GetComponent<SwimmerInput>().enabled = false;
        Ball.ResetForRestart(new Vector3(7.8f, Ball.SurfaceHeight, -8f));
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>().viewpoints[2];
        var forward = player.Motor.Facing;
        var side = Vector3.Cross(Vector3.up, forward);
        Vector3 point = player.Motor.Position;
        view.position = point + side * 2.9f - forward * 1.3f + Vector3.up * 1.85f;
        view.LookAt(point + forward * .35f + Vector3.up * .15f);
        int tread = effects.TreadEmissions;
        await Awaitable.WaitForSecondsAsync(1.30f);
        int idle = effects.TreadEmissions - tread;
        await Capture("Water-Idle");
        int entries = effects.LeftHandEntries + effects.RightHandEntries;
        int wakes = effects.WakeEmissions;
        await Swim(player.Motor, player.Motor.Facing, 1.42f);
        int normalWakes = effects.WakeEmissions - wakes;
        int normalEntries = effects.LeftHandEntries + effects.RightHandEntries - entries;
        wakes = effects.WakeEmissions;
        player.GetComponent<SwimmerStamina>().RequestSprint(true);
        await Awaitable.WaitForSecondsAsync(.69f);
        await Capture("Water-Sprint");
        await Awaitable.WaitForSecondsAsync(.73f);
        int sprintWakes = effects.WakeEmissions - wakes;
        Require(idle > 0 && normalEntries > 0 && sprintWakes > normalWakes,
            $"Water events idle {idle}, entry {normalEntries}, wake {normalWakes}/{sprintWakes}.");
        return $"Idle scull {idle} events, {normalEntries} normal hand entries, {normalWakes}/{sprintWakes} normal/sprint wakes.";
    }

    private static async Task<string> WaterBall(bool skip)
    {
        await Wait(() => LabReady);
        var effects = Object.FindAnyObjectByType<WaterInteractionEffects>();
        int previous = effects.WaterImpacts;
        await Wait(() => effects.WaterImpacts > previous);
        if (skip)
            Require(Ball.WaterBounces > 0 && Ball.CurrentShot == ShotType.Skip, "Intentional skip did not bounce.");
        else Require(!Ball.IsAirborne && Ball.WaterBounces == 0 && Mathf.Abs(Ball.Velocity.y) < .05f,
            "Ordinary drop retained an excessive vertical bounce.");
        return $"Water impact {effects.LastWaterImpactStrength:F2}, bounces {Ball.WaterBounces}, vertical speed {Ball.Velocity.y:F2} m/s.";
    }

    private static async Task Swim(SwimmerMotor motor, Vector3 direction, float time)
    {
        motor.SetMoveInput(new Vector2(direction.x, direction.z).normalized);
        await Awaitable.WaitForSecondsAsync(time);
    }

    private static async Task Wait(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 13f;
        while (!condition())
        {
            Require(Application.isPlaying && Time.realtimeSinceStartup < deadline, "Fixture did not reach its staged state.");
            await Awaitable.NextFrameAsync();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Directory.CreateDirectory("Logs/Milestone30IJ");
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"Logs/Milestone30IJ/{name}.jpg", image.EncodeToJPG(88)); }
        finally { Object.Destroy(image); }
    }
}
