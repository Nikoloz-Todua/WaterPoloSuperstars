using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum AthleteRole { FieldPlayer, Goalkeeper }
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerMotor), typeof(SwimmerBallInteraction))]
    public sealed class SwimmerIdentity : MonoBehaviour
    {
        [SerializeField] private string playerId;
        [SerializeField] private PrototypeTeam team = PrototypeTeam.Blue;
        [SerializeField] private AthleteRole role;
        [SerializeField, Min(1)] private int capNumber = 1;
        [SerializeField] private Transform startPosition;
        [SerializeField] private GameObject selectionIndicator;

        private SwimmerMotor motor;
        private SwimmerBallInteraction interaction;
        public string PlayerId => playerId;
        public PrototypeTeam Team => team;
        public AthleteRole Role => role;
        public bool IsFieldPlayer => role == AthleteRole.FieldPlayer;
        public int CapNumber => capNumber;
        public Transform StartPosition => startPosition;
        public SwimmerMotor Motor => motor != null ? motor : motor = GetComponent<SwimmerMotor>();
        public SwimmerBallInteraction Interaction => interaction != null ? interaction : interaction = GetComponent<SwimmerBallInteraction>();

        public void SetSelected(bool selected)
        {
            if (selectionIndicator != null) selectionIndicator.SetActive(selected);
        }
    }
}
