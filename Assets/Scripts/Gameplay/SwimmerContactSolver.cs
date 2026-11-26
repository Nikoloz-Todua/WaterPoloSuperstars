using Unity.Profiling;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Resolves the roster's proposed movements in the water plane before PhysX moves the bodies.</summary>
    internal sealed class SwimmerContactSolver
    {
        private const float UprightRadius = .34f;
        private const float CrawlRadius = .31f;
        private const float CrawlTail = .73f;
        private const float CrawlNose = .14f;
        private const float MaximumCorrection = .17f;
        private const int Iterations = 3;
        private static readonly ProfilerMarker ContactWork = new ProfilerMarker("WaterPolo.SwimmerContact");
        private readonly Body[] bodies;
        private readonly PrototypeMatch match;

        private struct Body
        {
            public SwimmerMotor Motor;
            public Vector3 Prior, Desired, Resolved, Facing;
            public float Extension;
            public bool Participates;
        }

        internal SwimmerContactSolver(PrototypeRoster roster, SwimmerIdentity[] players)
        {
            bodies = new Body[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                bodies[i].Motor = players[i].Motor;
                bodies[i].Motor.UseRosterContact(roster);
            }
            if (players.Length > 0) match = players[0].Interaction.Ball.Match;
        }

        internal void Step()
        {
            bool contact = match != null && ((match.IsPlaying && !match.AwaitingOpeningPossession) ||
                match.Rules != null && match.Rules.OrdinaryFreeThrow);
            for (int i = 0; i < bodies.Length; i++)
            {
                ref Body actor = ref bodies[i];
                actor.Prior = actor.Motor.Position;
                actor.Facing = actor.Motor.Facing;
                actor.Participates = actor.Motor.isActiveAndEnabled && actor.Motor.PlanMovement(out actor.Desired);
                actor.Resolved = actor.Desired;
                float extension = contact && actor.Participates && actor.Motor.UsesProneContact ? 1f : 0f;
                actor.Extension = Mathf.MoveTowards(actor.Extension, extension, Time.fixedDeltaTime * 4f);
            }

            if (contact)
            {
                using (ContactWork.Auto())
                {
                    for (int pass = 0; pass < Iterations; pass++)
                    for (int i = 0; i + 1 < bodies.Length; i++)
                    {
                        if (!bodies[i].Participates) continue;
                        for (int j = i + 1; j < bodies.Length; j++)
                            if (bodies[j].Participates) Separate(ref bodies[i], ref bodies[j]);
                    }
                }
            }

            for (int i = 0; i < bodies.Length; i++)
                if (bodies[i].Participates) bodies[i].Motor.CommitMovement(bodies[i].Resolved);
        }

        private static void Separate(ref Body a, ref Body b)
        {
            Vector3 between = b.Resolved - a.Resolved;
            between.y = 0f;
            float centreDistanceSquared = between.sqrMagnitude;
            if (centreDistanceSquared > 4.6f) return;

            Vector3 aAxis = a.Facing * ((CrawlTail + CrawlNose) * a.Extension);
            Vector3 bAxis = b.Facing * ((CrawlTail + CrawlNose) * b.Extension);
            Vector3 startGap = between + a.Facing * (CrawlTail * a.Extension) - b.Facing * (CrawlTail * b.Extension);
            Vector3 gap = ClosestGap(startGap, aAxis, bAxis);
            float radii = Mathf.Lerp(UprightRadius, CrawlRadius, a.Extension) +
                Mathf.Lerp(UprightRadius, CrawlRadius, b.Extension);
            if (gap.sqrMagnitude >= radii * radii) return;

            float distance = gap.magnitude;
            Vector3 normal = distance > .001f ? gap / distance :
                centreDistanceSquared > .000001f ? between.normalized : a.Facing;
            float depth = Mathf.Min(.09f, radii - distance);
            float pushA = Mathf.Max(0f, Vector3.Dot(a.Desired - a.Prior, normal));
            float pushB = Mathf.Max(0f, Vector3.Dot(b.Prior - b.Desired, normal));
            float share = pushA + pushB > .001f ? Mathf.Lerp(.08f, .92f, pushA / (pushA + pushB)) : .5f;
            Push(ref a, -normal * (depth * share));
            Push(ref b, normal * (depth * (1f - share)));
        }

        private static void Push(ref Body actor, Vector3 correction)
        {
            Vector3 offset = Vector3.ClampMagnitude(actor.Resolved - actor.Desired + correction, MaximumCorrection);
            actor.Resolved = actor.Motor.ClampContactPosition(actor.Desired + offset);
        }

        // For A(t) and B(u) on the finite axes, minimize |startGap + u*b - t*a|.
        // Clamping u followed by one solve for t also covers either axis degenerating to a point.
        private static Vector3 ClosestGap(Vector3 startGap, Vector3 a, Vector3 b)
        {
            float aa = a.sqrMagnitude, bb = b.sqrMagnitude;
            float ag = Vector3.Dot(a, startGap), bg = Vector3.Dot(b, startGap), ab = Vector3.Dot(a, b);
            float determinant = aa * bb - ab * ab;
            float t = determinant > .000001f ? Mathf.Clamp01((bb * ag - ab * bg) / determinant) : 0f;
            float u = bb > .000001f ? Mathf.Clamp01((ab * t - bg) / bb) : 0f;
            if (aa > .000001f) t = Mathf.Clamp01((ag + ab * u) / aa);
            if (bb > .000001f) u = Mathf.Clamp01((ab * t - bg) / bb);
            return startGap + b * u - a * t;
        }
    }
}
