using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>F8 inspects the arena using the same camera; gameplay continues under its existing authorities.</summary>
    [DefaultExecutionOrder(300)]
    public sealed class ArenaDeveloperCamera : MonoBehaviour
    {
        public GameplayCamera broadcast;
        public Transform[] viewpoints;
        public int ViewIndex { get; private set; } = -1;
        public void SetView(int index)
        {
            ViewIndex = index < 0 || index >= viewpoints.Length ? -1 : index;
            broadcast.enabled = ViewIndex < 0;
            if (ViewIndex < 0) broadcast.SnapToTarget();
        }
        void LateUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                SetView(ViewIndex + 1);
#endif
            if (ViewIndex >= 0 && viewpoints[ViewIndex] != null)
                transform.SetPositionAndRotation(viewpoints[ViewIndex].position, viewpoints[ViewIndex].rotation);
        }
        void OnDisable() { if (broadcast != null) broadcast.enabled = true; ViewIndex = -1; }
    }
}
