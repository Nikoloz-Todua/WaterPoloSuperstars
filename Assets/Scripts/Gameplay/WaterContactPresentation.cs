using Unity.Profiling;
using UnityEngine;
using ToduStudios.WaterPolo.WaterVisuals;

namespace ToduStudios.WaterPolo
{
    internal enum WaterContactKind { Entry, Exit, Pull, Kick, Wake, Tread, Bow, Impact, SkipImpact, Turn, Keeper, Gather, Release, Celebration }

    internal readonly struct WaterContactStamp
    {
        internal readonly float Time, Strength, Radius;
        internal readonly Vector3 Point, Velocity;
        internal readonly WaterContactKind Kind;

        internal WaterContactStamp(float time, WaterContactKind kind, Vector3 point, Vector3 velocity, float strength, float radius)
        {
            Time = time;
            Kind = kind;
            Point = point;
            Velocity = velocity;
            Strength = strength;
            Radius = radius;
        }
    }

    /// <summary>Samples final animated contacts and stores presentation-only events on the match clock.</summary>
    internal sealed class WaterContactPresentation
    {
        private const int HistoryCapacity = 1536;
        private const float RearmHeight = .047f;
        private static readonly ProfilerMarker SampleMarker = new("WPS.Water.SampleContacts");
        private static readonly ProfilerMarker RenderMarker = new("WPS.Water.BuildContactMesh");

        private struct Hand
        {
            public Vector3 Previous;
            public float NextEntry, NextExit, NextPull;
            public bool Armed, Wet;
        }

        private sealed class Athlete
        {
            public SwimmerIdentity Identity;
            public SwimmerStamina Stamina;
            public RiggedSwimmerVisual Visual;
            public Transform Left, Right, LeftFoot, RightFoot, Chest;
            public Hand LeftState, RightState;
            public Vector3 PreviousRoot, PreviousMotion;
            public float PreviousChest, NextWake, NextKick, NextTread, NextKeeper, NextTurn;
            public bool Valid;
            public int Foot;
        }

        private readonly WaterContactStamp[] history = new WaterContactStamp[HistoryCapacity];
        private readonly int[] totals = new int[(int)WaterContactKind.Celebration + 1];
        private readonly BallController ball;
        private readonly PrototypeMatch match;
        private readonly PoolBounds pool;
        private readonly PrototypeRoster roster;
        private readonly WaterContactMesh mesh;
        private Athlete[] athletes;
        private int written, count;
        private float lastSample, visibleSince, playbackTime, nextBow;
        private bool wasDribbling, playback;
        private SwimmerBallInteraction lastOwner;
        private SwimmerMotor leadingMotor;
        private Vector3 lastBall;
        private MatchPhase phase;
        private Camera camera;
        internal int LeftEntries { get; private set; }
        internal int RightEntries { get; private set; }
        internal int SurfaceCount => mesh.SurfaceCount;
        internal int ParticleCount => mesh.ParticleCount;
        internal int ImpactSurfaceCount => mesh.ImpactSurfaceCount;
        internal int ImpactCount => totals[(int)WaterContactKind.Impact] + totals[(int)WaterContactKind.SkipImpact];
        internal int WakeCount => totals[(int)WaterContactKind.Wake];
        internal int TreadCount => totals[(int)WaterContactKind.Tread];
        internal int HistoryCount => count;
        internal bool IsPlayback => playback;
        internal WaterContactStamp LatestImpact { get; private set; }
        internal WaterContactStamp LatestEntry { get; private set; }

        internal WaterContactPresentation(Transform parent, BallController ball, PrototypeMatch match, PrototypeRoster roster,
            PoolBounds pool, Material material)
        {
            this.ball = ball;
            this.match = match;
            this.roster = roster;
            this.pool = pool;
            phase = match.Phase;
            mesh = new WaterContactMesh(parent, material);
        }

        internal void CacheAthletes()
        {
            if (athletes != null) return;
            athletes = new Athlete[roster.Members.Count];
            for (int i = 0; i < athletes.Length; i++)
            {
                var player = roster.Members[i];
                var visual = player.GetComponentInChildren<RiggedSwimmerVisual>(true);
                var animator = visual != null ? visual.GetComponent<Animator>() : null;
                if (animator == null || !animator.isHuman || !animator.isInitialized) continue;
                athletes[i] = new Athlete
                {
                    Identity = player, Visual = visual, Stamina = player.GetComponent<SwimmerStamina>(),
                    Left = animator.GetBoneTransform(HumanBodyBones.LeftHand),
                    Right = animator.GetBoneTransform(HumanBodyBones.RightHand),
                    LeftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot),
                    RightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot),
                    Chest = animator.GetBoneTransform(HumanBodyBones.Chest),
                };
            }
        }

        internal void Tick(WaterEffectsQuality quality, int surfaceBudget, int particleBudget, float range)
        {
            if (quality == WaterEffectsQuality.Off) { mesh.Clear(); return; }
            if (match.IsPaused) return;
            if (camera == null) camera = match.GameplayView != null ? match.GameplayView.GetComponent<Camera>() : Camera.main;
            if (camera == null) return;
            float now = match.PresentationTime;
            float dt = now - lastSample;
            lastSample = now;
            if (!playback && dt > 0f && (match.CanSwim || phase == MatchPhase.PenaltyReady || phase == MatchPhase.PenaltyFlight))
            {
                using (SampleMarker.Auto())
                {
                    CacheAthletes();
                    foreach (var athlete in athletes)
                        if (athlete != null) SampleAthlete(athlete, now, dt);
                    SampleBall(now);
                }
            }
            using (RenderMarker.Auto()) Draw(playback ? playbackTime : now, quality, surfaceBudget, particleBudget, range);
        }

        private void SampleAthlete(Athlete athlete, float now, float dt)
        {
            if (!athlete.Identity.EligibleForPlay || !athlete.Visual.isActiveAndEnabled)
            {
                athlete.Valid = false;
                return;
            }
            Vector3 root = athlete.Identity.Motor.Position;
            bool reset = !athlete.Valid || dt > .20f || (root - athlete.PreviousRoot).sqrMagnitude > 2.25f;
            float waterline = pool.Waterline;
            Vector3 motion = athlete.Identity.Motor.Velocity;
            motion.y = 0f;
            float speed = motion.magnitude;
            bool sprint = athlete.Stamina != null && athlete.Stamina.IsSprinting;
            bool stroke = athlete.Visual.Locomotion > .55f;
            SampleHand(athlete.Left, ref athlete.LeftState, true, now, dt, waterline, sprint, stroke, reset);
            SampleHand(athlete.Right, ref athlete.RightState, false, now, dt, waterline, sprint, stroke, reset);

            Vector3 change = motion - athlete.PreviousMotion;
            float turbulence = !reset ? Mathf.InverseLerp(3.2f, 12f, change.magnitude / dt) *
                Mathf.InverseLerp(.65f, 3f, athlete.PreviousMotion.magnitude) : 0f;
            if (turbulence > .25f && now >= athlete.NextTurn)
            {
                Add(now, WaterContactKind.Turn, root - athlete.PreviousMotion.normalized * .47f, -change,
                    turbulence * (sprint ? 1f : .73f), .42f + turbulence * .32f);
                athlete.NextTurn = now + .24f;
            }

            if (speed > .48f && now >= athlete.NextWake)
            {
                Vector3 forward = motion / speed;
                Vector3 path = Vector3.Lerp(athlete.PreviousMotion, motion, .64f);
                Add(now, WaterContactKind.Wake, root - forward * (sprint ? .86f : .66f), reset ? motion : path,
                    sprint ? .98f : Mathf.Lerp(.39f, .66f, speed / 5f), sprint ? .91f : .71f);
                athlete.NextWake = now + (sprint ? .105f : .18f);
            }
            else if (!reset && speed < .42f && now >= athlete.NextTread)
            {
                Transform scull = (athlete.Foot & 1) == 0 ? athlete.Left : athlete.Right;
                if (scull != null && Mathf.Abs(scull.position.y - waterline) < .12f)
                {
                    Vector3 wash = Vector3.ProjectOnPlane(scull.position - root, Vector3.up);
                    Add(now, WaterContactKind.Tread, scull.position, wash, .20f, .31f);
                    athlete.Foot++;
                    athlete.NextTread = now + .58f;
                }
            }

            if (speed > .65f && now >= athlete.NextKick)
            {
                var foot = (athlete.Foot++ & 1) == 0 ? athlete.LeftFoot : athlete.RightFoot;
                if (foot != null && foot.position.y > waterline - .56f && foot.position.y < waterline + .11f)
                {
                    float depthFactor = Mathf.InverseLerp(-.56f, -.08f, foot.position.y - waterline);
                    Add(now, WaterContactKind.Kick, foot.position, motion,
                        (.17f + .37f * depthFactor) * (sprint ? 1.18f : .75f), .35f);
                }
                athlete.NextKick = now + (sprint ? .14f : .24f);
            }

            if (!athlete.Identity.IsFieldPlayer && athlete.Chest != null)
            {
                Vector3 chest = athlete.Chest.position;
                float vertical = (chest.y - athlete.PreviousChest) / dt;
                if (!reset && Mathf.Abs(vertical) > .54f && now >= athlete.NextKeeper && chest.y > waterline - .17f)
                {
                    Add(now, WaterContactKind.Keeper, chest, motion + Vector3.up * vertical,
                        Mathf.Clamp01(.23f + Mathf.Abs(vertical) * .3f), .8f);
                    athlete.NextKeeper = now + .38f;
                }
                athlete.PreviousChest = chest.y;
            }
            athlete.PreviousRoot = root;
            athlete.PreviousMotion = motion;
            athlete.Valid = true;
        }

        private void SampleHand(Transform hand, ref Hand state, bool left, float now, float dt, float line,
            bool sprint, bool stroke, bool reset)
        {
            if (hand == null) return;
            Vector3 point = hand.position;
            float height = point.y - line;
            Vector3 previous = state.Previous;
            state.Previous = point;
            if (reset)
            {
                state = new Hand { Previous = point, Armed = height >= RearmHeight, NextPull = now + .12f };
                return;
            }
            if (height >= RearmHeight) state.Armed = true;
            Vector3 velocity = (point - previous) / dt;
            if (state.Armed && previous.y >= line && height < 0f && velocity.y < -.19f)
            {
                state.Armed = false;
                state.Wet = true;
                if (now >= state.NextEntry)
                {
                    float strength = Mathf.Clamp(.20f + -velocity.y * .18f + (sprint ? .18f : 0f),
                        stroke ? .45f : .25f, stroke ? 1f : .68f);
                    LatestEntry = Add(now, WaterContactKind.Entry, SurfaceCrossing(previous, point, line),
                        velocity, strength, stroke ? .49f + strength * .15f : .34f);
                    state.NextEntry = now + .24f;
                    if (left) LeftEntries++; else RightEntries++;
                }
            }
            else if (state.Wet && previous.y <= line && height > 0f && velocity.y > .30f && now >= state.NextExit)
            {
                state.Wet = false;
                state.NextExit = now + .28f;
                Add(now, WaterContactKind.Exit, SurfaceCrossing(previous, point, line), velocity, sprint ? .46f : .28f, .33f);
            }
            else if (height < -.03f && height > -.21f && velocity.sqrMagnitude > 2.2f && now >= state.NextPull)
            {
                Add(now, WaterContactKind.Pull, point, velocity, sprint ? .30f : .18f, .38f);
                state.NextPull = now + .36f;
            }
        }

        internal static Vector3 SurfaceCrossing(Vector3 from, Vector3 to, float line)
            => Vector3.Lerp(from, to, Mathf.InverseLerp(from.y, to.y, line));

        private void SampleBall(float now)
        {
            Vector3 point = ball.transform.position;
            var owner = ball.Owner;
            if (owner != lastOwner) leadingMotor = owner != null ? owner.GetComponent<SwimmerMotor>() : null;
            if (wasDribbling && !ball.IsDribbling && owner != null && owner == lastOwner)
                Add(now, WaterContactKind.Gather, lastBall, lastBall - point, .40f, .42f);
            if (ball.IsDribbling && now >= nextBow && leadingMotor != null)
            {
                var motion = leadingMotor.Velocity;
                if (motion.sqrMagnitude > .16f)
                    Add(now, WaterContactKind.Bow, point, motion, .40f, .36f);
                nextBow = now + .115f;
            }
            lastBall = point;
            lastOwner = owner;
            wasDribbling = ball.IsDribbling;
        }

        internal void Contact(BallImpactEvent impact)
        {
            if (match.IsPaused || playback) return;
            if (impact.Kind == BallImpactKind.Water || impact.Kind == BallImpactKind.Skip)
            {
                bool skip = impact.Kind == BallImpactKind.Skip;
                float impulse = Mathf.Max(0f, -impact.Incoming.y) + .22f * Vector3.ProjectOnPlane(impact.Incoming, Vector3.up).magnitude;
                float strength = Mathf.Clamp(.22f + impulse * .115f, .24f, 1f);
                LatestImpact = Add(match.PresentationTime, skip ? WaterContactKind.SkipImpact : WaterContactKind.Impact,
                    impact.Position, skip ? impact.Outgoing : impact.Incoming, strength,
                    Mathf.Lerp(.46f, 1.10f, strength) * ball.Radius / .14f);
            }
            else if (impact.Kind == BallImpactKind.Save && Mathf.Abs(impact.Position.y - pool.Waterline) < 1.05f)
                Add(match.PresentationTime, WaterContactKind.Keeper, impact.Position, impact.Incoming, .38f + .55f * impact.Intensity, .68f);
        }

        internal void Release(BallReleaseKind kind)
        {
            if (!match.IsPaused && !playback && (kind == BallReleaseKind.Shot || kind == BallReleaseKind.Pass || kind == BallReleaseKind.Lob))
                Add(match.PresentationTime, WaterContactKind.Release, ball.PhysicsPosition, ball.Velocity, kind == BallReleaseKind.Pass ? .43f : .73f, .45f);
        }

        private WaterContactStamp Add(float now, WaterContactKind kind, Vector3 point, Vector3 velocity, float strength, float radius)
        {
            if (kind != WaterContactKind.Release) point.y = pool.Waterline;
            var stamp = new WaterContactStamp(now, kind, point, velocity, strength, radius);
            history[written++ % HistoryCapacity] = stamp;
            count = Mathf.Min(count + 1, HistoryCapacity);
            totals[(int)kind]++;
            return stamp;
        }

        internal void ExternalSurface(Vector3 point, float strength, bool splash)
        {
            if (!match.IsPaused && !playback)
                Add(match.PresentationTime, splash ? WaterContactKind.Celebration : WaterContactKind.Tread,
                    point, Vector3.zero, strength, .6f);
        }

        internal void SetPlayback(float time)
        {
            playback = true;
            playbackTime = time;
        }

        internal void EndPlayback()
        {
            playback = false;
            Clear();
        }

        internal void PhaseChanged()
        {
            if (phase == match.Phase) return;
            phase = match.Phase;
            visibleSince = match.PresentationTime;
            ResetSampling();
            if (!playback) mesh.Clear();
        }

        internal void Clear()
        {
            mesh.Clear();
            visibleSince = match.PresentationTime;
            written = count = 0;
            ResetSampling();
        }

        private void ResetSampling()
        {
            wasDribbling = false;
            nextBow = 0f;
            if (athletes != null)
                foreach (var athlete in athletes)
                    if (athlete != null)
                    {
                        athlete.Valid = false;
                        athlete.NextWake = athlete.NextKick = athlete.NextTread = athlete.NextKeeper = athlete.NextTurn = 0f;
                    }
        }

        private void Draw(float now, WaterEffectsQuality quality, int surfaceBudget, int particleBudget, float range)
        {
            mesh.Begin(camera, surfaceBudget, particleBudget, range, pool.Waterline, quality);
            float start = playback ? now - WaterContactMesh.LongestLifetime : Mathf.Max(visibleSince, now - WaterContactMesh.LongestLifetime);
            int min = written - count, max = written;
            while (min < max)
            {
                int mid = min + (max - min) / 2;
                if (history[mid % HistoryCapacity].Time <= now) min = mid + 1; else max = mid;
            }
            int end = min;
            for (int pass = 0; pass < 2; pass++)
            for (int i = end - 1; i >= written - count; i--)
            {
                var stamp = history[i % HistoryCapacity];
                if (stamp.Time < start) break;
                bool priority = stamp.Kind == WaterContactKind.Impact || stamp.Kind == WaterContactKind.SkipImpact ||
                    stamp.Kind == WaterContactKind.Keeper || stamp.Kind == WaterContactKind.Entry;
                if (priority == (pass == 0)) mesh.Append(stamp, i, now - stamp.Time);
            }
            mesh.Flush();
        }

        internal void Dispose() => mesh.Dispose();
    }
}
