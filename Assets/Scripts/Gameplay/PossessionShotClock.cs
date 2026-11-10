using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Observes real acquisitions. A release or same-team pass keeps the possession's remaining time.</summary>
    [DisallowMultipleComponent]
    public sealed class PossessionShotClock : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField, Min(1f)] private float possessionSeconds = 28f;
        [SerializeField, Min(1f)] private float restartPossessionSeconds = 18f;
        [Tooltip("Possession seconds consumed per live second; independent of the match display ratio.")]
        [SerializeField,Min(0.01f)] private float clockRate = 1f;
        public PrototypeTeam? PossessionTeam { get; private set; }
        public float Remaining { get; private set; }
        public float LiveSecondsRemaining => PossessionTeam.HasValue?Remaining/Mathf.Max(0.01f,clockRate):float.PositiveInfinity;
        public int TeamChanges { get; private set; }
        private void Awake()=>ResetPossession();
        private void OnEnable(){if(ball!=null)ball.PossessionChanged+=Acquired;}
        private void OnDisable(){if(ball!=null)ball.PossessionChanged-=Acquired;}
        public void ResetPossession(){PossessionTeam=null;Remaining=Mathf.Max(1f,possessionSeconds);}
        public void AwardFoul(PrototypeTeam team, bool exclusion)
        {
            if (PossessionTeam != team) { PossessionTeam = team; Remaining = possessionSeconds; TeamChanges++; }
            if (exclusion) Remaining = Mathf.Max(Remaining, restartPossessionSeconds);
            // An ordinary foul keeps time. A penalty's retained-possession reset is applied on resolution.
        }
        public void FinishPenalty(PrototypeTeam attackingTeam, SwimmerBallInteraction owner)
        {
            if (owner == null || owner.GetComponent<SwimmerIdentity>().Team == attackingTeam)
            { PossessionTeam = attackingTeam; Remaining = restartPossessionSeconds; }
            else { PossessionTeam = owner.GetComponent<SwimmerIdentity>().Team; Remaining = possessionSeconds; TeamChanges++; }
        }
        private void Acquired(SwimmerBallInteraction previous,SwimmerBallInteraction next)
        {
            if(next==null || !match.IsPlaying)return;
            PrototypeTeam team=next.GetComponent<SwimmerIdentity>().Team;
            if(PossessionTeam==team)return;
            PossessionTeam=team;Remaining=Mathf.Max(1f,possessionSeconds);TeamChanges++;
        }
        // Called once per live physics step by PrototypeMatch, after goal/quarter resolution.
        public void Tick(float seconds)
        {
            if(!match.IsPlaying || !PossessionTeam.HasValue)return;
            Remaining=Mathf.Max(0f,Remaining-Mathf.Max(0f,seconds)*clockRate);
            if(Remaining<=0f)match.RequestTurnover(PossessionTeam==PrototypeTeam.Blue?PrototypeTeam.White:PrototypeTeam.Blue);
        }
    }
}
