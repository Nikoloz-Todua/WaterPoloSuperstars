using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    public sealed class PoolWaterTestCamera : MonoBehaviour
    {
        public Transform[] viewpoints;
        public int viewIndex;
        private void Start() => SetView(viewIndex);
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) SetView(viewIndex + 1);
        }
        public void SetView(int index)
        {
            if (viewpoints == null || viewpoints.Length == 0) return;
            viewIndex = ((index % viewpoints.Length) + viewpoints.Length) % viewpoints.Length;
            transform.SetPositionAndRotation(viewpoints[viewIndex].position, viewpoints[viewIndex].rotation);
        }
    }
}
