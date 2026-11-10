using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum MatchPhase { PreMatch, Playing, GoalRestart, TurnoverRestart, QuarterBreak, FullTime, HalfTime }
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
        public bool IsPlaying => Phase==MatchPhase.Playing;
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

        public int BlueScore { get; private set; }
        public int WhiteScore { get; private set; }
        public bool IsRestarting => Phase==MatchPhase.GoalRestart || Phase==MatchPhase.TurnoverRestart;
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
        }

        private void FixedUpdate()
        {
            if (!IsPlaying)
                return;
            Vector3 current = ballBody.position;
            // Release spawn changes are not travel. Start the first segment at the recorded release point.
            Vector3 from = previousMotion == ball.MotionRevision ? previousPosition : ball.MotionOrigin;
            previousPosition = current;
            previousMotion = ball.MotionRevision;
            if (ball.IsScoringShot && ball.Owner == null && ball.State != BallState.Held && lastScoredShot != ball.ShotSequence)
            foreach (GoalMouth goal in goals)
            {
                if (goal != null && goal.isActiveAndEnabled && goal.TryGetCrossing(from, current, ball.Radius, out _))
                {
                    RegisterGoal(goal.ScoringTeam);
                    break;
                }
            }
            // A completed crossing observed this step wins over the quarter/shot-clock deadline.
            if(!IsPlaying)return;
            AdvanceLiveTime(Time.fixedDeltaTime);
        }

        /// <summary>One clock path for live physics and deterministic simulation. Never runs during stoppages.</summary>
        public void AdvanceLiveTime(float seconds)
        {
            if(!IsPlaying || seconds<=0f || float.IsNaN(seconds) || float.IsInfinity(seconds))return;
            float step=Mathf.Min(seconds,clock.Remaining);
            if(shotClock!=null)step=Mathf.Min(step,shotClock.LiveSecondsRemaining);
            if(clock.Remaining<=0f || clock.Tick(step))
            {
                Phase=clock.Quarter>=clock.Quarters?MatchPhase.FullTime:clock.Quarter==2?MatchPhase.HalfTime:MatchPhase.QuarterBreak;
                StopPlay(true);return;
            }
            if(shotClock!=null)shotClock.Tick(step);
        }

        private void RegisterGoal(PrototypeTeam team)
        {
            lastScoredShot = ball.ShotSequence;
            if (team == PrototypeTeam.Blue)
                BlueScore++;
            else
                WhiteScore++;
            Phase=MatchPhase.GoalRestart;restartTeam=team==PrototypeTeam.Blue?PrototypeTeam.White:PrototypeTeam.Blue;
            restartAt = Time.unscaledTime + restartDelay;
            StopPlay(false);
        }

        private void StopPlay(bool clearBall)
        {
            if (roster != null && playerSelection != null)
            {
                playerSelection.SetInputLocked(true);
                roster.SetSimulationPaused(true);
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
            ball.SetSimulationPaused(true);
            Changed?.Invoke();
        }

        public void RequestTurnover(PrototypeTeam awardedTeam)
        {
            if(!IsPlaying)return;
            Phase=MatchPhase.TurnoverRestart;restartTeam=awardedTeam;
            restartAt=Time.unscaledTime+turnoverDelay;StopPlay(true);
        }

        public void ContinueMatch()
        {
            if(Phase==MatchPhase.PreMatch){RestartMatch();return;}
            if(Phase!=MatchPhase.QuarterBreak && Phase!=MatchPhase.HalfTime)return;
            bool half=Phase==MatchPhase.HalfTime;
            if(half)SetEnds(true);
            clock.NextQuarter();restartTeam=null;CompleteRestart(half);
        }
        public void RestartMatch()=>PrepareMatch(true);
        public void ReturnToPrototype()=>PrepareMatch(false);
        private void PrepareMatch(bool play)
        {
            Phase=MatchPhase.PreMatch;StopPlay(true);
            SetEnds(false);EndSwitchCount=0;
            BlueScore=WhiteScore=0;lastScoredShot=-1;clock.Reset();restartTeam=null;
            NewMatch?.Invoke();
            if(play)CompleteRestart();
            else {if(roster!=null)roster.ResetPositions();Changed?.Invoke();}
        }

        private void Update()
        {
            if (IsRestarting && Time.unscaledTime >= restartAt)
                CompleteRestart();
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
                    if(player.Team==restartTeam.Value){awarded=player;break;}
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

        private void OnDisable()
        {
            if (!IsPlaying && ball != null && swimmer != null && ballStart != null && swimmerStart != null)
                CompleteRestart();
        }
    }
}
