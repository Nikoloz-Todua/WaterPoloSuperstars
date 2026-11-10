using UnityEngine;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerIdentity))]
    public sealed class SwimmerPassing : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField, Min(0.1f)] private float passSpeed = 8f;
        [SerializeField, Range(0f, 60f)] private float assistCone = 35f;
        [SerializeField, Range(0f, 1f)] private float assistStrength = 0.45f;
        [SerializeField, Range(0f, 25f)] private float maximumCorrection = 12f;
        [SerializeField, Min(1f)] private float maximumTargetDistance = 12f;
        [SerializeField, Min(0.1f)] private float receivingDistance = 1.35f;
        [SerializeField, Min(0.02f)] private float minimumFlightTime = 0.08f;
        [SerializeField, Min(0.1f)] private float receivingWindow = 2.5f;
        private SwimmerIdentity identity;
        public SwimmerIdentity Identity => identity != null ? identity : identity = GetComponent<SwimmerIdentity>();
        public SwimmerBallInteraction Interaction => Identity.Interaction;
        public float ReceivingDistance => receivingDistance;
        public float MaximumTargetDistance => maximumTargetDistance;

        public bool CanReceiveFrom(SwimmerBallInteraction passer)
        {
            return Identity.EligibleForPlay && isActiveAndEnabled && Interaction.isActiveAndEnabled && roster != null &&
                passer != null && roster.AreTeammates(Identity, passer.GetComponent<SwimmerIdentity>());
        }

        public bool TryPass()
        {
            if (!Interaction.HasBall || roster == null) return false;
            Vector3 forward = Vector3.ProjectOnPlane(Identity.Motor.Facing, Vector3.up).normalized;
            Vector3 origin = Identity.Motor.Position;
            SwimmerPassing receiver = null;
            float best = float.NegativeInfinity;
            foreach (SwimmerIdentity teammate in roster.Members)
            {
                if (!teammate.IsFieldPlayer || !roster.AreTeammates(Identity, teammate)) continue;
                Vector3 delta = teammate.Motor.Position - origin;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance < 2.5f || distance > maximumTargetDistance) continue;
                float alignment = Vector3.Dot(forward, delta / distance);
                if (alignment < Mathf.Cos(assistCone * Mathf.Deg2Rad)) continue;
                SwimmerPassing candidate = teammate.GetComponent<SwimmerPassing>();
                if (candidate == null || !candidate.CanReceiveFrom(Interaction)) continue;
                float score = alignment - distance * 0.01f;
                if(!LaneOpen(origin,teammate.Motor.Position))score-=0.35f;
                if (score > best) { best = score; receiver = candidate; }
            }

            return Launch(forward, receiver);
        }

        public bool TryPassTo(SwimmerIdentity teammate)
        {
            if (!Interaction.HasBall || roster == null || teammate == null || !teammate.IsFieldPlayer || !roster.AreTeammates(Identity, teammate)) return false;
            Vector3 delta = Vector3.ProjectOnPlane(teammate.Motor.Position - Identity.Motor.Position, Vector3.up);
            float distance = delta.magnitude;
            var receiver = teammate.GetComponent<SwimmerPassing>();
            Vector3 forward = Vector3.ProjectOnPlane(Identity.Motor.Facing, Vector3.up).normalized;
            if (distance < 2.5f || distance > maximumTargetDistance || Vector3.Dot(forward, delta / distance) < Mathf.Cos(assistCone * Mathf.Deg2Rad) || receiver == null || !receiver.CanReceiveFrom(Interaction)) return false;
            return Launch(forward, receiver);
        }

        private bool Launch(Vector3 forward, SwimmerPassing receiver)
        {
            Vector3 origin = Identity.Motor.Position;
            Vector3 direction = forward;
            if (receiver != null)
            {
                Vector3 release = origin + forward * Interaction.ReleaseDistance;
                Vector3 desired = AimPoint(receiver.Identity) - release;
                desired.y = 0f;
                Vector3 assisted = Vector3.Slerp(forward, desired.normalized, assistStrength);
                direction = Vector3.RotateTowards(forward, assisted, maximumCorrection * Mathf.Deg2Rad, 0f).normalized;
            }
            float distance=receiver!=null?Vector3.Distance(origin,receiver.Identity.Motor.Position):6f;
            float speed=Mathf.Clamp(distance*0.9f,passSpeed*0.85f,passSpeed*1.35f);
            return Interaction.Ball.TryPass(Interaction, direction, speed, receiver, minimumFlightTime, receivingWindow);
        }
        public Vector3 AimPoint(SwimmerIdentity receiver)
        {
            float distance=Vector3.Distance(Identity.Motor.Position,receiver.Motor.Position);
            float time=Mathf.Clamp(distance/Mathf.Max(1f,passSpeed),0f,1.2f);
            return receiver.Motor.Position+Vector3.ClampMagnitude(receiver.Motor.Velocity*time,1.4f);
        }
        private bool LaneOpen(Vector3 from,Vector3 to)
        {
            Vector3 line=Vector3.ProjectOnPlane(to-from,Vector3.up);float length=line.sqrMagnitude;
            if(length<0.01f)return false;
            for(int i=0;i<roster.Members.Count;i++)
            {
                var p=roster.Members[i];if(p.Team==Identity.Team || !p.EligibleForPlay)continue;
                float along=Vector3.Dot(p.Motor.Position-from,line)/length;
                if(along>0.05f && along<0.95f && Vector3.ProjectOnPlane(p.Motor.Position-(from+line*along),Vector3.up).sqrMagnitude<1f)return false;
            }
            return true;
        }
    }
}
