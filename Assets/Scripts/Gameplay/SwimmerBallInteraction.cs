using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Commands callable by human input, future touch controls or AI; no movement code.</summary>
    [DisallowMultipleComponent]
    public sealed class SwimmerBallInteraction : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private Transform carryAnchor;
        [SerializeField, Min(0.1f)] private float pickupDistance = 2f;
        [SerializeField, Min(0f)] private float shotSpeed = 12f;
        [Tooltip("Distance from swimmer root, beyond the 1.1m capsule plus ball radius.")]
        [SerializeField, Min(0.1f)] private float releaseDistance = 1.5f;

        public Transform CarryAnchor => carryAnchor;
        public float PickupDistance => pickupDistance;
        public float ReleaseDistance => releaseDistance;
        public bool HasBall => ball != null && ball.Owner == this;

        public bool TryCollect() => ball != null && ball.TryCollect(this);
        public bool TryShoot() => ball != null && ball.TryShoot(this, shotSpeed);

        private void OnDisable()
        {
            if (HasBall)
                ball.Drop(this);
        }
    }
}
