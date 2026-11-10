using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SwimmerMotor))]
    public sealed class SwimmerInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string moveActionPath = "Player/Move";

        private SwimmerMotor motor;
        private InputAction moveAction;
        private InputAction sprintAction;
        private SwimmerStamina stamina;

        private void Awake()
        {
            motor = GetComponent<SwimmerMotor>();
            stamina = GetComponent<SwimmerStamina>();
            sprintAction = new InputAction("Sprint",InputActionType.Button,"<Keyboard>/leftShift");
            sprintAction.wantsInitialStateCheck=true;
        }

        private void OnEnable()
        {
            if (moveAction == null)
            {
                InputAction source = inputActions != null ? inputActions.FindAction(moveActionPath) : null;
                if (source == null)
                {
                    Debug.LogError("SwimmerInput requires an Input Actions asset with Player/Move.", this);
                    enabled = false;
                    return;
                }

                // Private action lifetime: never enable/disable a shared asset used by another actor/UI.
                moveAction = source.Clone();
            }

            moveAction.Enable();
            sprintAction.Enable();
        }

        private void Update()
        {
            motor.ClearFacingDirection();
            motor.SetMoveInput(moveAction.ReadValue<Vector2>());
            if(stamina!=null)stamina.RequestSprint(sprintAction.IsPressed());
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            sprintAction?.Disable();if(stamina!=null)stamina.StopSprint();
            if (motor != null)
                motor.SetMoveInput(Vector2.zero);
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            sprintAction?.Dispose();
        }
    }
}
