using System.Collections.Generic;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Shared membership/start positions, independent of input selection and tactical AI.</summary>
    [DefaultExecutionOrder(-80), DisallowMultipleComponent]
    public sealed class PrototypeRoster : MonoBehaviour
    {
        [SerializeField] private SwimmerIdentity[] members = new SwimmerIdentity[0];
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
