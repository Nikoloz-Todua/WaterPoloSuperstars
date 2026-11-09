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
        private InputAction pass;
        private SwimmerPassing passing;

        private void Awake()
        {
            interaction = GetComponent<SwimmerBallInteraction>();
            passing = GetComponent<SwimmerPassing>();
            collect = new InputAction("CollectBall", InputActionType.Button, "<Keyboard>/e");
            collect.AddBinding("<Gamepad>/buttonWest");
            shoot = new InputAction("ShootBall", InputActionType.Button, "<Keyboard>/space");
            shoot.AddBinding("<Gamepad>/buttonSouth");
            pass = new InputAction("PassBall", InputActionType.Button, "<Keyboard>/b");
            pass.AddBinding("<Gamepad>/buttonNorth");
        }

        private void OnEnable()
        {
            collect.Enable();
            shoot.Enable();
            pass.Enable();
        }

        private void Update()
        {
            // Shoot first: simultaneous E+Space while empty cannot collect and shoot in one frame.
            if (shoot.WasPressedThisFrame())
                interaction.TryShoot();
            else if (pass.WasPressedThisFrame())
                passing?.TryPass();
            else if (collect.WasPressedThisFrame())
                interaction.TryCollect();
        }

        private void OnDisable()
        {
            collect?.Disable();
            shoot?.Disable();
            pass?.Disable();
        }

        private void OnDestroy()
        {
            collect?.Dispose();
            shoot?.Dispose();
            pass?.Dispose();
        }
    }
}
