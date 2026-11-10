using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum AthleteRole { FieldPlayer, Goalkeeper }
    // Preserve the serialized values of the original centre/left/right athletes.
    public enum FieldPositionRole { CentreForward = 0, LeftWing = 1, RightWing = 2, CentreBack = 3, LeftFlat = 4, RightFlat = 5 }
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerMotor), typeof(SwimmerBallInteraction))]
    public sealed class SwimmerIdentity : MonoBehaviour
    {
        [SerializeField] private string playerId;
        [SerializeField] private PrototypeTeam team = PrototypeTeam.Blue;
        [SerializeField] private AthleteRole role;
        [SerializeField] private FieldPositionRole fieldPosition;
        [SerializeField, Min(1)] private int capNumber = 1;
        [SerializeField] private Transform startPosition;
        [SerializeField] private GameObject selectionIndicator;

        private SwimmerMotor motor;
        private SwimmerBallInteraction interaction;
        public string PlayerId => playerId;
        public PrototypeTeam Team => team;
        public AthleteRole Role => role;
        public FieldPositionRole FieldPosition => fieldPosition;
        public bool IsFieldPlayer => role == AthleteRole.FieldPlayer;
        public int CapNumber => capNumber;
        public int PersonalFouls { get; internal set; }
        public float ExclusionRemaining { get; internal set; }
        public bool PermanentlyRemoved { get; internal set; }
        public bool EligibleForPlay => !PermanentlyRemoved && ExclusionRemaining <= 0f;
        public MatchRules Rules { get; internal set; }
        public Transform StartPosition => startPosition;
        public SwimmerMotor Motor => motor != null ? motor : motor = GetComponent<SwimmerMotor>();
        public SwimmerBallInteraction Interaction => interaction != null ? interaction : interaction = GetComponent<SwimmerBallInteraction>();

        public void SetSelected(bool selected)
        {
            if (selectionIndicator != null) selectionIndicator.SetActive(selected);
        }
    }
}
