using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum StealResult { Unavailable, Cooldown, NoOpponent, OutOfRange, WrongFacing, Protected, Shielded, Success }

    /// <summary>Same command for human/touch/AI. The ball resolves every ownership change atomically.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerIdentity))]
    public sealed class SwimmerSteal : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PlayerSelection selection;
        [SerializeField, Min(0.5f)] private float reach = 1.8f;
        [SerializeField, Range(0f, 180f)] private float facingAngle = 65f;
        [SerializeField, Min(0.1f)] private float cooldown = 1.6f;
        [SerializeField, Min(0f)] private float acquisitionProtection = 1f;
        [Tooltip("Carrier facing dot toward attacker. Rear approaches below this value are shielded.")]
        [SerializeField, Range(-1f, 1f)] private float minimumExposure = -0.25f;
        private SwimmerIdentity identity;
        public float NextAttemptAt { get; private set; }
        public bool CanAttempt => Time.time >= NextAttemptAt;
        public StealResult LastResult { get; private set; }
        public float Reach => reach;
        public float Cooldown => cooldown;

        private void Awake()
        {
            identity = GetComponent<SwimmerIdentity>();
            if (roster == null || match == null || selection == null)
            { Debug.LogError("SwimmerSteal requires roster, match and selection references.", this); enabled = false; }
        }
        private void OnEnable() { if (match != null) match.Changed += ResetAttempt; }
        private void OnDisable() { if (match != null) match.Changed -= ResetAttempt; }
        private void ResetAttempt() { NextAttemptAt = 0f; LastResult = StealResult.Unavailable; }

        public StealResult TrySteal()
        {
            if (!isActiveAndEnabled || identity == null || !identity.Motor.enabled || match.IsRestarting || selection.InputLocked)
                return LastResult = StealResult.Unavailable;
            if (!CanAttempt) return LastResult = StealResult.Cooldown;
            // A valid attempt consumes its cooldown even if facing/range/protection rejects it.
            NextAttemptAt = Time.time + cooldown;
            var ball = identity.Interaction.Ball;
            var carrier = ball.Owner != null ? ball.Owner.GetComponent<SwimmerIdentity>() : null;
            if (!roster.Contains(identity) || carrier == null || !roster.Contains(carrier)) return LastResult = StealResult.NoOpponent;
            return LastResult = ball.TrySteal(identity.Interaction, reach, Mathf.Cos(facingAngle * Mathf.Deg2Rad), acquisitionProtection, minimumExposure);
        }
    }
}
