using UnityEngine;
using Unity.Profiling;

namespace ToduStudios.WaterPolo
{
    public enum DefenseStrategy { Press, Zone }
    /// <summary>Explicit team geometry and shared roster context; never owns the ball.</summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeTeamTactics : MonoBehaviour
    {
        [SerializeField] private PrototypeTeam team;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private BallController ball;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private GoalMouth attackingGoal;
        [SerializeField] private GoalMouth defendedGoal;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private FieldFormation[] formation = FieldFormation.Defaults();
        [SerializeField, Min(0.1f)] private float assignmentInterval = 0.5f;
        [SerializeField, Min(0f)] private float reassignmentAdvantage = 1f;
        [SerializeField, Min(1f)] private float supportForward = 4f;
        [SerializeField, Min(1f)] private float supportWidth = 3f;
        [SerializeField, Min(0.5f)] private float pressureDistance = 1.5f;
        [SerializeField, Min(1f)] private float shootingRange = 9f;
        [SerializeField, Range(0f, 1f)] private float shotQualityThreshold = 0.55f;
        [SerializeField, Min(0.1f)] private float minimumHoldTime = 0.8f;
        [SerializeField, Min(1f)] private float maximumHoldTime = 8f;
        [SerializeField, Min(0.5f)] private float teamPassCooldown = 3f;
        [SerializeField] private DefenseStrategy defenseStrategy;
        [SerializeField] private bool automaticDefense;
        [SerializeField, Min(0f)] private float laneClearance = 1.05f;
        [SerializeField, Min(0f)] private float passProgressWeight = 0.08f;
        [SerializeField, Min(0f)] private float passOpennessWeight = 0.5f;
        [SerializeField, Min(0.1f)] private float aiShotSpeed = 16f;
        [SerializeField, Min(.02f)] private float blockReaction = .085f;
        [SerializeField, Min(.05f)] private float blockCommitment = .28f;
        [SerializeField, Min(.05f)] private float blockRecovery = .30f;
        private SwimmerIdentity blockingPlayer;
        private Vector3 blockLook;
        private float blockStarts, blockEnds, blockRecovers;
        private int blockChecked = -1, lastBlockShot = -1;
        private float nextDefenseChoice;
        private SwimmerIdentity[] marks, previousMarks;
        private bool[] usedThreats;
        private SwimmerIdentity lastPasser, lastReceiver;
        private float lastPassTime, recoveryUntil;
        private int carryLane;
        private static readonly ProfilerMarker SupportSampling = new ProfilerMarker("WaterPolo.TeamSupport");
        private const float SupportInterval = .20f;
        private const float SupportCommitment = .65f;
        private SwimmerBallInteraction supportOwner;
        private SwimmerIdentity supportSelection;
        private float nextSupportSample;
        private float supportPossessionStarted;
        private Vector3 supportOrigin;
        private Vector3[] sampledPositions, supportTargets;
        private float[] supportUntil;
        private bool[] sampledEligible;
        private bool[] supportAssigned;
        private SwimmerBallInteraction cachedOwner;
        private SwimmerIdentity cachedCarrier;
        public bool Counterattacking => Time.time < recoveryUntil;
        public int FieldAdvantage
        {
            get
            {
                int value = 0;
                for(int i=0;i<roster.FieldPlayers.Count;i++)
                {var p=roster.FieldPlayers[i];if(p.EligibleForPlay)value+=p.Team==team?1:-1;}
                return value;
            }
        }
        public DefenseStrategy DefenseMode => defenseStrategy;
        public float AIShotSpeed => aiShotSpeed;
        public bool ShotUrgent => match.ShotClock!=null && match.ShotClock.PossessionTeam==team && match.ShotClock.Remaining<5f;
        public void ToggleDefense() { if(match.IsPlaying) SetDefense(defenseStrategy==DefenseStrategy.Press?DefenseStrategy.Zone:DefenseStrategy.Press); }
        public void SetDefense(DefenseStrategy mode) { defenseStrategy=mode; nextAssignment=0f; presser=null; }
        private SwimmerIdentity presser, loosePursuer;
        private SwimmerBallInteraction assignedOwner;
        private float nextAssignment, nextPass;
        public PrototypeTeam Team => team;
        public PrototypeRoster Roster => roster;
        public BallController Ball => ball;
        public PoolBounds Pool => pool;
        public GoalMouth AttackingGoal => attackingGoal;
        public GoalMouth DefendedGoal => defendedGoal;
        public Vector3 AttackDirection => attackingGoal.transform.forward;
        public float ShootingRange => shootingRange;
        public float ShotQualityThreshold => shotQualityThreshold;
        public float MinimumHoldTime => minimumHoldTime;
        public float MaximumHoldTime => maximumHoldTime;
        public bool CanPass => Time.time >= nextPass;
        public SwimmerIdentity Carrier
        {
            get
            {
                if (cachedOwner != ball.Owner)
                {
                    cachedOwner = ball.Owner;
                    cachedCarrier = cachedOwner != null ? cachedOwner.GetComponent<SwimmerIdentity>() : null;
                }
                return cachedCarrier;
            }
        }
        public SwimmerIdentity Presser { get { RefreshAssignments(); return presser; } }
        public SwimmerIdentity LoosePursuer { get { RefreshAssignments(); return loosePursuer; } }
        public void RecordPass()
        {
            nextPass = Time.time + teamPassCooldown; lastPassTime = Time.time;
            lastPasser = ball.LastReleaser != null ? ball.LastReleaser.GetComponent<SwimmerIdentity>() : null;
            lastReceiver = ball.IntendedReceiver != null ? ball.IntendedReceiver.GetComponent<SwimmerIdentity>() : null;
        }
        public void ResetDecisions() { nextAssignment = nextPass = nextDefenseChoice = 0f; carryLane=0; presser = loosePursuer = null; assignedOwner = null;
            supportOwner = null; nextSupportSample = 0f;
            blockingPlayer = null; blockChecked = lastBlockShot = -1; }
        public void SetGoals(GoalMouth attack,GoalMouth defend)
        { attackingGoal=attack;defendedGoal=defend;ResetDecisions(); }
        private void OnEnable() { if (match != null) { match.Changed += ResetDecisions; match.NewMatch += ClearHistory; } if(ball!=null)ball.PossessionChanged+=ObserveRecovery; }
        private void OnDisable() { if (match != null) { match.Changed -= ResetDecisions; match.NewMatch -= ClearHistory; } if(ball!=null)ball.PossessionChanged-=ObserveRecovery; }
        private PrototypeTeam? lastPossessionTeam;
        private void ClearHistory() { lastPasser=lastReceiver=null;lastPossessionTeam=null;recoveryUntil=0f; }
        private void ObserveRecovery(SwimmerBallInteraction previous,SwimmerBallInteraction next)
        {
            if(next==null || !match.IsPlaying)return;
            var ownerTeam=next.GetComponent<SwimmerIdentity>().Team;
            if(ownerTeam==team && lastPossessionTeam.HasValue && lastPossessionTeam!=team)recoveryUntil=Time.time+3f;
            lastPossessionTeam=ownerTeam;
        }
        public static Vector3 Planar(Vector3 delta) { delta.y = 0f; return delta; }
        public static bool Eligible(SwimmerIdentity player) => player != null && player.EligibleForPlay && player.IsFieldPlayer && player.enabled && player.gameObject.activeInHierarchy && player.Motor.enabled && player.Interaction.isActiveAndEnabled;
        public Vector3 Clamp(Vector3 position) => pool.ClampPosition(position, 1.1f);

        public int Slot(SwimmerIdentity player)
        {
            int rank = 0;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var member = roster.Members[i]; if (member == player) return rank;
                if (Eligible(member) && member.Team == team) rank++;
            }
            return rank;
        }
        public Vector3 Formation(SwimmerIdentity player)
        {
            return TeamPoint(Layout(player.FieldPosition).periodStart+ExtraRoleOffset(player));
        }
        public Vector3 TeamPoint(Vector2 coordinates) => Clamp(pool.transform.position+Vector3.Cross(Vector3.up,AttackDirection)*coordinates.x+AttackDirection*coordinates.y);
        private FieldFormation Layout(FieldPositionRole role)
        {
            for(int i=0;i<formation.Length;i++)if(formation[i].role==role)return formation[i];
            return new FieldFormation(role,new Vector2(0f,-7f),new Vector2(0f,5f),new Vector2(0f,-8f));
        }
        private Vector2 ExtraRoleOffset(SwimmerIdentity player)
        {
            int earlier=0;
            for(int i=0;i<roster.FieldPlayers.Count;i++)
            {var other=roster.FieldPlayers[i];if(other==player)break;if(other.Team==team && other.FieldPosition==player.FieldPosition)earlier++;}
            return new Vector2(earlier%2==0?0f:1.8f,-earlier*2f);
        }
        public Vector3 Support(SwimmerIdentity player, SwimmerIdentity carrier)
        {
            float progress=Vector3.Dot(carrier.Motor.Position-pool.transform.position,AttackDirection);
            var slot=Layout(player.FieldPosition);
            float shift=Mathf.Clamp(progress+(carrier.IsFieldPlayer?supportForward:6f)-6.5f,carrier.IsFieldPlayer?-10f:-14f,0f);
            Vector2 point=slot.attack+ExtraRoleOffset(player);point.y+=shift;
            point.x*=Mathf.Max(0.25f,supportWidth)/3f;
            float ballLateral=Vector3.Dot(carrier.Motor.Position-pool.transform.position,Vector3.Cross(Vector3.up,AttackDirection));
            point.x+=Mathf.Clamp(ballLateral*0.18f,-1.2f,1.2f);
            if(Counterattacking && player.FieldPosition!=FieldPositionRole.CentreBack)point.y+=2.5f;
            if(FieldAdvantage>0 && player.FieldPosition!=FieldPositionRole.CentreBack)point.x*=1.12f;
            Vector3 target=TeamPoint(point);
            if(Planar(target-carrier.Motor.Position).sqrMagnitude<9f)
            {
                Vector3 away=player.FieldPosition==FieldPositionRole.CentreBack?-AttackDirection:Planar(target-carrier.Motor.Position).normalized;
                if(away.sqrMagnitude<0.1f)away=-AttackDirection;
                target=carrier.Motor.Position+away*3.5f;
            }
            return Clamp(target);
        }

        internal Vector3 LiveSupport(SwimmerIdentity player, SwimmerIdentity carrier, SwimmerIdentity selected)
        {
            if (supportOwner != ball.Owner || supportPossessionStarted != ball.OwnershipStartedAt ||
                selected != supportSelection || Time.time >= nextSupportSample)
                SampleSupport(carrier, selected);
            for (int i = 0; i < roster.Members.Count; i++)
                if (roster.Members[i] == player && supportAssigned[i]) return supportTargets[i];
            return Support(player, carrier);
        }

        private void SampleSupport(SwimmerIdentity carrier, SwimmerIdentity selected)
        {
            using var measurement = SupportSampling.Auto();
            int count = roster.Members.Count;
            if (sampledPositions == null || sampledPositions.Length != count)
            {
                sampledPositions = new Vector3[count];
                sampledEligible = new bool[count];
                supportTargets = new Vector3[count];
                supportUntil = new float[count];
                supportAssigned = new bool[count];
            }
            Vector3 origin = carrier.Motor.Position;
            bool changed = supportOwner != ball.Owner || supportPossessionStarted != ball.OwnershipStartedAt || selected != supportSelection;
            bool moved = Planar(origin - supportOrigin).sqrMagnitude > 2.25f;
            if (changed || moved) supportOrigin = origin;
            supportOwner = ball.Owner;
            supportPossessionStarted = ball.OwnershipStartedAt;
            supportSelection = selected;
            nextSupportSample = Time.time + SupportInterval;
            for (int i = 0; i < count; i++)
            {
                var athlete = roster.Members[i];
                sampledEligible[i] = Eligible(athlete);
                if (sampledEligible[i]) sampledPositions[i] = athlete.Motor.Position;
                if (changed || !sampledEligible[i] || athlete == carrier || athlete == selected)
                    supportAssigned[i] = false;
            }

            Vector3 forward = AttackDirection;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < count; i++)
            {
                var player = roster.Members[i];
                if (!sampledEligible[i] || player.Team != team || player == carrier || player == selected) continue;
                if (!moved && supportAssigned[i] && Time.time < supportUntil[i]) continue;

                Vector3 anchor = Support(player, carrier);
                Vector3 current = sampledPositions[i];
                bool center = player.FieldPosition == FieldPositionRole.CentreForward;
                float lateral = center ? 1.35f : 2.15f;
                float depth = center ? .95f : 1.65f;
                Vector3 best = ClampSupport(anchor, anchor, player.FieldPosition);
                float score = SupportScore(i, best, anchor, origin);
                // A swimmer already in a useful pocket can hold it. Commit to a new pocket only for a clear improvement.
                Vector3 hold = ClampSupport(current, anchor, player.FieldPosition);
                if (Planar(hold - anchor).sqrMagnitude < 7.3f)
                    ConsiderSupport(i, hold, anchor, origin, ref best, ref score);
                if (supportAssigned[i])
                    ConsiderSupport(i, ClampSupport(supportTargets[i], anchor, player.FieldPosition), anchor, origin, ref best, ref score, .13f);
                for (int side = -1; side <= 1; side += 2)
                {
                    ConsiderSupport(i, ClampSupport(anchor + right * (side * lateral), anchor, player.FieldPosition), anchor, origin, ref best, ref score);
                    ConsiderSupport(i, ClampSupport(anchor + forward * (side * depth), anchor, player.FieldPosition), anchor, origin, ref best, ref score);
                    ConsiderSupport(i, ClampSupport(anchor + (right * lateral - forward * depth) * side, anchor, player.FieldPosition), anchor, origin, ref best, ref score);
                }
                if (!supportAssigned[i] || Planar(best - supportTargets[i]).sqrMagnitude > .20f)
                    supportUntil[i] = Time.time + SupportCommitment;
                supportTargets[i] = best;
                supportAssigned[i] = true;
            }
        }

        private Vector3 ClampSupport(Vector3 point, Vector3 anchor, FieldPositionRole role)
        {
            Vector3 forward = AttackDirection;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            bool wing = role == FieldPositionRole.LeftWing || role == FieldPositionRole.RightWing;
            float width = Vector3.Dot(anchor - pool.transform.position, right);
            float x = Vector3.Dot(point - anchor, right);
            if (wing && Mathf.Abs(width) > 2f)
                x = width < 0f ? Mathf.Min(x, -2f - width) : Mathf.Max(x, 2f - width);
            float z = Mathf.Clamp(Vector3.Dot(point - anchor, forward), -2.4f, 2.4f);
            point = anchor + right * Mathf.Clamp(x, -2.4f, 2.4f) + forward * z;
            float goalDepth = Vector3.Dot(attackingGoal.transform.position - point, forward);
            // Keep off-ball outlets outside the goal area, even when a counterattack pushes the anchor up.
            if (goalDepth < 2.15f) point -= forward * (2.15f - goalDepth);
            return Clamp(point);
        }

        private void ConsiderSupport(int index, Vector3 point, Vector3 anchor, Vector3 origin,
            ref Vector3 best, ref float bestScore, float preference = 0f)
        {
            float score = SupportScore(index, point, anchor, origin) + preference;
            if (score <= bestScore) return;
            best = point;
            bestScore = score;
        }

        private float SupportScore(int index, Vector3 point, Vector3 anchor, Vector3 origin)
        {
            Vector3 lane = Planar(point - origin);
            float rangeSquared = lane.sqrMagnitude;
            float range = Mathf.Sqrt(rangeSquared);
            float defenderGap = 9f, laneGap = 9f, teammateGap = 9f;
            for (int i = 0; i < sampledPositions.Length; i++)
            {
                if (i == index || !sampledEligible[i]) continue;
                Vector3 other = sampledPositions[i];
                float gap = Planar(point - other).sqrMagnitude;
                if (roster.Members[i].Team == team)
                {
                    teammateGap = Mathf.Min(teammateGap, gap);
                    if (supportAssigned[i]) teammateGap = Mathf.Min(teammateGap, Planar(point - supportTargets[i]).sqrMagnitude);
                    continue;
                }
                defenderGap = Mathf.Min(defenderGap, gap);
                float along = Vector3.Dot(Planar(other - origin), lane) / Mathf.Max(.01f, rangeSquared);
                if (along > .08f && along < .98f)
                    laneGap = Mathf.Min(laneGap, Planar(other - origin - lane * along).sqrMagnitude);
            }

            return 1.8f * Mathf.InverseLerp(.12f, laneClearance * laneClearance, laneGap) +
                1.15f * Mathf.InverseLerp(.40f, 5f, defenderGap) +
                1.1f * Mathf.InverseLerp(.80f, 6.25f, teammateGap) -
                Mathf.Max(0f, 3.2f - range) * 1.25f - Mathf.Max(0f, range - 11f) * .21f -
                Planar(point - anchor).magnitude * .23f - Planar(point - sampledPositions[index]).magnitude * .14f;
        }
        public SwimmerIdentity MarkFor(SwimmerIdentity player)
        {
            RefreshAssignments();
            for(int i=0;i<roster.Members.Count;i++)if(roster.Members[i]==player)return marks[i];
            return null;
        }
        public Vector3 Defense(SwimmerIdentity player, SwimmerIdentity carrier)
        {
            SwimmerIdentity mark = MarkFor(player);
            if (mark != null && player.FieldPosition == FieldPositionRole.CentreBack &&
                mark.FieldPosition == FieldPositionRole.CentreForward)
            {
                Vector3 center = mark.Motor.Position;
                Vector3 towardGoal = Planar(defendedGoal.transform.position - center).normalized;
                Vector3 towardBall = Planar(carrier.Motor.Position - center).normalized;
                Vector3 shade = Vector3.ProjectOnPlane(towardBall, towardGoal);
                return Clamp(center + towardGoal * .98f + shade * (defenseStrategy == DefenseStrategy.Zone ? .44f : .26f));
            }
            if (carrier.IsFieldPlayer && Presser == player) return Clamp(SwimmerTactics.Defend(carrier.Motor.Position, defendedGoal.transform.position, pressureDistance));
            if(defenseStrategy==DefenseStrategy.Zone)
            {
                Vector3 station=TeamPoint(Layout(player.FieldPosition).zone+ExtraRoleOffset(player));
                Vector3 lane=mark!=null?Vector3.Lerp(mark.Motor.Position,carrier.Motor.Position,0.25f):carrier.Motor.Position;
                if(FieldAdvantage<0)station=Vector3.Lerp(station,defendedGoal.transform.position,0.15f);
                return Clamp(station+Vector3.ClampMagnitude(Planar(lane-station),3f)*0.4f);
            }
            Vector3 threat = mark != null ? mark.Motor.Position : carrier.Motor.Position + Vector3.Cross(Vector3.up, AttackDirection) * (Slot(player) % 2 == 0 ? -3f : 3f);
            Vector3 goalSide=SwimmerTactics.Defend(threat,defendedGoal.transform.position,1.2f);
            float shapeWeight=FieldAdvantage<0?0.5f:0.12f;
            return Clamp(Vector3.Lerp(goalSide,TeamPoint(Layout(player.FieldPosition).zone+ExtraRoleOffset(player)),shapeWeight));
        }

        internal float BlockPose(SwimmerIdentity player, out Vector3 target)
        {
            UpdateBlock();
            target = blockLook;
            if (blockingPlayer != player || !match.IsPlaying || !Eligible(player)) return 0f;
            float now = Time.time;
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.InverseLerp(blockStarts, blockStarts + .10f, now),
                1f - Mathf.InverseLerp(blockEnds - .07f, blockRecovers, now)));
        }

        internal float BlockMobility(SwimmerIdentity player) =>
            blockingPlayer == player && Time.time >= blockStarts
                ? Mathf.Lerp(.46f, 1f, Mathf.InverseLerp(blockEnds, blockRecovers, Time.time)) : 1f;

        private void UpdateBlock()
        {
            if (blockChecked == Time.frameCount) return;
            blockChecked = Time.frameCount;
            if (!match.IsPlaying || (blockingPlayer != null && Time.time < blockRecovers) ||
                !ball.TryReadShotThreat(out var source, out Vector3 origin, out Vector3 direction, out int shot) ||
                shot == lastBlockShot)
                return;

            var shooter = source.GetComponent<SwimmerIdentity>();
            Vector3 lane = Planar(direction).normalized;
            if (shooter == null || shooter.Team == team ||
                Vector3.Dot(lane, Planar(defendedGoal.transform.position - origin).normalized) < .74f) return;
            float best = float.PositiveInfinity;
            foreach (var player in roster.FieldPlayers)
            {
                if (!Eligible(player) || player.Team != team ||
                    (player != Presser && MarkFor(player) != shooter && ball.Owner != null)) continue;
                Vector3 delta = Planar(player.Motor.Position - origin);
                float distance = delta.magnitude;
                float forward = Vector3.Dot(delta, lane);
                if (distance < 1f || distance > 3.2f || forward < .55f ||
                    Vector3.Dot(player.Motor.Facing, -delta.normalized) < .25f) continue;
                float offset = (delta - lane * forward).magnitude;
                float cost = offset + distance * .15f;
                if (offset > .67f || cost >= best) continue;
                best = cost;
                blockingPlayer = player;
                blockLook = origin + lane * Mathf.Max(0f, forward - .48f) + Vector3.up * .25f;
            }
            if (float.IsPositiveInfinity(best)) return;
            lastBlockShot = shot;
            blockStarts = Time.time + blockReaction;
            blockEnds = blockStarts + blockCommitment;
            blockRecovers = blockEnds + blockRecovery;
        }
        public float Pressure(Vector3 position)
        {
            float nearest = 100f;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (Eligible(p) && p.Team != team) nearest = Mathf.Min(nearest, Planar(p.Motor.Position - position).magnitude);
            }
            return Mathf.Clamp01(1f - nearest / 3f);
        }
        public bool LaneClear(Vector3 from, Vector3 to, float clearance = 0.9f)
        {
            Vector3 line = Planar(to - from); float length = line.sqrMagnitude;
            if (length < 0.01f) return false;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team == team) continue;
                float t = Vector3.Dot(Planar(p.Motor.Position - from), line) / length;
                if (t > 0.08f && t < 0.95f && Planar(p.Motor.Position - (from + line * t)).sqrMagnitude < clearance * clearance) return false;
            }
            return true;
        }
        public float ShotQuality(Vector3 position)
        {
            Vector3 local = attackingGoal.transform.InverseTransformPoint(position);
            float distance = Planar(attackingGoal.transform.position - position).magnitude;
            float effectiveRange=Mathf.Min(shootingRange,ball.EstimateFlatTravelRange(aiShotSpeed)*0.85f);
            if (local.z >= -1f || distance > effectiveRange) return 0f;
            float angle = Mathf.Clamp01(1f - Mathf.Abs(local.x) / 10f);
            Vector3 aim=ShotTarget();
            float keeperPenalty=0f;
            for(int i=0;i<roster.Members.Count;i++)
            {var member=roster.Members[i];if(!member.IsFieldPlayer && member.Team!=team && Vector3.Distance(member.Motor.Position,aim)<2f)keeperPenalty=0.08f;}
            return Mathf.Clamp01(0.5f * (1f - distance / Mathf.Max(0.1f,effectiveRange * 1.5f)) + 0.3f * angle + (LaneClear(position, aim) ? 0.2f : 0f) - Pressure(position) * 0.2f - keeperPenalty);
        }
        public Vector3 ShotTarget()
        {
            float x=0f;
            float corner=Mathf.Min(attackingGoal.Width*0.3f,Mathf.Max(0f,attackingGoal.Width*0.5f-ball.Radius-0.05f));
            for(int i=0;i<roster.Members.Count;i++)
            {
                var member=roster.Members[i];
                if(!member.IsFieldPlayer && member.Team!=team)
                {float keeperX=attackingGoal.transform.InverseTransformPoint(member.Motor.Position).x;x=keeperX>=0f?-corner:corner;break;}
            }
            Vector3 point=attackingGoal.transform.TransformPoint(new Vector3(x,0f,0f));point.y=pool.Waterline;return point;
        }
        public Vector3 CarryTarget(SwimmerIdentity carrier)
        {
            Vector3 target=attackingGoal.transform.position-AttackDirection*4.5f;
            Vector3 right=Vector3.Cross(Vector3.up,AttackDirection);
            // Once the nearest marker is behind the carrier, keep driving the open route to goal.
            if(LaneClear(carrier.Motor.Position,target,laneClearance))return Clamp(target);
            if(!LaneClear(carrier.Motor.Position,target,laneClearance))
            {
                Vector3 left=target-right*3f,rightLane=target+right*3f;
                float leftPressure=Pressure(left),rightPressure=Pressure(rightLane);
                if(carryLane==0)carryLane=leftPressure<=rightPressure?-1:1;
                else if(carryLane<0 && rightPressure+0.15f<leftPressure)carryLane=1;
                else if(carryLane>0 && leftPressure+0.15f<rightPressure)carryLane=-1;
                target=carryLane<0?left:rightLane;
            }
            return Clamp(target);
        }
        public SwimmerIdentity BetterPass(SwimmerIdentity carrier)
        {
            SwimmerIdentity best = null; float bestScore = 0.12f;
            Vector3 origin = carrier.Motor.Position;
            float currentDistance = Planar(attackingGoal.transform.position - origin).magnitude;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team != team || p == carrier) continue;
                Vector3 delta = Planar(p.Motor.Position - origin); float distance = delta.magnitude;
                if (distance < 2.5f || distance > 11f || !LaneClear(origin, p.Motor.Position,laneClearance)) continue;
                float progress = currentDistance - Planar(attackingGoal.transform.position - p.Motor.Position).magnitude;
                float alignment=Vector3.Dot(delta/distance,AttackDirection);
                float score = progress * passProgressWeight + (Pressure(origin) - Pressure(p.Motor.Position)) * passOpennessWeight + ShotQuality(p.Motor.Position)*0.2f + alignment*0.05f - distance*0.005f;
                if(p==lastPasser && carrier==lastReceiver && Time.time-lastPassTime<6f)score-=0.65f;
                if(Counterattacking && progress>2f)score+=0.2f;
                if (score > bestScore) { best = p; bestScore = score; }
            }
            return best;
        }
        private void RefreshAssignments()
        {
            if(automaticDefense && Time.time>=nextDefenseChoice)
            {
                nextDefenseChoice=Time.time+3f;
                float danger=Planar(ball.PhysicsPosition-defendedGoal.transform.position).magnitude;
                if(danger<7f)SetDefense(DefenseStrategy.Press);else if(danger>11f)SetDefense(DefenseStrategy.Zone);
            }
            if (assignedOwner == ball.Owner && Time.time < nextAssignment) return;
            bool changed = assignedOwner != ball.Owner; assignedOwner = ball.Owner;
            nextAssignment = Time.time + assignmentInterval;
            Vector3 point = Carrier != null ? Carrier.Motor.Position : ball.IsAirborne?ball.LandingPoint:ball.PhysicsPosition;
            var previous = ball.Owner != null ? presser : loosePursuer;
            SwimmerIdentity nearest = null; float best = float.PositiveInfinity;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team != team) continue;
                float distance = Planar(p.Motor.Position - point).magnitude;
                if(Carrier!=null && Carrier.Team!=team)
                {
                    distance+=Mathf.Clamp(Vector3.Dot(p.Motor.Position-point,AttackDirection),0f,5f)*0.35f;
                    if (p.FieldPosition == FieldPositionRole.CentreBack && Carrier.FieldPosition != FieldPositionRole.CentreForward)
                        distance += 2.25f;
                }
                if (!changed && p == previous) distance -= reassignmentAdvantage;
                if (distance < best) { nearest = p; best = distance; }
            }
            if (ball.Owner != null) presser = Carrier!=null && Carrier.IsFieldPlayer && Carrier.Team!=team ? nearest:null;
            else loosePursuer = nearest;
            AssignMarks();
        }

        private void AssignMarks()
        {
            if(marks==null || marks.Length!=roster.Members.Count)
            {marks=new SwimmerIdentity[roster.Members.Count];previousMarks=new SwimmerIdentity[marks.Length];usedThreats=new bool[marks.Length];}
            System.Array.Copy(marks,previousMarks,marks.Length);System.Array.Clear(marks,0,marks.Length);System.Array.Clear(usedThreats,0,usedThreats.Length);
            var carrier = Carrier;
            if (carrier != null && carrier.Team != team)
            {
                int centerAttack = -1, centerDefense = -1;
                for (int i = 0; i < roster.Members.Count; i++)
                {
                    var player = roster.Members[i];
                    if (!Eligible(player)) continue;
                    if (player.Team == team && player.FieldPosition == FieldPositionRole.CentreBack) centerDefense = i;
                    if (player.Team != team && player.FieldPosition == FieldPositionRole.CentreForward) centerAttack = i;
                }
                if (centerAttack >= 0 && centerDefense >= 0)
                {
                    var threat = roster.Members[centerAttack];
                    var marker = roster.Members[centerDefense];
                    if (marker != presser || carrier == threat)
                    {
                        marks[centerDefense] = threat;
                        usedThreats[centerAttack] = true;
                    }
                }
            }
            // Most dangerous threats first; unique nearest suitable defender with incumbent hysteresis.
            for(int count=0;count<roster.FieldPlayers.Count;count++)
            {
                int threatIndex=-1;float danger=float.PositiveInfinity;
                for(int i=0;i<roster.Members.Count;i++)
                {
                    var p=roster.Members[i];if(!Eligible(p) || p.Team==team || p==Carrier || usedThreats[i])continue;
                    float d=Planar(p.Motor.Position-defendedGoal.transform.position).sqrMagnitude;
                    if(d<danger){danger=d;threatIndex=i;}
                }
                if(threatIndex<0)break;usedThreats[threatIndex]=true;
                var threat=roster.Members[threatIndex];int defender=-1;float cost=float.PositiveInfinity;
                Vector3 goalSide=SwimmerTactics.Defend(threat.Motor.Position,defendedGoal.transform.position,1.2f);
                for(int i=0;i<roster.Members.Count;i++)
                {
                    var p=roster.Members[i];if(!Eligible(p) || p.Team!=team || p==presser || marks[i]!=null)continue;
                    float d=Planar(p.Motor.Position-goalSide).magnitude;
                    if(previousMarks[i]==threat)d-=reassignmentAdvantage;
                    if(threat.FieldPosition==FieldPositionRole.CentreForward && p.FieldPosition==FieldPositionRole.CentreBack)d-=1.5f;
                    if(d<cost){cost=d;defender=i;}
                }
                if(defender>=0)marks[defender]=threat;
            }
        }

        private void Awake()
        {
            if (roster == null || ball == null || pool == null || match == null || attackingGoal == null || defendedGoal == null || attackingGoal.ScoringTeam != team || defendedGoal.ScoringTeam == team)
            {
                Debug.LogError("PrototypeTeamTactics requires a roster, ball, pool and correctly assigned opposing goals.", this);
                enabled = false;
            }
        }
    }
}
