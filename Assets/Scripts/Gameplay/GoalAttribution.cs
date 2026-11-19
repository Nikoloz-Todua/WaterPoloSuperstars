namespace ToduStudios.WaterPolo
{
    public enum GoalAttributionKind { Unknown, AttackingShot, DefensiveDeflection, OwnGoal }

    /// <summary>Frozen attribution for a goal whose team and legality were decided by PrototypeMatch.</summary>
    public readonly struct GoalAttribution
    {
        public readonly GoalAttributionKind Kind;
        public readonly string PlayerId;
        public readonly int CapNumber;
        internal readonly SwimmerBallInteraction Actor;

        internal GoalAttribution(GoalAttributionKind kind, SwimmerBallInteraction actor, string id, int cap)
        {
            Kind=kind;
            Actor=actor;
            PlayerId=id;
            CapNumber=cap;
        }
    }

    // Store identity at the controlled release because a scene slot can later be occupied by a substitute.
    internal readonly struct GoalAction
    {
        public readonly SwimmerBallInteraction Actor;
        public readonly PrototypeTeam Team;
        private readonly string id;
        private readonly int cap;
        private readonly BallReleaseKind release;

        public GoalAction(SwimmerBallInteraction actor, BallReleaseKind kind)
        {
            Actor=actor;
            release=kind;
            var player=actor.GetComponent<SwimmerIdentity>();
            Team=player!=null ? player.Team : default;
            id=player!=null
                ? string.IsNullOrEmpty(player.CollectionDefinitionId) ? player.PlayerId : player.CollectionDefinitionId
                : null;
            cap=player!=null ? player.CapNumber : 0;
        }

        public GoalAttribution Attribute(PrototypeTeam scoringTeam, bool opposingTouch)
        {
            if (string.IsNullOrEmpty(id)) return default;
            if (Team != scoringTeam)
                return opposingTouch ? default : new GoalAttribution(GoalAttributionKind.OwnGoal,Actor,id,cap);
            if (release != BallReleaseKind.Shot) return default;
            return new GoalAttribution(opposingTouch ? GoalAttributionKind.DefensiveDeflection : GoalAttributionKind.AttackingShot,
                Actor,id,cap);
        }
    }
}
