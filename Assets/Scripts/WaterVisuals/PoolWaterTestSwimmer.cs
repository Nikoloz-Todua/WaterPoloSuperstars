using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    // A bounded visual rig for waterline/occlusion inspection, never a gameplay athlete.
    public sealed class PoolWaterTestSwimmer : MonoBehaviour
    {
        public Transform leftArm, rightArm, leftLeg, rightLeg;
        public bool animate = true;
        private Vector3 origin;
        private Quaternion leftArmRest, rightArmRest, leftLegRest, rightLegRest;
        private void Awake()
        {
            origin = transform.localPosition;
            leftArmRest = leftArm.localRotation; rightArmRest = rightArm.localRotation;
            leftLegRest = leftLeg.localRotation; rightLegRest = rightLeg.localRotation;
        }
        private void Update()
        {
            if (!animate) return;
            float t = Time.time * 1.6f;
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(t) * 0.025f);
            leftArm.localRotation = leftArmRest * Quaternion.Euler(Mathf.Sin(t) * 9f, 0f, 0f);
            rightArm.localRotation = rightArmRest * Quaternion.Euler(-Mathf.Sin(t) * 9f, 0f, 0f);
            leftLeg.localRotation = leftLegRest * Quaternion.Euler(Mathf.Sin(t * 1.3f) * 13f, 0f, 0f);
            rightLeg.localRotation = rightLegRest * Quaternion.Euler(-Mathf.Sin(t * 1.3f) * 13f, 0f, 0f);
        }
    }
}
