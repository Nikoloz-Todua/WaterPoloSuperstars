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
        private PrototypeMatch match;
        [SerializeField] private MobileMatchControls mobileControls;

        private void Awake()
        {
            motor = GetComponent<SwimmerMotor>();
            match = GetComponent<SwimmerBallInteraction>()?.Ball?.Match;
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
            if (match != null && match.IsPaused) return;
            motor.ClearFacingDirection();
            Vector2 keyboard=moveAction.ReadValue<Vector2>();
            motor.SetMoveInput(keyboard.sqrMagnitude>0.0001f?keyboard:mobileControls!=null?mobileControls.MovementFor(motor):Vector2.zero);
            if(stamina!=null)stamina.RequestSprint(sprintAction.IsPressed() || mobileControls!=null && mobileControls.SprintFor(motor));
        }

        public void ResetAfterMenu()
        { motor.ClearFacingDirection(); motor.SetMoveInput(Vector2.zero); stamina?.RequestSprint(false); }

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
