using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Replace only this visual prefab for Blender art. Motor, carry/release anchors and colliders stay on the athlete root.</summary>
    public sealed class ArenaAthleteVisual : MonoBehaviour
    {
        public SwimmerMotor motor;
        public SwimmerIdentity identity;
        public Transform leftArm, rightArm, leftLeg, rightLeg;
        public Renderer[] skinSlots, capSlots, swimwearSlots;
        Quaternion la, ra, ll, rl;
        PrototypeMatch match;
        Vector3 origin;
        void Awake()
        {
            origin = transform.localPosition;
            la = leftArm.localRotation; ra = rightArm.localRotation;
            ll = leftLeg.localRotation; rl = rightLeg.localRotation;
            match = FindAnyObjectByType<PrototypeMatch>();
        }
        void LateUpdate()
        {
            if (match != null && match.IsPaused) return;
            float t = (match != null ? match.PresentationTime : Time.time) * 2.7f + identity.CapNumber;
            float speed = Mathf.Clamp01(motor.Velocity.magnitude / 5f);
            float kick = Mathf.Sin(t * 1.5f) * (8f + speed * 16f);
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(t) * .016f);
            leftLeg.localRotation = ll * Quaternion.Euler(kick, 0, 0);
            rightLeg.localRotation = rl * Quaternion.Euler(-kick, 0, 0);
            leftArm.localRotation = la * Quaternion.Euler(Mathf.Sin(t) * (5 + speed * 16), 0, 0);
            rightArm.localRotation = ra * Quaternion.Euler(-Mathf.Sin(t) * (5 + speed * 16), 0, 0);
        }
    }
}
