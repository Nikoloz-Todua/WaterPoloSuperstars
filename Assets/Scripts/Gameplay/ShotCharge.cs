using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Human charge commands, shared by keyboard/gamepad and future touch buttons. AI shoots directly.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerBallInteraction))]
    public sealed class ShotCharge : MonoBehaviour
    {
        [SerializeField] private PlayerSelection selection;
        [SerializeField, Min(0.05f)] private float chargeDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float minimumShotSpeed = 10f;
        [SerializeField, Min(0.1f)] private float maximumShotSpeed = 22f;
        private SwimmerBallInteraction interaction;
        private SwimmerIdentity identity;
        private float startedAt;
        public bool IsCharging { get; private set; }
        public float NormalizedCharge => IsCharging ? Mathf.Clamp01((Time.time - startedAt) / chargeDuration) : 0f;
        private bool CanCharge => selection != null && !selection.InputLocked && selection.ActivePlayer == identity && interaction.HasBall;

        private void Awake()
        {
            interaction = GetComponent<SwimmerBallInteraction>();
            identity = GetComponent<SwimmerIdentity>();
        }
        private void OnEnable() { if (interaction.Ball != null) interaction.Ball.PossessionChanged += OnPossessionChanged; }
        public bool BeginCharge()
        {
            if (IsCharging || !CanCharge) return false;
            startedAt = Time.time;
            IsCharging = true;
            return true;
        }
        public bool ReleaseCharge()
        {
            if (!IsCharging) return false;
            float speed = Mathf.Lerp(minimumShotSpeed, Mathf.Max(minimumShotSpeed, maximumShotSpeed), NormalizedCharge);
            bool allowed = CanCharge;
            CancelCharge(); // Clear before release emits a synchronous possession/selection change.
            return allowed && interaction.TryShoot(speed);
        }
        public void CancelCharge() => IsCharging = false;
        private void Update() { if (IsCharging && !CanCharge) CancelCharge(); }
        private void OnPossessionChanged(SwimmerBallInteraction previous, SwimmerBallInteraction next)
        { if (next != interaction) CancelCharge(); }
        private void OnDisable()
        {
            CancelCharge();
            if (interaction != null && interaction.Ball != null) interaction.Ball.PossessionChanged -= OnPossessionChanged;
        }
    }
}
