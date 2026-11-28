using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum BoundaryRestartKind { ThrowIn, GoalThrow, CornerThrow }

    public readonly struct BoundaryRestartAward
    {
        public readonly BoundaryRestartKind Kind;
        public readonly PrototypeTeam Team;
        public readonly Vector3 ExitPoint, Mark;

        internal BoundaryRestartAward(BoundaryRestartKind kind, PrototypeTeam team, Vector3 exitPoint, Vector3 mark)
        { Kind = kind; Team = team; ExitPoint = exitPoint; Mark = mark; }

        public string Caption => Kind switch
        {
            BoundaryRestartKind.GoalThrow => "GOAL THROW",
            BoundaryRestartKind.CornerThrow => "CORNER THROW",
            _ => "THROW IN"
        };
    }

    public sealed partial class MatchRules
    {
        public BoundaryRestartAward? BoundaryRestart { get; private set; }
        [SerializeField, Range(.25f, 1.5f)] private float boundaryFlightSeconds = .85f;
        public event Action<BoundaryRestartAward> BallReplacementRequested;

        internal bool AwardBoundaryRestart(FieldExit exit)
        {
            var touch = ball.LastTouch;
            var possession = match.ShotClock.PossessionTeam;
            var previousTeam = touch.Player != null ? touch.Player.Team : possession;
            bool shot = ball.ReleaseKind == BallReleaseKind.Shot;
            bool fromPenalty = match.Phase == MatchPhase.PenaltyFlight;
            bool keeperTouch = touch.Player != null && !touch.Player.IsFieldPlayer;
            var kind = BoundaryRestartKind.ThrowIn;
            var awarded = Other(previousTeam ?? selection.HumanTeam);
            Vector3 mark = pool.ClampPosition(exit.Point, 0f);
            if (exit.AcrossGoalLine)
            {
                var blueGoal = Team(PrototypeTeam.Blue).DefendedGoal;
                var whiteGoal = Team(PrototypeTeam.White).DefendedGoal;
                bool atBlue = Mathf.Abs(blueGoal.transform.position.z - exit.Point.z) <
                    Mathf.Abs(whiteGoal.transform.position.z - exit.Point.z);
                var defenders = atBlue ? PrototypeTeam.Blue : PrototypeTeam.White;
                var defendedGoal = atBlue ? blueGoal : whiteGoal;
                bool corner = previousTeam == defenders && touch.Player != null &&
                    (keeperTouch || touch.ControlledRelease || touch.BlockedShot || !shot && touch.Intentional);
                kind = corner ? BoundaryRestartKind.CornerThrow : BoundaryRestartKind.GoalThrow;
                awarded = corner ? Other(defenders) : defenders;
                // Use the same side as the exit. Put a goal-throw replacement just inside 2m.
                mark.z = defendedGoal.transform.position.z - defendedGoal.transform.forward.z * (corner ? 2f : 1.45f);
                if (corner)
                    mark.x = pool.transform.position.x + (exit.Point.x >= pool.transform.position.x ? 1f : -1f) * pool.Width * .5f;
            }
            else if (touch.BlockedShot && !keeperTouch && previousTeam.HasValue)
                awarded = previousTeam.Value;
            else if (!previousTeam.HasValue)
                awarded = NearestTaker(mark, null)?.Team ?? awarded;

            var boundaryTaker = NearestTaker(mark, awarded);
            if (boundaryTaker == null) return false;

            bool shortClock = kind == BoundaryRestartKind.CornerThrow || fromPenalty && awarded == taker?.Team ||
                kind == BoundaryRestartKind.ThrowIn && awarded == possession &&
                (touch.BlockedShot && keeperTouch || touch.Intentional && previousTeam != possession);
            bool fullClock = kind == BoundaryRestartKind.ThrowIn && shot;
            penalty = false;
            penaltyKeeper = null;
            taker = boundaryTaker;
            var restart = new BoundaryRestartAward(kind, awarded, exit.Point, mark);
            BoundaryRestart = restart;
            ball.RetireForReplacement();
            match.BeginBoundaryStoppage();
            match.ShotClock.AwardBoundaryRestart(awarded, shortClock, fullClock);
            if (kind != BoundaryRestartKind.CornerThrow || possession != awarded) ReleaseTeam(awarded);
            readyAt = match.PresentationTime + Mathf.Clamp(
                ball.IsAirborne ? boundaryFlightSeconds : boundaryFlightSeconds * .7f, .34f, 1.5f);
            Emit(RefereeDecision.BallOut, taker, awarded);
            BallReplacementRequested?.Invoke(restart);
            return true;
        }

        private SwimmerIdentity NearestTaker(Vector3 mark, PrototypeTeam? team)
        {
            SwimmerIdentity best = null;
            float distance = float.PositiveInfinity;
            foreach (var player in roster.Members)
            {
                if (!player.EligibleForPlay || !player.Interaction.isActiveAndEnabled ||
                    team.HasValue && player.Team != team) continue;
                float d = (player.Motor.Position - mark).sqrMagnitude;
                if (d < distance) { best = player; distance = d; }
            }
            return best;
        }

        private Vector3 RestartDirection(Vector3 mark)
        {
            var attack = Team(taker.Team).AttackDirection;
            if (!BoundaryRestart.HasValue || BoundaryRestart.Value.Kind == BoundaryRestartKind.GoalThrow)
                return attack;
            float inward = mark.x >= pool.transform.position.x ? -1f : 1f;
            return (attack + Vector3.right * inward).normalized;
        }

        private void ClearOffensiveGoalArea()
        {
            var goal = Team(taker.Team).AttackingGoal;
            foreach (var p in roster.Members)
            {
                if (p == taker || p.Team != taker.Team || !p.EligibleForPlay) continue;
                Vector3 point = goal.transform.InverseTransformPoint(p.Motor.Position);
                if (point.z <= -2f || Mathf.Abs(point.x) >= goal.Width * .5f + goal.PostThickness + 2f) continue;
                point.z = -2.15f;
                p.Motor.ResetPose(goal.transform.TransformPoint(point), Quaternion.LookRotation(goal.transform.forward));
            }
        }

        private static PrototypeTeam Other(PrototypeTeam team) =>
            team == PrototypeTeam.Blue ? PrototypeTeam.White : PrototypeTeam.Blue;

        private bool CanShootFromBoundary()
        {
            if (!BoundaryRestart.HasValue) return false;
            var restart = BoundaryRestart.Value;
            return restart.Kind != BoundaryRestartKind.ThrowIn ||
                match.PresentationTime <= readyAt + 1.5f &&
                Team(restart.Team).AttackingGoal.transform.InverseTransformPoint(restart.Mark).z <= -6f;
        }
    }
}
