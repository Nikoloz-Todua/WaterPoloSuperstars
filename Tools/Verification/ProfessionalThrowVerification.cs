using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ProfessionalThrowVerification
{
    private const string Output = "Logs/Milestone30/Throwing";

    public static async Task<object> Run()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        Require(Application.isPlaying && match != null && ball != null, "Play the production arena.");
        if (intro != null && intro.Active) intro.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Opening sprint-off did not finish.", 19f);
        var gameLens = match.GameplayView.GetComponent<Camera>();

        var options = new SerializedObject(ball);
        bool profileWasEnabled = ball.ProfessionalThrows;
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(brain => brain.enabled).ToArray();
        var athlete = roster.FieldPlayers.First(player => player.Team == PrototypeTeam.Blue);
        var receiver = roster.FieldPlayers.First(player => player.Team == athlete.Team && player != athlete);
        var visual = athlete.GetComponentInChildren<RiggedSwimmerVisual>();
        var input = athlete.GetComponent<SwimmerInput>();
        var charge = athlete.GetComponent<ShotCharge>();
        var passing = athlete.GetComponent<SwimmerPassing>();
        bool inputWasEnabled = input.enabled;
        Vector3 origin = new Vector3(-2f, 0f, -3f);
        Vector3 direction = Vector3.right;
        Vector3 releaseOrigin = default;
        float releaseTime = 0f;
        float releaseError = 0f;
        int releaseCount = 0;
        bool releasedFromGrip = false;
        int renderedFrame = -1;
        Vector3 renderedBall = default, renderedPalm = default, renderedHead = default;
        var throws = new List<object>();
        var turnTrace = new List<string>();
        string turnProbe = null;
        Directory.CreateDirectory(Output);

        void Released(SwimmerBallInteraction source, BallReleaseKind kind)
        {
            if (source != athlete.Interaction || kind == BallReleaseKind.Drop) return;
            releaseCount++;
            releaseOrigin = ball.ShotOrigin;
            releaseTime = Time.time;
            releaseError = Vector3.Distance(releaseOrigin, visual.EvaluatedBallGrip);
            releasedFromGrip = ball.IsEvaluatedRelease;
        }

        void ReadRenderedState(ScriptableRenderContext context, Camera view)
        {
            if (view != gameLens) return;
            renderedBall = ball.transform.position;
            renderedPalm = visual.EvaluatedBallGrip;
            renderedHead = visual.HeadPosition;
            renderedFrame = Time.frameCount;
            if (turnProbe != null)
            {
                Vector3 published = athlete.Interaction.HeldBallPoint;
                Rigidbody physical = ball.GetComponent<Rigidbody>();
                turnTrace.Add($"{turnProbe} f{renderedFrame} t{Time.time:F3} owner{ball.Owner == athlete.Interaction} " +
                    $"dribble{ball.IsDribbling} gather{ball.IsGathering} interpolation{physical.interpolation} " +
                    $"render{renderedBall:F3} rigid{ball.PhysicsPosition:F3} published{published:F3} palm{renderedPalm:F3} " +
                    $"wrist{visual.RightHand.position:F3} speed{athlete.Motor.Velocity:F3} physicsFacing{athlete.Motor.Facing:F3} " +
                    $"renderFacing{athlete.transform.forward:F3} error{Vector3.Distance(renderedBall, renderedPalm):F4}");
            }
        }

        void SetProfile(bool enabled)
        {
            options.Update();
            options.FindProperty("professionalThrows").boolValue = enabled;
            options.ApplyModifiedPropertiesWithoutUndo();
        }

        async Task Frame()
        {
            int frame = renderedFrame;
            await Until(() => renderedFrame > frame, "Game View stopped rendering.");
        }

        void Photograph(string name) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/" + name + ".png"));

        void FrameAthlete()
        {
            if (camera == null) return;
            var view = camera.viewpoints[2];
            Vector3 focus = athlete.transform.position + direction * .35f + Vector3.up * .11f;
            view.position = focus + direction * .95f - Vector3.forward * 1.65f + Vector3.up * .66f;
            view.LookAt(focus);
            camera.SetView(2);
        }

        async Task Possess(bool waitForGather = true)
        {
            athlete.Motor.ResetPose(origin, Quaternion.LookRotation(direction));
            ball.ResetForRestart(origin + direction * .85f);
            await Until(() => ball.Owner == athlete.Interaction, "The active player did not acquire the fixture ball.");
            if (waitForGather) await Until(() => !ball.IsGathering, "Gather did not complete.");
            input.enabled = false;
            athlete.Motor.SetMoveInput(Vector2.zero);
            FrameAthlete();
        }

        async Task ProbeThrow(string name, Func<bool> request, BallReleaseKind kind, bool catchPass = false)
        {
            int count = releaseCount;
            float requestedAt = Time.time;
            FrameAthlete();
            Require(request(), $"{name} request was rejected.");
            Require(ball.Owner == athlete.Interaction && ball.HasPendingThrow && releaseCount == count,
                $"{name} became physical before the preparation pose.");
            Require(!request(), $"{name} accepted a second pending throw.");
            int sample = 0;
            int gatherFrames = 0;
            var preparationFrames = new List<object>();
            while (ball.HasPendingThrow)
            {
                if (Time.time - requestedAt > 1f) throw new TimeoutException($"{name} did not reach release.");
                await Frame();
                FrameAthlete();
                float seconds = Time.time - requestedAt;
                bool gathering = ball.IsGathering;
                if (gathering) gatherFrames++;
                if (sample < 6)
                {
                    Photograph($"{name}-{sample:00}");
                    preparationFrames.Add(new { seconds, gathering, grip = Vector3.Distance(renderedBall, renderedPalm),
                        height = renderedBall.y, behindHead = Vector3.Dot(renderedBall - renderedHead, direction) });
                }
                sample++;
            }
            Require(releaseCount == count + 1 && releasedFromGrip && ball.ReleaseKind == kind,
                $"{name} must release once from the visual clock.");
            File.AppendAllText(Output + "/Release-Trace.txt",
                $"{name} dt {releaseTime - requestedAt:F3} from {releaseOrigin:F3}, gripError {releaseError:F5}, velocity {ball.Velocity:F3}\n");
            Require(releaseError < .012f && releaseOrigin.y > ball.SurfaceHeight + .07f,
                $"{name} release position diverged from the evaluated palm or was too low at {releaseOrigin}.");
            Require(Vector3.Dot(ball.Velocity.normalized, direction) > .7f, $"{name} launched away from the requested target.");
            throws.Add(new { name, seconds = releaseTime - requestedAt, gatherFrames,
                releaseX = releaseOrigin.x, releaseY = releaseOrigin.y, releaseZ = releaseOrigin.z, releaseError, preparationFrames });
            Photograph(name + "-Release");
            for (int i = 0; i < 3; i++) await Frame();
            Photograph(name + "-FollowThrough");
            if (catchPass)
                await Until(() => ball.Owner == receiver.Interaction, $"{name} missed the staged receiver.", 2.5f);
            else
                await Until(() => Time.time >= releaseTime + .24f, "Follow-through time did not advance.");
        }

        try
        {
            SetProfile(true);
            Require(visual.SupportsEvaluatedGrip, "The current Humanoid does not expose the grip bones.");
            foreach (var brain in brains) brain.enabled = false;
            foreach (var member in roster.Members.Where(member => member != athlete))
                member.Motor.ResetPose(new Vector3(member.Team == PrototypeTeam.Blue ? -7.5f : 7.5f, 0f,
                    2.5f + member.CapNumber), Quaternion.identity);
            selection.Select(athlete);
            ball.Released += Released;
            RenderPipelineManager.endCameraRendering += ReadRenderedState;
            File.WriteAllText(Output + "/Release-Trace.txt", "");

            await Possess();
            await Frame();
            Photograph("Low-Hold");
            float maxGripError = 0f;
            float leastHeadClearance = float.PositiveInfinity;
            int turningSamples = 0;
            foreach (float yaw in new[] { 90f, 180f, 270f, 360f, 450f, 330f, 440f })
            {
                Vector3 turningAim = Quaternion.AngleAxis(yaw, Vector3.up) * direction;
                turnProbe = yaw.ToString("F0");
                athlete.Motor.SetFacingDirection(turningAim);
                float turningDeadline = Time.time + 1f;
                do
                {
                    await Frame();
                    maxGripError = Mathf.Max(maxGripError, Vector3.Distance(renderedBall, renderedPalm));
                    leastHeadClearance = Mathf.Min(leastHeadClearance, Vector3.Distance(renderedBall, renderedHead));
                    turningSamples++;
                } while (Vector3.Dot(athlete.Motor.Facing, turningAim) < .999f && Time.time < turningDeadline);
                Require(Vector3.Dot(athlete.Motor.Facing, turningAim) >= .999f, "The fixture did not complete its turn.");
                Photograph("Hold-Turn-" + (int)yaw);
            }
            turnProbe = null;
            File.WriteAllLines(Output + "/Turn-Trace.txt", turnTrace);
            bool turningPassed = maxGripError < .012f && leastHeadClearance > ball.Radius + .11f;

            await Possess();
            Require(charge.BeginCharge(), "Could not begin the standard shot charge.");
            await Until(() => charge.NormalizedCharge > .72f, "Charge did not advance.");
            await Frame();
            float loadBehind = Vector3.Dot(renderedBall - renderedHead, direction);
            Require(loadBehind < -.09f, $"The ready ball was not behind the head, offset {loadBehind:F3} m.");
            Photograph("Rear-Loaded");
            await ProbeThrow("Standard-Shot", () => charge.IsCharging ? charge.ReleaseCharge() :
                athlete.Interaction.TryShoot(19f, ShotType.Powerful), BallReleaseKind.Shot);

            await Possess();
            athlete.Motor.SetMoveInput(Vector2.right);
            await Until(() => ball.IsDribbling, "Dribbling did not start for the quick shot.");
            await ProbeThrow("Quick-Shot", () => athlete.Interaction.TryShoot(12f, ShotType.Normal), BallReleaseKind.Shot);
            athlete.Motor.SetMoveInput(Vector2.zero);

            await Possess();
            Require(athlete.Interaction.TryShoot(16f, ShotType.Normal) && ball.HasPendingThrow, "Pause fixture could not queue.");
            int beforePause = releaseCount;
            match.SetPaused(true);
            await Frame();
            Require(!ball.HasPendingThrow && ball.Owner == athlete.Interaction, "Pause did not cancel preparation cleanly.");
            match.SetPaused(false);
            await Task.Delay(300);
            Require(releaseCount == beforePause, "A cancelled shot released on resume.");

            Require(athlete.Interaction.TryShoot(16f, ShotType.Normal), "Restart fixture could not queue.");
            ball.ResetForRestart(origin + Vector3.left * 3f);
            await Task.Delay(330);
            Require(!ball.HasPendingThrow && releaseCount == beforePause, "A reset ball completed the old throw.");

            await Possess();
            receiver.Motor.ResetPose(origin + direction * 4f, Quaternion.LookRotation(-direction));
            await ProbeThrow("Short-Pass", () => passing.TryPassTo(receiver), BallReleaseKind.Pass, true);
            await Possess();
            receiver.Motor.ResetPose(origin + direction * 8.8f, Quaternion.LookRotation(-direction));
            await ProbeThrow("Long-Pass", () => passing.TryPassTo(receiver), BallReleaseKind.Pass, true);
            await Possess();
            athlete.Motor.SetMoveInput(Vector2.right);
            await Until(() => ball.IsDribbling, "Dribbling did not start for the quick pass.");
            receiver.Motor.ResetPose(athlete.Motor.Position + direction * 4.2f, Quaternion.LookRotation(-direction));
            await ProbeThrow("Quick-Pass", () => passing.TryPassTo(receiver), BallReleaseKind.Pass, true);

            return new { passed = turningPassed, turningSamples, maxGripError, leastHeadClearance, loadBehind, releaseCount, throws, pauseCancellation = true,
                resetCancellation = true, physicalPassCatches = 3 };
        }
        finally
        {
            if (match.IsPaused) match.SetPaused(false);
            RenderPipelineManager.endCameraRendering -= ReadRenderedState;
            ball.Released -= Released;
            athlete.Motor.StopMotion();
            input.enabled = inputWasEnabled;
            if (camera != null) camera.SetView(-1);
            foreach (var brain in brains) brain.enabled = true;
            SetProfile(profileWasEnabled);
        }
    }

    private static void Require(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }

    private static async Task Until(Func<bool> predicate, string error, float timeout = 5f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup >= deadline) throw new TimeoutException(error);
            await Task.Delay(12);
        }
    }
}
