using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerBallInteraction))]
    public sealed class BallInteractionInput : MonoBehaviour
    {
        private SwimmerBallInteraction interaction;
        private InputAction collect;
        private InputAction shoot;

        private void Awake()
        {
            interaction = GetComponent<SwimmerBallInteraction>();
            collect = new InputAction("CollectBall", InputActionType.Button, "<Keyboard>/e");
            collect.AddBinding("<Gamepad>/buttonWest");
            shoot = new InputAction("ShootBall", InputActionType.Button, "<Keyboard>/space");
            shoot.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            collect.Enable();
            shoot.Enable();
        }

        private void Update()
        {
            // Shoot first: simultaneous E+Space while empty cannot collect and shoot in one frame.
            if (shoot.WasPressedThisFrame())
                interaction.TryShoot();
            else if (collect.WasPressedThisFrame())
                interaction.TryCollect();
        }

        private void OnDisable()
        {
            collect?.Disable();
            shoot?.Disable();
        }

        private void OnDestroy()
        {
            collect?.Dispose();
            shoot?.Dispose();
        }
    }
}
