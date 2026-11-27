using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Observes actual ball/hand travel before score evaluation. No trigger timing or goal-wide collision wall.</summary>
    [DefaultExecutionOrder(-120), DisallowMultipleComponent]
    public sealed class GoalkeeperInterception : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeGoalkeeper[] keepers;
        private Vector3 previousBall;
        private Vector3[] previousHands;
        private Vector3[] previousLeft, previousRight;
        private bool[] previousPalms;
        private bool[] touching;
        private int previousMotion;

        private void Awake()
        {
            if (ball == null || match == null || keepers == null || keepers.Length == 0)
            { Debug.LogError("GoalkeeperInterception requires ball, match and keeper references.", this); enabled = false; return; }
            previousHands = new Vector3[keepers.Length]; touching = new bool[keepers.Length]; Sample();
            previousLeft = new Vector3[keepers.Length];
            previousRight = new Vector3[keepers.Length];
            previousPalms = new bool[keepers.Length];
        }
        private void OnEnable() { if (match != null) match.Changed += ResetContacts; }
        private void OnDisable() { if (match != null) match.Changed -= ResetContacts; }
        private void ResetContacts()
        {
            if (touching == null) return;
            System.Array.Clear(touching, 0, touching.Length); Sample();
        }
        private void Sample()
        {
            previousBall = ball.PhysicsPosition; previousMotion = ball.MotionRevision;
            for (int i = 0; i < keepers.Length; i++)
            {
                if (keepers[i] == null) continue;
                previousHands[i] = keepers[i].HandPosition;
                if (previousPalms != null)
                    previousPalms[i] = keepers[i].TryReadPalms(out previousLeft[i], out previousRight[i]);
            }
        }
        private void FixedUpdate()
        {
            Vector3 current = ball.PhysicsPosition;
            Vector3 from = previousMotion == ball.MotionRevision ? previousBall : ball.MotionOrigin;
            if (match.BallInMotion && ball.Owner == null)
            {
                // Two opposite goal areas do not overlap; resolve at most one confirmed contact per step.
                for (int i = 0; i < keepers.Length; i++)
                {
                    var keeper = keepers[i]; if (keeper == null || !keeper.isActiveAndEnabled) continue;
                    if (keeper.UsesAnimatedHands)
                    {
                        if (TryAnimatedContact(i, keeper, from, current)) break;
                        continue;
                    }
                    Vector3 hand = keeper.HandPosition;
                    float radius = keeper.SaveReach + ball.Radius;
                    if (touching[i])
                    {
                        // A settled loose ball can be collected without rerolling a previously missed fast save.
                        if ((current - hand).sqrMagnitude <= radius * radius && ball.CanCollect && ball.Velocity.sqrMagnitude < 6.25f && keeper.ResolveContact(current, hand, ball.Velocity)) break;
                        if ((current - hand).sqrMagnitude > (radius + 0.1f) * (radius + 0.1f)) touching[i] = false;
                        continue;
                    }
                    if (!GoalkeeperPositioning.SweptContact(from, current, previousHands[i], hand, radius, out float fraction)) continue;
                    Vector3 contact = Vector3.Lerp(from, current, fraction);
                    // A ball already across the whole-ball scoring plane cannot be rescued retroactively.
                    if (keeper.DefendedGoal.transform.InverseTransformPoint(contact).z >= 0f || keeper.DefendedGoal.TryGetCrossing(from, contact, ball.Radius, out _)) continue;
                    touching[i] = true;
                    if (keeper.ResolveContact(contact, Vector3.Lerp(previousHands[i], hand, fraction), ball.Velocity)) break;
                }
            }
            Sample();
        }

        private bool TryAnimatedContact(int index, PrototypeGoalkeeper keeper, Vector3 from, Vector3 current)
        {
            if (!keeper.CanContactWithPalm(out bool shot) || !keeper.TryReadPalms(out var left, out var right)) return false;
            Vector3 leftStart = previousPalms[index] ? previousLeft[index] : left;
            Vector3 rightStart = previousPalms[index] ? previousRight[index] : right;
            // The ball sphere and a small palm pad are sampled on the visible rig.
            float radius = ball.Radius + .033f;
            bool hitLeft = GoalkeeperPositioning.SweptContact(from, current, leftStart, left, radius, out float leftTime);
            bool hitRight = GoalkeeperPositioning.SweptContact(from, current, rightStart, right, radius, out float rightTime);
            if (!hitLeft && !hitRight) return false;
            bool useRight = hitRight && (!hitLeft || rightTime <= leftTime);
            float time = useRight ? rightTime : leftTime;
            Vector3 contact = Vector3.Lerp(from, current, time);
            if (keeper.DefendedGoal.transform.InverseTransformPoint(contact).z >= 0f ||
                keeper.DefendedGoal.TryGetCrossing(from, contact, ball.Radius, out _)) return false;
            Vector3 palm = useRight ? Vector3.Lerp(rightStart, right, time) : Vector3.Lerp(leftStart, left, time);
            return keeper.ResolvePalmContact(contact, palm, ball.Velocity, useRight, shot);
        }
    }
}
