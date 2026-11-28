using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum MatchPhase { PreMatch, Playing, GoalRestart, TurnoverRestart, QuarterBreak, FullTime, HalfTime,
        FreeThrowSetup, FreeThrowReady, PenaltySetup, PenaltyReady, PenaltyFlight, CentreRestartSetup, CentreRestartReady, SprintSetup, SprintRace, PreMatchPresentation }
    /// <summary>Authoritative phases, score evaluation and transitions. Clocks advance only in Playing.</summary>
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class PrototypeMatch : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private GoalMouth[] goals;
        [SerializeField] private SwimmerMotor swimmer;
        [SerializeField] private Transform swimmerStart, ballStart;
        [SerializeField] private GameplayCamera gameplayCamera;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PlayerSelection playerSelection;
        internal PlayerSelection Selection => playerSelection;
        [SerializeField, Min(0.1f)] private float restartDelay = 1.5f;
        [SerializeField] private MatchClock clock = new MatchClock();
        [SerializeField] private PossessionShotClock shotClock;
        [SerializeField] private PrototypeTeamTactics[] teams = new PrototypeTeamTactics[0];
        [SerializeField] private PrototypeGoalkeeper[] keepers = new PrototypeGoalkeeper[0];
        private GoalMouth[] initialAttackGoals,initialDefendGoals;
        public bool EndsSwitched { get; private set; }
        public int EndSwitchCount { get; private set; }
        [SerializeField, Min(0.1f)] private float turnoverDelay = 1f;
        private PrototypeTeam? restartTeam;
        public MatchPhase Phase { get; private set; } = MatchPhase.Playing;
        public bool IsPlaying => !IsPaused && Phase==MatchPhase.Playing;
        public bool IsPaused { get; private set; }
        private PreMatchPresentation preMatch;
        public bool IsPreMatchPresentation => Phase==MatchPhase.PreMatchPresentation;
        internal void AttachPreMatch(PreMatchPresentation presentation) => preMatch=presentation;

        internal void FinishPreMatch(PreMatchPresentation presentation)
        {
            if (!IsPreMatchPresentation || IsPaused || presentation!=preMatch) return;
            BeginPeriodSprint();
            gameplayCamera?.SnapToTarget();
        }
        [SerializeField] private PeriodSprintOff periodSprint;
        public PeriodSprintOff SprintOff=>periodSprint;
        public GameplayCamera GameplayView=>gameplayCamera;
        public Vector3 PeriodCenter=>new Vector3(ballStart.position.x,ball.SurfaceHeight,ballStart.position.z);
        public bool IsSprintRace=>Phase==MatchPhase.SprintRace;
        private bool awaitingOpeningPossession;
        public bool AwaitingOpeningPossession=>awaitingOpeningPossession;
        public bool IsPeriodSprint=>Phase==MatchPhase.SprintSetup||IsSprintRace;
        public bool CanSwim=>!IsPaused&&(Phase==MatchPhase.Playing||IsSprintRace);
        public event Action SprintStarted;
        // Gameplay stoppage deadlines share a pause-aware unscaled timeline. UI animation uses real unscaled time.
        public float PresentationTime => (IsPaused ? pausedAt : Time.unscaledTime) - pausedSeconds;
        private float pausedAt, pausedSeconds, resumeTimeScale = 1f;
        private bool resumeAudioPause;
        public event Action<bool> PauseChanged;
        [SerializeField] private MatchRules rules;
        public MatchRules Rules => rules;
        public bool BallInMotion => !IsPaused && (IsPlaying || IsSprintRace || Phase == MatchPhase.PenaltyFlight);
        public MatchClock Clock => clock;
        public PossessionShotClock ShotClock => shotClock;
        public string Result => BlueScore==WhiteScore?"DRAW":BlueScore>WhiteScore?"WIN":"LOSE";

        private Rigidbody ballBody;
        private SwimmerInput movementInput;
        private BallInteractionInput interactionInput;
        private Vector3 previousPosition;
        private int previousMotion, lastScoredShot = -1;
        private bool restoreMotor, restoreMovementInput, restoreInteractionInput;
        private float restartAt;
        [SerializeField, Min(0.1f)] private float centrePreparationDelay=0.85f;
        [SerializeField, Min(0.1f)] private float aiCentrePassDelay=0.45f;
        public SwimmerIdentity CentreTaker { get; private set; }
        public SwimmerIdentity CentreReceiver { get; private set; }
        public bool IsCentreRestart => Phase==MatchPhase.CentreRestartSetup || Phase==MatchPhase.CentreRestartReady;
        public int CentreSignals { get; private set; }
        public event Action<SwimmerIdentity> CentreRestartSignalled;
        private float centreReadyAt, centrePassAt;

        public int BlueScore { get; private set; }
        public int WhiteScore { get; private set; }
        public bool IsRestarting => Phase==MatchPhase.GoalRestart || Phase==MatchPhase.TurnoverRestart;
        public GoalAttribution LastGoalAttribution { get; private set; }
        public GoalReplayDirector GoalPresentation { get; private set; }
        public bool GoalPresentationActive => GoalPresentation != null && GoalPresentation.Active;
        internal bool GoalRecoveryActive => !IsPaused && !EndingPenalty && Phase == MatchPhase.GoalRestart &&
            GoalPresentationActive && GoalPresentation.Phase == GoalPresentationPhase.Celebration;
        internal void AttachGoalPresentation(GoalReplayDirector director) => GoalPresentation = director;
        internal void ReleaseGoalPresentation()
        {
            if (Phase == MatchPhase.GoalRestart) restartAt = PresentationTime;
        }
        public Guid MatchReplayId { get; private set; }
        public float RestartDelay => restartDelay;
        public event Action Changed;
        public event Action NewMatch;

        private void Awake()
        {
            if (ball == null || swimmer == null || swimmerStart == null || ballStart == null || goals == null || goals.Length != 2)
            {
                Debug.LogError("PrototypeMatch requires the ball, swimmer, start markers and two goals.", this);
                enabled = false;
                return;
            }
            ballBody = ball.GetComponent<Rigidbody>();
            movementInput = swimmer.GetComponent<SwimmerInput>();
            interactionInput = swimmer.GetComponent<BallInteractionInput>();
            clock.Reset();
            CaptureInitialEnds();
        }

        private void OnEnable()
        {
            if (ballBody == null)
                return;
            previousPosition = ballBody.position;
            previousMotion = ball.MotionRevision;
            ball.Released += CentreReleased;
            ball.PossessionChanged+=SprintAcquired;
        }
        private void Start()
        {
            if(periodSprint==null)return;
            if(preMatch==null || !preMatch.isActiveAndEnabled || !preMatch.CanBegin)
            {
                BeginPeriodSprint();
                return;
            }
            MatchReplayId=Guid.NewGuid();
            Phase=MatchPhase.PreMatchPresentation;
            StopPlay(true);
        }
        private void BeginPeriodSprint()
        {
            Phase=MatchPhase.SprintSetup;awaitingOpeningPossession=true;CentreTaker=CentreReceiver=null;restartTeam=null;
            StopPlay(true);shotClock.ResetPossession();periodSprint.Prepare();
            playerSelection.SetInputLocked(false);if(periodSprint.HumanSprinter!=null)playerSelection.Select(periodSprint.HumanSprinter);playerSelection.SetInputLocked(true);
            Changed?.Invoke();
        }
        private void ReleasePeriodSprint()
        {
            Phase=MatchPhase.SprintRace;periodSprint.Begin();
            roster.SetSimulationPaused(false);playerSelection.SetInputLocked(false);
            ball.ReleasePeriodBall();previousPosition=ballBody.position;previousMotion=ball.MotionRevision;
            Changed?.Invoke();SprintStarted?.Invoke();
        }
        private void SprintAcquired(SwimmerBallInteraction previous,SwimmerBallInteraction next)
        {
            if(!awaitingOpeningPossession||IsPaused||next==null)return;
            Phase=MatchPhase.Playing;awaitingOpeningPossession=false;periodSprint.Finish();
            playerSelection.RestorePossessionSelection();
            previousPosition=ballBody.position;previousMotion=ball.MotionRevision;Changed?.Invoke();
        }
        private void EndUnclaimedSprint()
        {
            // Keep the real loose ball and stopped opening clocks; unlock selection and ordinary pursuit.
            Phase=MatchPhase.Playing;periodSprint.EndUnclaimedContest();playerSelection.RefreshEligibility();Changed?.Invoke();
        }

        private void FixedUpdate()
        {
            if (!BallInMotion)
                return;
            Vector3 current = ballBody.position;
            // Release spawn changes are not travel. Start the first segment at the recorded release point.
            Vector3 from = previousMotion == ball.MotionRevision ? previousPosition : ball.MotionOrigin;
            previousPosition = current;
            previousMotion = ball.MotionRevision;
            if (ball.CanScoreGoal && ball.Owner == null && ball.State != BallState.Held && lastScoredShot != ball.ShotSequence)
            foreach (GoalMouth goal in goals)
            {
                if (goal != null && goal.isActiveAndEnabled && goal.TryGetCrossing(from, current, ball.Radius, out _))
                {
                    RegisterGoal(goal);
                    return;
                }
            }
            if (ball.Owner == null && rules != null && ball.TryGetFieldExit(from, current, out var exit))
            {
                if (EndingPenalty) { FinishPeriod(); return; }
                if (rules.ResolvePendingPenalty() || rules.AwardBoundaryRestart(exit)) return;
            }
            // A completed crossing observed this step wins over the quarter/shot-clock deadline.
            if(!IsPlaying)return;
            AdvanceLiveTime(Time.fixedDeltaTime);
        }

        /// <summary>One clock path for live physics and deterministic simulation. Never runs during stoppages.</summary>
        internal bool EndingPenalty { get; private set; }
        public void AdvanceLiveTime(float seconds)
        {
            if(!IsPlaying || awaitingOpeningPossession || seconds<=0f || float.IsNaN(seconds) || float.IsInfinity(seconds))return;
            float step=Mathf.Min(seconds,clock.Remaining);
            if(shotClock!=null)step=Mathf.Min(step,shotClock.LiveSecondsRemaining);
            if(rules!=null)rules.TickLive(step);
            if(clock.Remaining<=0f || clock.Tick(step))
            {
                EndingPenalty = true;
                if (rules != null && rules.ResolvePendingPenalty()) return;
                FinishPeriod();
                return;
            }
            if (shotClock != null && step >= shotClock.LiveSecondsRemaining && rules != null && rules.ResolvePendingPenalty()) return;
            if(shotClock!=null)shotClock.Tick(step);
        }

        private void FinishPeriod()
        {
            EndingPenalty = false;
            Phase = clock.Quarter >= clock.Quarters ? MatchPhase.FullTime :
                clock.Quarter == 2 ? MatchPhase.HalfTime : MatchPhase.QuarterBreak;
            StopPlay(true);
        }

        private void RegisterGoal(GoalMouth goal)
        {
            PrototypeTeam team=goal.ScoringTeam;
            lastScoredShot = ball.ShotSequence;
            LastGoalAttribution = ball.AttributeGoal(team);
            if (team == PrototypeTeam.Blue)
                BlueScore++;
            else
                WhiteScore++;
            if(rules!=null)rules.GoalScored();
            Phase=MatchPhase.GoalRestart;
            if (GoalPresentation != null && GoalPresentation.isActiveAndEnabled) GoalPresentation.Begin(goal,ball);
            if (EndingPenalty && !GoalPresentationActive) { FinishPeriod(); return; }
            restartTeam=team==PrototypeTeam.Blue?PrototypeTeam.White:PrototypeTeam.Blue;
            ball.CaptureGoal(goal);
            restartAt = PresentationTime + restartDelay;
            StopPlay(false, GoalRecoveryActive);
        }

        private void StopPlay(bool clearBall, bool keepAthletesMoving = false)
        {
            if (roster != null && playerSelection != null)
            {
                playerSelection.SetInputLocked(true);
                if (!keepAthletesMoving) roster.SetSimulationPaused(true);
            }
            else
            {
                // Keep single-swimmer scenes usable until the explicit team setup is run.
                restoreMotor = swimmer.enabled;
                restoreMovementInput = movementInput != null && movementInput.enabled;
                restoreInteractionInput = interactionInput != null && interactionInput.enabled;
                if (movementInput != null) movementInput.enabled = false;
                if (interactionInput != null) interactionInput.enabled = false;
                swimmer.enabled = false;
            }
            if(clearBall)ball.ResetForRestart(ballStart.position);
            if (!ball.AwaitingReplacement) ball.SetSimulationPaused(true);
            Changed?.Invoke();
        }

        public void RequestTurnover(PrototypeTeam awardedTeam)
        {
            if(!IsPlaying)return;
            Phase=MatchPhase.TurnoverRestart;restartTeam=awardedTeam;
            restartAt=PresentationTime+turnoverDelay;StopPlay(true);
        }

        public void BeginRuleRestart(bool penalty, bool keepAthletesMoving = false)
        {
            if (!IsPlaying) return;
            Phase = penalty ? MatchPhase.PenaltySetup : MatchPhase.FreeThrowSetup;
            StopPlay(true, keepAthletesMoving);
        }
        internal void BeginBoundaryStoppage()
        {
            if (IsSprintRace) periodSprint.EndUnclaimedContest();
            awaitingOpeningPossession = false;
            restartTeam = null;
            Phase = MatchPhase.FreeThrowSetup;
            StopPlay(false);
        }

        public void ReadyRuleRestart(bool penalty)
        {
            if (IsPaused || (Phase != MatchPhase.FreeThrowSetup && Phase != MatchPhase.PenaltySetup)) return;
            Phase = penalty ? MatchPhase.PenaltyReady : MatchPhase.FreeThrowReady;
            roster.SetSimulationPaused(false);
            // MatchRules retains the protected mark and permits ordinary-foul support movement.
            ball.SetSimulationPaused(false);
            playerSelection.SetInputLocked(false);
            if (rules.Taker.Team == playerSelection.HumanTeam) playerSelection.Select(rules.Taker);
            previousPosition = ballBody.position; previousMotion = ball.MotionRevision;
            Changed?.Invoke();
        }
        public void LaunchPenalty()
        {
            if (IsPaused || Phase != MatchPhase.PenaltyReady) return;
            Phase = MatchPhase.PenaltyFlight;
            playerSelection.SetInputLocked(true);
            Changed?.Invoke();
        }
        public void EndRuleRestart()
        {
            if (IsPaused || rules == null || !rules.IsRulePhase) return;
            if (EndingPenalty) { FinishPeriod(); return; }
            Phase = MatchPhase.Playing;
            roster.SetSimulationPaused(false); ball.SetSimulationPaused(false);
            playerSelection.SetInputLocked(false);
            if (ball.Owner != null)
            {
                var carrier = ball.Owner.GetComponent<SwimmerIdentity>();
                if (carrier.Team == playerSelection.HumanTeam) playerSelection.Select(carrier);
            }
            previousPosition = ballBody.position; previousMotion = ball.MotionRevision;
            Changed?.Invoke();
        }

        public void ContinueMatch()
        {
            if (IsPaused) return;
            if(Phase==MatchPhase.PreMatch){RestartMatch();return;}
            if(Phase!=MatchPhase.QuarterBreak && Phase!=MatchPhase.HalfTime)return;
            bool half=Phase==MatchPhase.HalfTime;
            if(half)SetEnds(true);
            clock.NextQuarter();restartTeam=null;if(periodSprint!=null)BeginPeriodSprint();else CompleteRestart(half);
        }
        public void RestartMatch()=>PrepareMatch(true);
        public void ReturnToPrototype()=>PrepareMatch(false);
        private void PrepareMatch(bool play)
        {
            SetPaused(false);
            EndingPenalty = false;
            CentreTaker=CentreReceiver=null;CentreSignals=0;awaitingOpeningPossession=false;
            MatchReplayId = Guid.NewGuid();
            Phase=MatchPhase.PreMatch;StopPlay(true);
            SetEnds(false);EndSwitchCount=0;
            BlueScore=WhiteScore=0;lastScoredShot=-1;LastGoalAttribution=default;clock.Reset();restartTeam=null;
            NewMatch?.Invoke();
            if(play){if(periodSprint!=null)BeginPeriodSprint();else CompleteRestart();}
            else {if(roster!=null)roster.ResetPositions();Changed?.Invoke();}
        }

        private void Update()
        {
            if(IsPaused)return;
            if(IsPreMatchPresentation && (preMatch==null || !preMatch.isActiveAndEnabled))
            {
                BeginPeriodSprint();
                gameplayCamera?.SnapToTarget();
            }
            if(Phase==MatchPhase.SprintSetup&&periodSprint!=null)
            {if(periodSprint.NeedsRestage)BeginPeriodSprint();if(periodSprint.Ready)ReleasePeriodSprint();}
            if(IsSprintRace){periodSprint.Tick();if(periodSprint.ContestExpired)EndUnclaimedSprint();}
            if (GoalRecoveryActive) DriveGoalRecovery();
            if (IsRestarting && !GoalPresentationActive && PresentationTime>=restartAt)
            {
                if (Phase==MatchPhase.GoalRestart)
                {
                    if (EndingPenalty) FinishPeriod();
                    else BeginCentreRestart();
                }
                else CompleteRestart();
            }
            if(Phase==MatchPhase.CentreRestartSetup && PresentationTime>=centreReadyAt)ReadyCentreRestart();
            if(Phase==MatchPhase.CentreRestartReady && CentreTaker.Team!=playerSelection.HumanTeam && PresentationTime>=centrePassAt)
                TryCentrePass(CentreTaker, BallActionSource.RuleRestart);
        }

        private void CompleteRestart(bool smoothCamera = false)
        {
            if (roster != null) roster.ResetPositions();
            else swimmer.ResetPose(swimmerStart.position, swimmerStart.rotation);
            ball.ResetForRestart(ballStart.position);
            if(shotClock!=null)shotClock.ResetPossession();
            Phase=MatchPhase.Playing;
            SwimmerIdentity awarded=null;
            if(restartTeam.HasValue && roster!=null)
            {
                foreach(var player in roster.FieldPlayers)
                    if(player.Team==restartTeam.Value && player.EligibleForPlay){awarded=player;break;}
                if(awarded!=null)
                {
                    ball.ResetForRestart(awarded.Motor.Position+awarded.Motor.Facing*1f);
                    ball.TryCollect(awarded.Interaction);
                }
            }
            restartTeam=null;
            previousPosition = ballBody.position;
            previousMotion = ball.MotionRevision;
            if (roster != null && playerSelection != null)
            {
                roster.SetSimulationPaused(false);
                playerSelection.SetInputLocked(false);
                if(awarded!=null && awarded.Team==playerSelection.HumanTeam)playerSelection.Select(awarded);
            }
            else
            {
                swimmer.enabled = restoreMotor;
                if (movementInput != null) movementInput.enabled = restoreMovementInput;
                if (interactionInput != null) interactionInput.enabled = restoreInteractionInput;
            }
            if (gameplayCamera != null && !smoothCamera) gameplayCamera.SnapToTarget();
            Changed?.Invoke();
        }

        private PrototypeTeamTactics Team(PrototypeTeam team)
        {foreach(var t in teams)if(t.Team==team)return t;return null;}

        private void DriveGoalRecovery()
        {
            if (roster == null) return;
            foreach (var athlete in roster.FieldPlayers)
            {
                if (!athlete.EligibleForPlay) continue;
                var motor = athlete.Motor;
                var direction = Team(athlete.Team).AttackDirection;
                var relative = motor.Position - ballStart.position;
                float depth = Vector3.Dot(relative, direction);
                if (GoalPresentation != null && GoalPresentation.TryReadReaction(athlete, out var reaction) &&
                    reaction.HoldPosition)
                {
                    motor.SetMoveInput(Vector2.zero);
                    motor.SetFacingDirection(reaction.LookAt - motor.Position);
                    continue;
                }
                if (depth <= -3.15f)
                {
                    motor.SetMoveInput(Vector2.zero);
                    motor.SetFacingDirection(direction);
                    continue;
                }
                Vector3 move = -direction * Mathf.Clamp((depth + 3.15f) * .26f, .12f, .54f);
                motor.SetFacingDirection(move);
                motor.SetMoveInput(new Vector2(move.x, move.z));
            }
        }

        private void BeginCentreRestart()
        {
            if(Phase!=MatchPhase.GoalRestart || !restartTeam.HasValue)return;
            var awarded=restartTeam.Value;restartTeam=null;CentreTaker=CentreReceiver=null;
            // Stable designated field taker; never assign a goalkeeper or an excluded athlete.
            foreach(var p in roster.FieldPlayers)
                if(p.Team==awarded && p.EligibleForPlay && (CentreTaker==null || p.CapNumber<CentreTaker.CapNumber))CentreTaker=p;
            if(CentreTaker==null){Debug.LogWarning("No eligible field athlete for centre restart; match remains stopped.",this);return;}
            Vector3 center=ballStart.position;center.y=CentreTaker.Motor.Position.y;
            foreach(var p in roster.FieldPlayers)
            {
                if(!p.EligibleForPlay || p==CentreTaker)continue;
                p.Motor.StopMotion();
                var direction=Team(p.Team).AttackDirection;Vector3 point=p.Motor.Position;
                float depth=Vector3.Dot(point-center,direction);
                if(depth>-3f)
                {
                    point = p.StartPosition.position;
                    float startDepth = Vector3.Dot(point - center, direction);
                    if (startDepth > -3f) point -= direction * (startDepth + 3f);
                    p.Motor.ResetPose(point,Quaternion.LookRotation(direction));
                }
                if(p.Team==awarded && (CentreReceiver==null || (point-center).sqrMagnitude<(CentreReceiver.Motor.Position-center).sqrMagnitude))CentreReceiver=p;
            }
            if(CentreReceiver!=null)
            {
                var direction=Team(awarded).AttackDirection;var right=Vector3.Cross(Vector3.up,direction);
                CentreReceiver.Motor.ResetPose(center+right*3.8f-direction*3.5f,Quaternion.LookRotation(direction));
            }
            Vector3 facing=CentreReceiver!=null?CentreReceiver.Motor.Position-center:-Team(awarded).AttackDirection;
            CentreTaker.Motor.ResetPose(center,Quaternion.LookRotation(facing));
            Phase=MatchPhase.CentreRestartSetup;
            ball.ResetForRestart(center);ball.TryAwardRestart(CentreTaker.Interaction);
            if(shotClock!=null)shotClock.AwardCentreRestart(awarded);
            centreReadyAt=PresentationTime+centrePreparationDelay;
            previousPosition=ballBody.position;previousMotion=ball.MotionRevision;
            Changed?.Invoke();
        }
        private void ReadyCentreRestart()
        {
            if(IsPaused || Phase!=MatchPhase.CentreRestartSetup)return;
            Phase=MatchPhase.CentreRestartReady;roster.SetSimulationPaused(false);ball.SetSimulationPaused(false);
            bool human=CentreTaker.Team==playerSelection.HumanTeam;
            playerSelection.SetInputLocked(!human);
            if(human)playerSelection.Select(CentreTaker);
            centrePassAt=PresentationTime+aiCentrePassDelay;
            CentreSignals++;Changed?.Invoke();CentreRestartSignalled?.Invoke(CentreTaker);
        }
        public bool CanCentreRelease(SwimmerBallInteraction player,BallReleaseKind kind)=>!IsPaused &&
            Phase==MatchPhase.CentreRestartReady && CentreTaker!=null && CentreTaker.EligibleForPlay &&
            CentreTaker.Interaction==player && kind==BallReleaseKind.Pass;
        public bool IsCentreReceiver(SwimmerIdentity receiver)=>receiver!=null && CentreTaker!=null && receiver!=CentreTaker &&
            receiver.IsFieldPlayer && receiver.EligibleForPlay && receiver.Team==CentreTaker.Team && roster.Contains(receiver) &&
            Vector3.Dot(receiver.Motor.Position-ballStart.position,Team(receiver.Team).AttackDirection)<=0f;
        public bool TryCentrePass(SwimmerIdentity player, BallActionSource source = BallActionSource.HumanInput)
        {
            if(player==null || !CanCentreRelease(player.Interaction,BallReleaseKind.Pass) || !IsCentreReceiver(CentreReceiver))return false;
            return player.GetComponent<SwimmerPassing>().TryPassTo(CentreReceiver, source);
        }
        private void CentreReleased(SwimmerBallInteraction player,BallReleaseKind kind)
        {
            if(!CanCentreRelease(player,kind))return;
            // Release has already created a physical ball. Resume once, without resetting or assigning it.
            Phase=MatchPhase.Playing;CentreTaker=CentreReceiver=null;
            playerSelection.SetInputLocked(false);
            previousPosition=ballBody.position;previousMotion=ball.MotionRevision;Changed?.Invoke();
        }

        private void CaptureInitialEnds()
        {
            if(initialAttackGoals!=null)return;
            initialAttackGoals=new GoalMouth[teams.Length];initialDefendGoals=new GoalMouth[teams.Length];
            for(int i=0;i<teams.Length;i++){initialAttackGoals[i]=teams[i].AttackingGoal;initialDefendGoals[i]=teams[i].DefendedGoal;}
        }
        private void SetEnds(bool reversed)
        {
            CaptureInitialEnds();
            if(reversed && !EndsSwitched)EndSwitchCount++;
            EndsSwitched=reversed;
            for(int i=0;i<teams.Length;i++)
            {
                var attack=reversed?initialDefendGoals[i]:initialAttackGoals[i];var defend=reversed?initialAttackGoals[i]:initialDefendGoals[i];
                teams[i].SetGoals(attack,defend);attack.SetScoringTeam(teams[i].Team);
                if(teams[i].Team==playerSelection.HumanTeam && gameplayCamera!=null)gameplayCamera.SetAttackingGoal(attack);
            }
            for(int i=0;i<keepers.Length;i++)
                for(int j=0;j<teams.Length;j++)if(keepers[i].Identity.Team==teams[j].Team)keepers[i].SetDefendedGoal(teams[j].DefendedGoal);
            if(roster!=null)roster.SetEndsReversed(reversed);
        }

        /// <summary>Suspension preserves phase, body velocities and possession without applying a restart freeze.</summary>
        public void SetPaused(bool paused)
        {
            if (paused == IsPaused || (paused && (Phase == MatchPhase.PreMatch || Phase == MatchPhase.FullTime))) return;
            if (paused)
            {
                pausedAt = Time.unscaledTime; resumeTimeScale = Time.timeScale; resumeAudioPause = AudioListener.pause;
                IsPaused = true; Time.timeScale = 0f; AudioListener.pause = true;
            }
            else
            {
                pausedSeconds += Time.unscaledTime - pausedAt; IsPaused = false;
                Time.timeScale = resumeTimeScale; AudioListener.pause = resumeAudioPause;
            }
            if (playerSelection != null && playerSelection.ActivePlayer != null)
            {
                var active = playerSelection.ActivePlayer;
                active.GetComponent<BallInteractionInput>()?.ResetAfterMenu();
                if (!paused) active.GetComponent<SwimmerInput>()?.ResetAfterMenu();
            }
            PauseChanged?.Invoke(paused);
        }

        private void OnDisable()
        {
            SetPaused(false);
            if(ball!=null)ball.Released-=CentreReleased;
            if(ball!=null)ball.PossessionChanged-=SprintAcquired;
            // Scene teardown may already have destroyed goals/keepers. Unsubscribe and restore
            // suspension here; gameplay resets belong to the explicit restart/period commands.
        }
    }
}
