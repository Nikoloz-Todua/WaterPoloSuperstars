using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    public sealed class TeamStrategyInput : MonoBehaviour
    {
        [SerializeField] private PrototypeTeamTactics team;
        [SerializeField] private PlayerSelection selection;
        private InputAction toggle;
        private void Awake()=>toggle=new InputAction("ToggleDefense",InputActionType.Button,"<Keyboard>/z");
        private void OnEnable()=>toggle.Enable();
        private void Update(){if(!selection.InputLocked && toggle.WasPressedThisFrame())team.ToggleDefense();}
        private void OnDisable()=>toggle?.Disable();
        private void OnDestroy()=>toggle?.Dispose();
    }
}
