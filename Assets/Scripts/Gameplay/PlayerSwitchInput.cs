using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo
{
    [DefaultExecutionOrder(-40), DisallowMultipleComponent, RequireComponent(typeof(PlayerSelection))]
    public sealed class PlayerSwitchInput : MonoBehaviour
    {
        private PlayerSelection selection;
        private InputAction switchPlayer;
        private void Awake()
        {
            selection = GetComponent<PlayerSelection>();
            switchPlayer = new InputAction("SwitchPlayer", InputActionType.Button, "<Keyboard>/c");
            switchPlayer.AddBinding("<Gamepad>/rightShoulder");
        }
        private void OnEnable() => switchPlayer.Enable();
        private void Update()
        {
            if (switchPlayer.WasPressedThisFrame()) selection.SelectNext();
        }
        private void OnDisable() => switchPlayer?.Disable();
        private void OnDestroy() => switchPlayer?.Dispose();
    }
}
