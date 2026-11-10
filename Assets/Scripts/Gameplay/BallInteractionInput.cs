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
        private InputAction steal;
        private InputAction hold;
        private InputAction skipModifier, highModifier;
        private SwimmerPassing passing;
        private SwimmerSteal stealing;
        private ShotCharge charge;
        private bool blockShootUntilRelease;
        private int enabledFrame;
        private InputAction lob;
        private LobPassing lobPassing;

        private void Awake()
        {
            interaction = GetComponent<SwimmerBallInteraction>();
            passing = GetComponent<SwimmerPassing>();
            stealing = GetComponent<SwimmerSteal>();
            charge = GetComponent<ShotCharge>();
            lobPassing=GetComponent<LobPassing>();lob=new InputAction("LobPass",InputActionType.Button,"<Keyboard>/l");
            collect = new InputAction("CollectBall", InputActionType.Button, "<Keyboard>/e");
            collect.AddBinding("<Gamepad>/buttonWest");
            shoot = new InputAction("ShootBall", InputActionType.Button, "<Keyboard>/space");
            shoot.AddBinding("<Gamepad>/buttonSouth");
            pass = new InputAction("PassBall", InputActionType.Button, "<Keyboard>/b");
            pass.AddBinding("<Gamepad>/buttonNorth");
            steal = new InputAction("AttemptSteal", InputActionType.Button, "<Keyboard>/f");
            hold = new InputAction("IntentionalHold", InputActionType.Button, "<Keyboard>/g");
            skipModifier=new InputAction("SkipShotModifier",InputActionType.Button,"<Keyboard>/q");
            highModifier=new InputAction("HighShotModifier",InputActionType.Button,"<Keyboard>/r");
        }

        private void OnEnable()
        {
            collect.Enable();
            shoot.Enable();
            pass.Enable();
            steal.Enable();
            hold.Enable();
            skipModifier.Enable();highModifier.Enable();
            lob.Enable();
            enabledFrame = Time.frameCount;
            blockShootUntilRelease = false;
            foreach (var control in shoot.controls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                    blockShootUntilRelease = true;
        }

        private void Update()
        {
            // A control handoff must not replay another athlete's button press/held charge.
            if (Time.frameCount == enabledFrame) return;
            if (blockShootUntilRelease)
            {
                if (!shoot.IsPressed()) blockShootUntilRelease = false;
            }
            else if (shoot.WasPressedThisFrame())
                charge?.BeginCharge(skipModifier.IsPressed()?ShotType.Skip:highModifier.IsPressed()?ShotType.High:ShotType.Normal);
            if (!blockShootUntilRelease && shoot.WasReleasedThisFrame())
                charge?.ReleaseCharge();
            else if (pass.WasPressedThisFrame())
            { charge?.CancelCharge(); passing?.TryPass(); }
            else if(lob.WasPressedThisFrame())
            {charge?.CancelCharge();lobPassing?.TryPass();}
            else if (steal.WasPressedThisFrame())
                stealing?.TrySteal();
            else if (hold.WasPressedThisFrame())
                GetComponent<SwimmerIdentity>().Rules?.TryHold(GetComponent<SwimmerIdentity>());
            else if (collect.WasPressedThisFrame())
                interaction.TryCollect();
        }

        private void OnDisable()
        {
            charge?.CancelCharge();
            collect?.Disable();
            shoot?.Disable();
            pass?.Disable();
            steal?.Disable();
            hold?.Disable();
            skipModifier?.Disable();highModifier?.Disable();
            lob?.Disable();
        }

        private void OnDestroy()
        {
            collect?.Dispose();
            shoot?.Dispose();
            pass?.Dispose();
            steal?.Dispose();
            hold?.Dispose();
            skipModifier?.Dispose();highModifier?.Dispose();
            lob?.Dispose();
        }
    }
}
