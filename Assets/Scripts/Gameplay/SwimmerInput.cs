using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo
{
    [DefaultExecutionOrder(-10), DisallowMultipleComponent]
    [RequireComponent(typeof(SwimmerMotor))]
    public sealed class SwimmerInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string moveActionPath = "Player/Move";

        private SwimmerMotor motor;
        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction aimAction;
        private InputAction independentAim;
        private SwimmerStamina stamina;
        private SwimmerBallInteraction interaction;
        private PrototypeMatch match;
        [SerializeField] private MobileMatchControls mobileControls;
        private bool sprintTapArmed;
        private Keyboard movementKeyboard;
        private string movementLayout;
        private const string DisplayKeyPrefix="<Keyboard>/#(";

        private void Awake()
        {
            motor = GetComponent<SwimmerMotor>();
            interaction = GetComponent<SwimmerBallInteraction>();
            match = interaction?.Ball?.Match;
            stamina = GetComponent<SwimmerStamina>();
            sprintAction = new InputAction("Sprint",InputActionType.Button,"<Keyboard>/leftShift");
            sprintAction.wantsInitialStateCheck=true;
            aimAction = new InputAction("Aim in place", InputActionType.Button, "<Keyboard>/leftAlt");
            independentAim = new InputAction("Aim while moving", InputActionType.Value);
            independentAim.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/numpad8").With("Down", "<Keyboard>/numpad2")
                .With("Left", "<Keyboard>/numpad4").With("Right", "<Keyboard>/numpad6");
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

            RefreshKeyboardLayout();
            moveAction.Enable();
            sprintAction.Enable();
            aimAction.Enable();
            independentAim.Enable();
            sprintTapArmed=false;
        }

        private void Update()
        {
            if (match != null && match.IsPaused) return;
            RefreshKeyboardLayout();
            motor.ClearFacingDirection();
            Vector2 keyboard=moveAction.ReadValue<Vector2>();
            Vector2 direction=keyboard.sqrMagnitude>0.0001f?keyboard:mobileControls!=null?mobileControls.MovementFor(motor):Vector2.zero;
            if (match != null && match.Phase == MatchPhase.PenaltyReady && match.Rules.Taker.Motor == motor)
            {
                match.Rules.AimPenalty(direction);
                Vector3 aim = match.Rules.PenaltyAimPoint - motor.Position;
                motor.SetFacingDirection(aim);
                motor.SetMoveInput(Vector2.zero);
                return;
            }
            var view = match != null ? match.GameplayView : null;
            if (view != null) direction = view.ScreenToWorldMovement(direction);
            bool explicitAim = false;
            if (interaction != null && interaction.HasBall && match != null && match.IsPlaying)
            {
                var move = new Vector3(direction.x, 0f, direction.y);
                bool aimOnly = aimAction.IsPressed();
                bool touchAim = mobileControls != null && mobileControls.IsPassAiming(motor);
                Vector2 independent = independentAim.ReadValue<Vector2>();
                bool separateAim = independent.sqrMagnitude > .035f;
                explicitAim = aimOnly || touchAim || separateAim;
                if (!touchAim)
                {
                    if (separateAim)
                    {
                        if (view != null) independent = view.ScreenToWorldMovement(independent);
                        motor.SetControlAim(new Vector3(independent.x, 0f, independent.y));
                    }
                    else if (aimOnly || !motor.IsRetreatInput(move)) motor.SetControlAim(move);
                }
                if (aimOnly) direction = Vector2.zero;
                motor.SetFacingDirection(motor.ControlAim);
            }
            else motor.ClearControlAim();
            bool sprint=sprintAction.IsPressed() || mobileControls!=null && mobileControls.SprintFor(motor);
            if(!sprintAction.IsPressed())sprintTapArmed=true;
            if(match!=null&&match.IsSprintRace)
            {
                if(sprintTapArmed&&sprintAction.WasPressedThisFrame())match.SprintOff.Stroke(GetComponent<SwimmerIdentity>());
                direction=match.SprintOff.HumanMovement(direction,sprint);sprint|=match.SprintOff.HumanSprint;
                match.SprintOff.TryHumanPickup();
            }
            motor.SetMoveInput(direction, usePossessionFacing: true, explicitAim: explicitAim);
            if(stamina!=null)stamina.RequestSprint(sprint);
        }

        private void RefreshKeyboardLayout()
        {
            var keyboard=Keyboard.current;
            string layout=keyboard?.keyboardLayout;
            if (keyboard==movementKeyboard && layout==movementLayout) return;
            movementKeyboard=keyboard;
            movementLayout=layout;
            // Layouts without Latin lettering still support the physical W/A/S/D cluster.
            for (int i=0;i<moveAction.bindings.Count;i++)
            {
                var binding=moveAction.bindings[i];
                if (!binding.path.StartsWith(DisplayKeyPrefix,System.StringComparison.Ordinal) ||
                    binding.path.Length!=DisplayKeyPrefix.Length+2) continue;
                string letter=binding.path.Substring(DisplayKeyPrefix.Length,1);
                string fallback=keyboard!=null && keyboard.FindKeyOnCurrentKeyboardLayout(letter)==null
                    ? "<Keyboard>/"+letter.ToLowerInvariant() : null;
                if (binding.overridePath==fallback) continue;
                if (fallback==null) moveAction.RemoveBindingOverride(i);
                else moveAction.ApplyBindingOverride(i,fallback);
            }
        }

        public void ResetAfterMenu()
        { sprintTapArmed=false;motor.ClearFacingDirection(); motor.ClearControlAim(); motor.SetMoveInput(Vector2.zero); stamina?.RequestSprint(false); }

        private void OnDisable()
        {
            moveAction?.Disable();
            aimAction?.Disable();
            independentAim?.Disable();
            sprintAction?.Disable();if(stamina!=null)stamina.StopSprint();
            if (motor != null)
            {
                motor.ClearFacingDirection();
                motor.ClearControlAim();
                motor.SetMoveInput(Vector2.zero);
            }
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            aimAction?.Dispose();
            independentAim?.Dispose();
            sprintAction?.Dispose();
        }
    }
}
