using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Pure, bounded candidate scoring. Higher scores mean a more useful immediate intervention.</summary>
    public static class ContextualSelection
    {
        public static float Score(SwimmerIdentity candidate, PrototypeTeamTactics team)
        {
            var ball = team.Ball;
            var carrier = team.Carrier;
            Vector3 point = carrier != null ? carrier.Motor.Position : ball.IsAirborne ? ball.LandingPoint : ball.PhysicsPosition;
            Vector3 delta = PrototypeTeamTactics.Planar(point - candidate.Motor.Position);
            float arrival = delta.magnitude / Mathf.Max(0.1f, candidate.Motor.MaxSpeed);
            float momentum = delta.sqrMagnitude > 0.01f ? Vector3.Dot(candidate.Motor.Velocity, delta.normalized) * 0.12f : 0f;
            if (carrier == null)
                return -arrival + momentum + (team.LoosePursuer == candidate ? 0.35f : 0f) +
                    (ball.IntendedReceiver == candidate.Interaction ? 0.6f : 0f);
            if (carrier.Team != candidate.Team)
            {
                Vector3 toGoal = PrototypeTeamTactics.Planar(team.DefendedGoal.transform.position - point).normalized;
                float goalSide = Vector3.Dot(candidate.Motor.Position - point, toGoal);
                float score = -arrival + momentum + Mathf.Clamp(goalSide, -3f, 3f) * 0.2f;
                if (team.Presser == candidate) score += 0.65f;
                else if (team.Presser != null && Vector3.Distance(team.Presser.Motor.Position, point) < 2f)
                    score += team.LaneClear(point, candidate.Motor.Position) ? 0.1f : 0.35f;
                return score;
            }
            // Off-ball support: reachable, open, progressive options. Selection never transfers possession.
            if (candidate == carrier) return -2f;
            float progress = Vector3.Dot(candidate.Motor.Position - point, team.AttackDirection);
            float shape = PrototypeTeamTactics.Planar(team.Support(candidate, carrier) - candidate.Motor.Position).magnitude;
            return -arrival * 0.45f + Mathf.Clamp(progress, -5f, 6f) * 0.15f - team.Pressure(candidate.Motor.Position) +
                (team.LaneClear(point, candidate.Motor.Position) ? 0.5f : -0.5f) - shape * 0.025f;
        }
    }
}
