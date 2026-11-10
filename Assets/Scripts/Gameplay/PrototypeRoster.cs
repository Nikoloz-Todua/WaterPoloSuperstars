using System.Collections.Generic;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Shared membership/start positions, independent of input selection and tactical AI.</summary>
    [DefaultExecutionOrder(-80), DisallowMultipleComponent]
    public sealed class PrototypeRoster : MonoBehaviour
    {
        [SerializeField] private SwimmerIdentity[] members = new SwimmerIdentity[0];
        [SerializeField] private PoolBounds poolBounds;
        private Vector3[] initialStarts;
        private Quaternion[] initialRotations;
        public bool EndsReversed { get; private set; }
        private bool[] restoreMotors;
        private bool paused;
        private SwimmerIdentity[] fieldPlayers;
        public IReadOnlyList<SwimmerIdentity> Members => members;
        public IReadOnlyList<SwimmerIdentity> FieldPlayers
        {
            get
            {
                if (fieldPlayers == null)
                {
                    var fields = new List<SwimmerIdentity>();
                    foreach (var member in members) if (member != null && member.IsFieldPlayer) fields.Add(member);
                    fieldPlayers = fields.ToArray();
                }
                return fieldPlayers;
            }
        }
        private void OnValidate() => fieldPlayers = null;

        private void Awake()
        {
            CaptureStarts();
            restoreMotors = new bool[members.Length];
            var ids = new HashSet<string>();
            foreach (SwimmerIdentity player in members)
            {
                if (player == null || string.IsNullOrWhiteSpace(player.PlayerId) ||
                    !ids.Add(player.PlayerId) || player.StartPosition == null)
                {
                    Debug.LogError("PrototypeRoster needs distinct player IDs and assigned start positions.", this);
                    enabled = false;
                    return;
                }
            }
        }

        private void CaptureStarts()
        {
            if(initialStarts!=null)return;
            initialStarts=new Vector3[members.Length];initialRotations=new Quaternion[members.Length];
            for(int i=0;i<members.Length;i++)
            {if(members[i]==null || members[i].StartPosition==null)continue;initialStarts[i]=members[i].StartPosition.position;initialRotations[i]=members[i].StartPosition.rotation;}
        }
        public void SetEndsReversed(bool reversed)
        {
            CaptureStarts();EndsReversed=reversed;
            Vector3 center=poolBounds!=null?poolBounds.transform.position:Vector3.zero;
            Quaternion rotation=reversed?Quaternion.Euler(0f,180f,0f):Quaternion.identity;
            for(int i=0;i<members.Length;i++)
                members[i].StartPosition.SetPositionAndRotation(center+rotation*(initialStarts[i]-center),rotation*initialRotations[i]);
        }

        public bool Contains(SwimmerIdentity player)
        {
            foreach (SwimmerIdentity member in members)
                if (member == player) return true;
            return false;
        }

        public bool AreTeammates(SwimmerIdentity a, SwimmerIdentity b) =>
            a != null && b != null && a != b && a.Team == b.Team && Contains(a) && Contains(b);

        public void SetSimulationPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            for (int i = 0; i < members.Length; i++)
            {
                SwimmerMotor motor = members[i].Motor;
                if (value)
                {
                    restoreMotors[i] = motor.enabled;
                    motor.StopMotion();
                    motor.enabled = false;
                }
                else motor.enabled = restoreMotors[i];
            }
        }

        public void ResetPositions()
        {
            foreach (SwimmerIdentity player in members)
                player.Motor.ResetPose(player.StartPosition.position, player.StartPosition.rotation);
        }
    }
}
