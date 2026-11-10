using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Chooses a modestly assisted horizontal lob; BallController owns the physical flight and reception.</summary>
    [DisallowMultipleComponent,RequireComponent(typeof(SwimmerBallInteraction))]
    public sealed class LobPassing : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField,Min(3f)] private float defaultDistance=8f;
        [SerializeField,Min(3f)] private float maximumDistance=16f;
        [SerializeField,Min(0.2f)] private float flightDuration=1.3f;
        [SerializeField,Range(0f,60f)] private float assistCone=30f;
        [SerializeField,Range(0f,20f)] private float maximumCorrection=12f;
        private SwimmerIdentity identity;
        private void Awake()=>identity=GetComponent<SwimmerIdentity>();
        public bool TryPass()
        {
            if(!identity.Interaction.HasBall)return false;
            Vector3 forward=Vector3.ProjectOnPlane(identity.Motor.Facing,Vector3.up).normalized;
            SwimmerPassing receiver=null;float best=-1f,distance=defaultDistance;
            Vector3 release=identity.Motor.Position+forward*identity.Interaction.ReleaseDistance;
            foreach(var player in roster.FieldPlayers)
            {
                if(!roster.AreTeammates(identity,player) || !PrototypeTeamTactics.Eligible(player))continue;
                Vector3 gap=Vector3.ProjectOnPlane(player.Motor.Position-release,Vector3.up);float length=gap.magnitude;
                if(length<2f || length>maximumDistance)continue;
                float alignment=Vector3.Dot(forward,gap/length);
                if(alignment<Mathf.Cos(assistCone*Mathf.Deg2Rad) || alignment<=best)continue;
                receiver=player.GetComponent<SwimmerPassing>();best=alignment;distance=length;
            }
            if(receiver!=null)
            {
                Vector3 desired=Vector3.ProjectOnPlane(receiver.Identity.Motor.Position-release,Vector3.up).normalized;
                forward=Vector3.RotateTowards(forward,desired,maximumCorrection*Mathf.Deg2Rad,0f);
            }
            return identity.Interaction.Ball.TryLob(identity.Interaction,forward,Mathf.Min(distance,maximumDistance),flightDuration,receiver);
        }
    }
}
