using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Shared fatigue rules; the motor advances this once per live physics step for human and AI alike.</summary>
    [DisallowMultipleComponent]
    public sealed class SwimmerStamina : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private bool sprintAllowed = true;
        [SerializeField, Range(1f,2f)] private float sprintMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float sprintAccelerationMultiplier = 1.25f;
        [SerializeField, Min(0f)] private float drainPerSecond = 0.16f;
        [SerializeField, Min(0f)] private float restRecoveryPerSecond = 0.12f;
        [SerializeField, Min(0f)] private float swimRecoveryPerSecond = 0.06f;
        [SerializeField, Range(0.01f,1f)] private float restartThreshold = 0.25f;
        private bool requested,exhausted;
        public float Value { get; private set; } = 1f;
        public bool IsSprinting { get; private set; }
        public bool SprintAllowed => sprintAllowed;
        public float SpeedMultiplier => IsSprinting?sprintMultiplier:1f;
        public float AccelerationMultiplier => IsSprinting?sprintAccelerationMultiplier:1f;
        public void RequestSprint(bool value)=>requested=value;
        public void StopSprint(){requested=false;IsSprinting=false;}
        public void ResetStamina(){Value=1f;exhausted=false;StopSprint();}
        private void OnEnable(){if(match!=null)match.NewMatch+=ResetStamina;}
        private void OnDisable(){if(match!=null)match.NewMatch-=ResetStamina;StopSprint();}
        public void Simulate(bool moving,float seconds)
        {
            if(match!=null && match.IsPaused)return;
            if(match!=null && !match.IsPlaying){StopSprint();return;}
            if(!sprintAllowed){StopSprint();return;}
            if(exhausted && Value>=restartThreshold)exhausted=false;
            IsSprinting=requested && moving && !exhausted && Value>0f;
            Value=Mathf.Clamp01(Value+(IsSprinting?-drainPerSecond:(moving?swimRecoveryPerSecond:restRecoveryPerSecond))*Mathf.Max(0f,seconds));
            if(Value<=0f){exhausted=true;IsSprinting=false;}
        }
    }
}
