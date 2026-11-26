using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class BallPhysicsPolishVerification
{
    const string Output = "Logs/BallPhysicsPolish/";
    static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();
    static PrototypeMatch Match => One<PrototypeMatch>();
    static BallController Ball => One<BallController>();
    static PrototypeRoster Roster => One<PrototypeRoster>();
    static SwimmerIdentity Player(PrototypeTeam team) => Roster.FieldPlayers.First(p => p.Team == team);
    static PrototypeTeamTactics Tactics(PrototypeTeam team) =>
        Object.FindObjectsByType<PrototypeTeamTactics>().Single(t => t.Team == team);
    static PrototypeGoalkeeper Keeper(PrototypeTeam team) =>
        Object.FindObjectsByType<PrototypeGoalkeeper>(FindObjectsInactive.Include).Single(p => p.Identity.Team == team);
    static void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }

    static async Task Await(Func<bool> ready, string reason, double timeout = 10)
    {
        double deadline = EditorApplication.timeSinceStartup + timeout;
        while (!ready())
        {
            Check(EditorApplication.timeSinceStartup < deadline, $"{reason} ({Match.Phase}, {Ball.PhysicsPosition}, {Ball.Velocity})");
            await Task.Delay(12);
        }
    }

    static async Task Shot(string name)
    {
        ScreenCapture.CaptureScreenshot(Output + name + ".png");
        int frame = Time.frameCount;
        await Await(() => Time.frameCount > frame + 2, "Screenshot rendered");
    }

    static object Save(string name, object details)
    {
        File.WriteAllText(Output + name + ".json", JsonConvert.SerializeObject(details, Formatting.Indented));
        return details;
    }

    static void RouteInput()
    {
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(type).Focus();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }

    static async Task<PrototypeMatch> Begin()
    {
        RouteInput();
        var match = Match;
        var intro = One<PreMatchPresentation>();
        Check(intro != null && intro.Active && intro.CastCount == 14, "Production pre-match scene is active");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace));
            await Await(() => !intro.Active && match.IsPeriodSprint, "Actual keyboard skip releases ceremony");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        await Await(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Physical sprint acquisition");
        Check(match.SprintOff.Acquisitions == 1 && !match.GameplayView.CinematicOverride, "Exactly one opening possession, live camera restored");
        Park();
        return match;
    }

    static void Park()
    {
        foreach (var ai in Object.FindObjectsByType<PrototypeSwimmerAI>()) ai.enabled = false;
        foreach (var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>()) keeper.enabled = false;
        foreach (var p in Roster.Members)
        {
            float x = p.Team == PrototypeTeam.Blue ? -8 : 8;
            p.Motor.ResetPose(new Vector3(x, 0f, 2f * (p.CapNumber % 7 - 3)), Quaternion.identity);
        }
    }

    static void Claim(SwimmerIdentity player, Vector3 point, Vector3 aim)
    {
        Match.ShotClock.ResetPossession();
        player.Motor.ResetPose(point, Quaternion.LookRotation(aim));
        Ball.ResetForRestart(player.Motor.Position);
        Check(player.Interaction.TryCollect(), "Legal nearby fixture pickup");
        One<PlayerSelection>().Select(player);
    }

    static async Task<BallImpactEvent> ObserveImpact(Action launch, BallImpactKind kind, string image, bool requireAirborne)
    {
        var ball = Ball;
        BallImpactEvent? recorded = null;
        void Contact(BallImpactEvent e) { if (e.Kind == kind) recorded = e; }
        ball.Contact += Contact;
        try
        {
            launch();
            if (requireAirborne) await Await(() => ball.IsAirborne || recorded.HasValue, "Expected real ballistic flight", 2);
            await Await(() => recorded.HasValue, "Physical " + kind + " event for " + image, 4);
            await Shot(image);
            return recorded.Value;
        }
        finally { ball.Contact -= Contact; }
    }

    public static async Task<object> Water()
    {
        var match = await Begin();
        var ball = Ball;
        var blue = Player(PrototypeTeam.Blue);
        var lens = match.GameplayView.GetComponent<Camera>();
        match.GameplayView.enabled = false;
        void View(Vector3 focus)
        {
            Vector3 eye = focus + new Vector3(3.8f, 2.1f, -3.9f);
            lens.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye));
            lens.fieldOfView = 48;
        }

        try
        {
            await Task.Delay(600);
            Claim(blue, Vector3.zero, Vector3.forward);
            View(new Vector3(.3f, .10f, .45f));
            var charge = blue.GetComponent<ShotCharge>();
            Check(charge.BeginCharge(), "Raise the held ball before dropping");
            await Await(() => charge.NormalizedCharge > .62f && ball.PhysicsPosition.y > ball.SurfaceHeight + .15f,
                "Ball cleared the surface");
            int waterCount = 0;
            void CountWater(BallImpactEvent e) { if (e.Kind == BallImpactKind.Water) waterCount++; }
            ball.Contact += CountWater;
            BallImpactEvent drop;
            float drift;
            try
            {
                drop = await ObserveImpact(() =>
                {
                    ball.Drop(blue.Interaction);
                    blue.Motor.ResetPose(new Vector3(-1.3f, 0, -.7f), Quaternion.identity);
                }, BallImpactKind.Water, "01-Normal-Water-Landing", true);
                await Task.Delay(420);
                drift = Vector3.Distance(ball.PhysicsPosition, drop.Position);
                Check(!ball.IsAirborne && ball.WaterBounces == 0 && waterCount == 1 && Mathf.Abs(ball.PhysicsPosition.y - ball.SurfaceHeight) < .01f, "One splash and no vertical rebound");
                await Shot("02-Floating-Loose-Ball");
            }
            finally { ball.Contact -= CountWater; }

            Claim(blue, new Vector3(0, 0, -4), Vector3.forward);
            var receiver = Roster.FieldPlayers.First(p => p.Team == blue.Team && p != blue);
            receiver.Motor.ResetPose(new Vector3(0, 0, 2), Quaternion.identity);
            void LeaveReceivingLane(SwimmerBallInteraction player, BallReleaseKind kind)
            {
                if (player == blue.Interaction && kind == BallReleaseKind.Pass)
                    receiver.Motor.ResetPose(new Vector3(4, 0, 3), Quaternion.identity);
            }
            ball.Released += LeaveReceivingLane;
            BallImpactEvent dry;
            try
            {
                View(new Vector3(0, .2f, 2.3f));
                dry = await ObserveImpact(() => Check(blue.GetComponent<SwimmerPassing>().TryPassTo(receiver),
                    "Real aimed dry pass"), BallImpactKind.Water, "03-Missed-Dry-Pass", true);
            }
            finally { ball.Released -= LeaveReceivingLane; }
            Check(ball.Owner == null && !ball.IsAirborne && dry.Outgoing.magnitude <= 2.36f && dry.Outgoing.y == 0, "Missed catch settles with bounded drift");
            Check(dry.Outgoing.magnitude > .1f && ball.Velocity.magnitude < dry.Outgoing.magnitude, "Residual drift slows after landing");

            Claim(blue, new Vector3(0, 0, -4), Vector3.forward);
            View(new Vector3(0, .2f, 1.5f));
            var wet = await ObserveImpact(() => Check(ball.TryPass(blue.Interaction, Vector3.forward, 11f,
                null, .08f, 2.5f, PassDelivery.Wet), "Intentional wet pass"), BallImpactKind.Water, "04-Wet-Pass", true);
            Check(ball.Delivery == PassDelivery.Wet && wet.Outgoing.magnitude <= 1.91f && ball.Owner == null, "Wet splash and no mid-flight catch");

            Claim(blue, new Vector3(0, 0, -4), Vector3.forward);
            Check(ball.TryLob(blue.Interaction, Vector3.forward, 8f, 1.1f, null), "Lob pass was requested");
            await Await(() => ball.Owner == null && ball.Delivery == PassDelivery.Lob, "Lob pass released");
            await Await(() => ball.IsAirborne && ball.PhysicsPosition.y > 1f, "Distinct airborne lob arc", 2);
            await Await(() => !ball.IsAirborne, "Lob settles on water", 3);

            Claim(blue, Vector3.zero, Vector3.right);
            View(new Vector3(6.6f, .15f, -.25f));
            var ordinary = await ObserveImpact(() => Check(blue.Interaction.TryShoot(16f, ShotType.Normal), "Ordinary airborne shot"),
                BallImpactKind.Water, "04b-Normal-Shot-Landing", true);
            Check(ordinary.Outgoing.y == 0f && ordinary.Outgoing.magnitude <= 3.01f && ball.WaterBounces == 0,
                "Ordinary shot sheds downward velocity and shedding surface drag takes over");

            Vector3 forward = Tactics(blue.Team).AttackingGoal.transform.forward;
            Vector3 spot = Tactics(blue.Team).AttackingGoal.transform.position - forward * 8f + Vector3.right * 4f;
            Claim(blue, spot, forward);
            View(spot + forward * 3.8f);
            var skip = await ObserveImpact(() => Check(blue.Interaction.TryShoot(21, ShotType.Skip), "Physical skip release"),
                BallImpactKind.Skip, "05-Intentional-Skip", true);
            Check(ball.WaterBounces >= 1 && skip.Outgoing.y >= .6f &&
                Vector3.ProjectOnPlane(skip.Outgoing,Vector3.up).magnitude >= 15f &&
                skip.Outgoing.magnitude < skip.Incoming.magnitude, "First skip retained forward speed and lost total speed");
            await Await(() => !ball.IsAirborne || match.Rules.BoundaryRestart.HasValue, "Skip finishes as a real shot");
            int bounces = ball.WaterBounces;
            Check(bounces <= 2, "Production allows at most two qualifying skips");
            return Save("Water-PASS", new { dropIncomingY = drop.Incoming.y, dropOutgoingY = drop.Outgoing.y, dropDrift = drift, dropPoint = drop.Position.ToString(),
                dryLandingSpeed = dry.Outgoing.magnitude, wetLandingSpeed = wet.Outgoing.magnitude, ordinaryShotLandingSpeed = ordinary.Outgoing.magnitude, lob = true,
                skipUpward = skip.Outgoing.y, skipExitSpeed = skip.Outgoing.magnitude, skipBounces = bounces });
        }
        finally { match.GameplayView.enabled = true; match.GameplayView.SnapToTarget(); }
    }

    static async Task<BoundaryRestartAward> Exit(BoundaryRestartKind kind, PrototypeTeam awarded, float expectedClock,
        string image)
    {
        var match = Match;
        var ball = Ball;
        bool ready = false;
        bool correctOwner = false;
        void ObserveReady()
        {
            if (match.Phase != MatchPhase.FreeThrowReady) return;
            ready = true;
            correctOwner = ball.Owner == match.Rules.Taker.Interaction && match.Rules.Taker.Team == awarded &&
                !ball.AwaitingReplacement;
        }
        match.Changed += ObserveReady;
        try
        {
            await Await(() => match.Rules.BoundaryRestart.HasValue, "Boundary award " + image, 5);
            var restart = match.Rules.BoundaryRestart.Value;
            Check(restart.Kind == kind && restart.Team == awarded, $"Expected {kind} {awarded}, got {restart.Kind} {restart.Team}");
            Check(Mathf.Abs(match.ShotClock.Remaining - expectedClock) < .09f, $"Expected {expectedClock} restart seconds, got {match.ShotClock.Remaining}");
            Check(Object.FindObjectsByType<BallController>().Length == 1, "One authoritative ball remains");
            Check(match.Phase == MatchPhase.FreeThrowSetup && ball.AwaitingReplacement && ball.Owner == null &&
                !ball.GetComponent<SphereCollider>().enabled, "Escaped ball hidden, unowned and noninteractable");
            Check(!Player(awarded).Interaction.TryCollect(), "Cannot collect a retired ball");
            await Task.Delay(155);
            await Shot(image);
            await Await(() => ready, "Visible replacement is staged");
            Check(correctOwner, "Only the awarded taker holds the restored ball");
            return restart;
        }
        finally { match.Changed -= ObserveReady; }
    }

    static async Task PlayRestart()
    {
        var match = Match;
        var taker = match.Rules.Taker;
        bool blue = taker.Team == PrototypeTeam.Blue;
        if (blue)
        {
            var teammate = Roster.FieldPlayers.First(p => p.Team == taker.Team && p != taker);
            teammate.Motor.ResetPose(taker.Motor.Position + taker.Motor.Facing * 5f, Quaternion.LookRotation(-taker.Motor.Facing));
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                await Task.Delay(80);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
                await Await(() => match.IsPlaying && Ball.ReleaseKind == BallReleaseKind.Pass, "Real B input resumes boundary play", 3);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
        await Await(() => match.IsPlaying && Ball.ReleaseKind == BallReleaseKind.Pass, "Restart resumes with a physical pass", 9);
        Check(!match.Rules.BoundaryRestart.HasValue && !Ball.AwaitingReplacement, "Cleared the completed restart");
    }

    public static async Task<object> Boundaries()
    {
        var match = await Begin();
        var ball = Ball;
        match.GameplayView.SetView(MatchCameraView.SidelineBroadcast);
        var blue = Player(PrototypeTeam.Blue);
        var white = Player(PrototypeTeam.White);
        var pool = One<PoolBounds>();
        var results = new List<string>();
        foreach (int sign in new[] { -1, 1 })
        {
            Park();
            var passer = sign == -1 ? white : blue;
            Claim(passer, new Vector3(sign * 7.7f, 0, 0), Vector3.right * sign);
            Check(passer.Interaction.TryShoot(20f, ShotType.Normal), "Aim a real shot across the sideline");
            await Await(() => Mathf.Abs(ball.PhysicsPosition.x) >= pool.Width*.5f - .12f, "Whole-ball near sideline");
            if (sign == -1 && match.IsPlaying) await Shot("06-Sideline-Approach");
            var award = await Exit(BoundaryRestartKind.ThrowIn, sign == -1 ? PrototypeTeam.Blue : PrototypeTeam.White, 28,
                sign == -1 ? "07-Throw-In" : "08-Opposite-Throw-In");
            Check(Mathf.Abs(award.Mark.x - sign * 10f) < .01f && Mathf.Abs(award.Mark.z) < .6f, "Throw-in at the actual sideline exit");
            if (sign == -1)
            {
                float period = match.Clock.Remaining, shot = match.ShotClock.Remaining;
                match.SetPaused(true);
                await Task.Delay(250);
                Check(match.Clock.Remaining == period && match.ShotClock.Remaining == shot && ball.Owner == match.Rules.Taker.Interaction, "Both clocks and replacement stay put during pause");
                match.SetPaused(false);
                await Task.Delay(280);
                await Shot("09-Replacement-Ball-Ready");
            }
            await PlayRestart();
            results.Add(sign == -1 ? "left throw-in" : "right throw-in");
        }

        match.GameplayView.SetView(MatchCameraView.Lengthwise);
        foreach (var shooter in new[] { blue, white })
        {
            Park();
            var goal = Tactics(shooter.Team).AttackingGoal;
            Vector3 forward = goal.transform.forward;
            Claim(shooter, goal.transform.position - forward * 4f + Vector3.right * 5f, forward);
            Check(shooter.Interaction.TryShoot(21f, ShotType.Normal), "Physical wide shot beyond the posts");
            PrototypeTeam defender = shooter == blue ? PrototypeTeam.White : PrototypeTeam.Blue;
            var award = await Exit(BoundaryRestartKind.GoalThrow, defender, 28, "10-Goal-Throw-" + defender);
            Check(Mathf.Abs(goal.transform.InverseTransformPoint(award.Mark).z + 1.45f) < .01f, "Restarts inside the defending two-metre area");
            await PlayRestart();
            results.Add("goal throw " + defender);
        }

        Park();
        var opposite = Tactics(PrototypeTeam.Blue).AttackingGoal;
        var axis = opposite.transform.forward;
        Claim(blue, opposite.transform.position - axis * 5.5f, axis);
        Check(blue.Interaction.TryShoot(21f, ShotType.Normal), "Attacking shot for the save");
        await Await(() => ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Shot &&
            opposite.transform.InverseTransformPoint(ball.PhysicsPosition).z > -3.4f, "Released shot reaches the goal area");
        var defenderKeeper = Keeper(PrototypeTeam.White).Identity;
        Vector3 contact = ball.PhysicsPosition;
        Check(ball.TryDeflectContact(defenderKeeper.Interaction, contact, axis * 12f + Vector3.right * 15f),
            $"Controlled keeper deflection rejected ({match.Phase}, ball {ball.State} at {contact}, " +
            $"owner {ball.Owner}, keeper eligibility {defenderKeeper.EligibleForPlay})");
        var corner = await Exit(BoundaryRestartKind.CornerThrow, PrototypeTeam.Blue, 18, "11-Corner-Throw");
        Check(corner.Mark.x > 9.99f && Mathf.Abs(opposite.transform.InverseTransformPoint(corner.Mark).z + 2f) < .01f, "Nearest-side two-metre corner mark");
        await PlayRestart();
        Check(match.BlueScore == 0 && match.WhiteScore == 0, "Wide shots and restarts never score");
        return Save("Boundaries-PASS", new { results, keeperCorner = true, bothCameras = true, physicalPasses = true, cornerClock = 18, goals = 0 });
    }
    static async Task StrikeFieldPlayer(SwimmerIdentity victim, Vector3 incoming)
    {
        var ball = Ball;
        var body = ball.GetComponent<Rigidbody>();
        foreach (var other in Roster.Members)
            if (other != victim && other.Interaction != ball.LastReleaser)
                other.Motor.ResetPose(new Vector3(-7f, 0f, (other.CapNumber % 7) - 3f), Quaternion.identity);
        bool touched = false;
        var touchProperty = typeof(BallController).GetProperty("LastTouch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        void Impact()
        {
            var touch = touchProperty.GetValue(ball);
            var player = touch.GetType().GetField("Player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            touched |= (SwimmerIdentity)player.GetValue(touch) == victim;
        }
        ball.Impact += Impact;
        try
        {
            body.position = victim.Motor.Position - incoming * 1.35f + Vector3.up * .27f;
            body.linearVelocity = incoming * 13f;
            Physics.SyncTransforms();
            await Await(() => touched, "A real PhysX contact on the defending field player's collider", 2);
        }
        finally { ball.Impact -= Impact; }
    }

    static void ContinueDeflectedBall(SwimmerIdentity player, Vector3 outgoing)
    {
        var body = Ball.GetComponent<Rigidbody>();
        body.position = player.Motor.Position + outgoing * .68f + Vector3.up * .30f;
        body.linearVelocity = outgoing * 14f + Vector3.up * .4f;
    }

    static void EnableTeams()
    {
        foreach (var ai in Object.FindObjectsByType<PrototypeSwimmerAI>()) ai.enabled = true;
        foreach (var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>()) keeper.enabled = true;
    }

    public static async Task<object> DeflectionsAndHalfTime()
    {
        var match = await Begin();
        var ball = Ball;
        var blue = Player(PrototypeTeam.Blue);
        var white = Player(PrototypeTeam.White);

        Park();
        Claim(blue, Vector3.zero, Vector3.right);
        Check(blue.Interaction.TryShoot(16f, ShotType.Normal), "Physical ball approaching the sideline");
        var edgeBody = ball.GetComponent<Rigidbody>();
        edgeBody.position = new Vector3(10.06f, .44f, 0f);
        edgeBody.linearVelocity = Vector3.zero;
        await Task.Delay(110);
        Check(match.IsPlaying && !match.Rules.BoundaryRestart.HasValue && ball.PhysicsPosition.x > 10f,
            "Ball centre beyond paint with part of the ball still inside stays live");
        Claim(blue, Vector3.zero, Vector3.right);
        Check(blue.Interaction.TryShoot(18f, ShotType.Normal), "Open-play attacking shot");
        white.Motor.ResetPose(new Vector3(8.4f, 0, 3), Quaternion.identity);
        await StrikeFieldPlayer(white, Vector3.right);
        ContinueDeflectedBall(white, Vector3.right);
        await Exit(BoundaryRestartKind.ThrowIn, PrototypeTeam.White, 28, "12-Field-Block-Side");
        await PlayRestart();

        Park();
        var attacking = Tactics(PrototypeTeam.Blue).AttackingGoal;
        var attack = attacking.transform.forward;
        Claim(blue, attacking.transform.position - attack * 7f + Vector3.right * 4.8f, attack);
        Check(blue.Interaction.TryShoot(18f, ShotType.Normal), "Wide attacking shot for field block");
        white.Motor.ResetPose(attacking.transform.position - attack * 1.25f + Vector3.right * 4.8f, Quaternion.LookRotation(Vector3.Cross(Vector3.up, attack)));
        await StrikeFieldPlayer(white, attack);
        ContinueDeflectedBall(white, attack);
        await Exit(BoundaryRestartKind.GoalThrow, PrototypeTeam.White, 28, "13-Field-Block-End");
        await PlayRestart();

        Park();
        Claim(blue, Vector3.zero, Vector3.forward);
        match.AdvanceLiveTime(7f);
        Check(ball.TryPass(blue.Interaction, Vector3.forward, 10f, null, .08f, 2f), "A dry pass retains existing possession clock");
        white.Motor.ResetPose(new Vector3(8.4f, 0, 3), Quaternion.identity);
        await StrikeFieldPlayer(white, Vector3.right);
        float before = match.ShotClock.Remaining;
        ContinueDeflectedBall(white, Vector3.right);
        await Await(() => match.Rules.BoundaryRestart.HasValue, "Passive pass deflection leaves through the sideline");
        float remaining = match.ShotClock.Remaining;
        Check(remaining <= before && before - remaining < .4f && remaining < 22f, $"Same-team throw-in preserves the remaining clock ({before:F2} -> {remaining:F2}, {match.Rules.BoundaryRestart.Value.Team}, {ball.ReleaseKind})");
        await Exit(BoundaryRestartKind.ThrowIn, PrototypeTeam.Blue, remaining, "14-Retained-Possession");
        await PlayRestart();

        Park();
        Claim(blue, attacking.transform.position - attack * 6f, attack);
        Check(blue.Interaction.TryShoot(21f, ShotType.Normal), "Shot for goalkeeper's sideline block");
        await Await(() => attacking.transform.InverseTransformPoint(ball.PhysicsPosition).z > -2.4f,
            "Shot approaches the goalkeeper");
        Check(ball.TryDeflectContact(Keeper(PrototypeTeam.White).Identity.Interaction,
            ball.PhysicsPosition, Vector3.right * 19f + Vector3.up * 2.4f), "Explicit keeper play on the ball");
        await Exit(BoundaryRestartKind.ThrowIn, PrototypeTeam.Blue, 18, "15-Keeper-Block-Side");
        await PlayRestart();

        EnableTeams();
        var initial = Tactics(PrototypeTeam.Blue).AttackingGoal;
        for (int quarter = 1; quarter <= 2; quarter++)
        {
            match.ShotClock.ResetPossession();
            match.AdvanceLiveTime(match.Clock.Remaining);
            Check(match.Phase == (quarter == 1 ? MatchPhase.QuarterBreak : MatchPhase.HalfTime), "Quarter closes normally");
            match.ContinueMatch();
            await Await(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Physical opening pickup after interval");
        }
        Check(match.EndsSwitched && match.Clock.Quarter == 3 && match.SprintOff.Acquisitions == 3, "Halftime reversal survives all restart variants");
        var switched = Tactics(PrototypeTeam.Blue).AttackingGoal;
        Check(switched != initial, "Blue now attacks the opposite physical end");
        Park();
        Claim(blue, switched.transform.position - switched.transform.forward * 5 + Vector3.right * 4.5f, switched.transform.forward);
        Check(blue.Interaction.TryShoot(20f, ShotType.Normal), "Miss the opposite physical goal after halftime");
        var award = await Exit(BoundaryRestartKind.GoalThrow, PrototypeTeam.White, 28, "16-Reversed-Ends");
        Check(Mathf.Sign(award.ExitPoint.z) == Mathf.Sign(switched.transform.position.z), "Award mapped to new defended end");
        await PlayRestart();
        return Save("DeflectionsAndHalfTime-PASS", new
        {
            partialBallRemainsLive = true, actualFieldContact = true, fieldBlockSide = "defender throw-in", fieldBlockEnd = "goal throw",
            passivePassClock = remaining, keeperShotBlockClock = 18, endsSwitched = true, starts = match.SprintOff.Starts
        });
    }

    public static async Task<object> PenaltyExit()
    {
        var match = await Begin();
        var blue = Player(PrototypeTeam.Blue);
        var offender = Player(PrototypeTeam.White);
        var goal = Tactics(PrototypeTeam.Blue).AttackingGoal;
        var axis = goal.transform.forward;
        Vector3 spot = goal.transform.position - axis * 4f;
        Claim(blue, spot, axis);
        var charge = blue.GetComponent<ShotCharge>();
        Check(charge.BeginCharge(), "Shooting action in the penalty area");
        offender.Motor.ResetPose(spot - axis * .8f, Quaternion.LookRotation(axis));
        Check(match.Rules.ReportContact(offender, blue, IllegalContact.Push), "Real penalty foul");
        await Await(() => match.Phase == MatchPhase.PenaltyReady, "Penalty awarded after advantage");
        blue.Motor.ResetPose(goal.transform.position - axis * 5f, Quaternion.LookRotation((axis + goal.transform.right * .58f).normalized));
        Check(blue.Interaction.TryShoot(21f, ShotType.Normal) && match.Phase == MatchPhase.PenaltyFlight, "Five-metre penalty launched");
        Check(Mathf.Abs(goal.transform.InverseTransformPoint(Ball.ShotOrigin).z + 5f) < .02f, "Correct penalty line");
        await Exit(BoundaryRestartKind.GoalThrow, PrototypeTeam.White, 28f, "17-Penalty-Exit");
        await PlayRestart();
        Check(match.BlueScore == 0 && match.WhiteScore == 0 && !match.Rules.IsPenalty, "Wide penalty exits to ordinary play without a goal");
        return Save("PenaltyExit-PASS", new { physicalPenalty = true, goalThrow = "White", possessionSeconds = 28, score = match.BlueScore });
    }
}
