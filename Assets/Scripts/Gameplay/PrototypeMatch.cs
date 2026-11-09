using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>One prototype score authority and restart coordinator; no match clock or teams of actors.</summary>
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

        private Rigidbody ballBody;
        private SwimmerInput movementInput;
        private BallInteractionInput interactionInput;
        private Vector3 previousPosition;
        private int previousMotion, lastScoredShot = -1;
        private bool restoreMotor, restoreMovementInput, restoreInteractionInput;
        private float restartAt;

        public int BlueScore { get; private set; }
        public int WhiteScore { get; private set; }
        public bool IsRestarting { get; private set; }
        public float RestartDelay => restartDelay;
        public event Action Changed;

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
            if (IsRestarting)
                return;
            Vector3 current = ballBody.position;
            // Release spawn changes are not travel. Start the first segment at the recorded release point.
            Vector3 from = previousMotion == ball.MotionRevision ? previousPosition : ball.MotionOrigin;
            previousPosition = current;
            previousMotion = ball.MotionRevision;
            if (!ball.IsScoringShot || ball.Owner != null || ball.State == BallState.Held || lastScoredShot == ball.ShotSequence)
                return;
            foreach (GoalMouth goal in goals)
            {
                if (goal != null && goal.isActiveAndEnabled && goal.TryGetCrossing(from, current, ball.Radius, out _))
                {
                    RegisterGoal(goal.ScoringTeam);
                    break;
                }
            }
        }

        private void RegisterGoal(PrototypeTeam team)
        {
            lastScoredShot = ball.ShotSequence;
            if (team == PrototypeTeam.Blue)
                BlueScore++;
            else
                WhiteScore++;
            IsRestarting = true;
            restartAt = Time.unscaledTime + restartDelay;
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
            ball.SetSimulationPaused(true);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (IsRestarting && Time.unscaledTime >= restartAt)
                CompleteRestart();
        }

        private void CompleteRestart()
        {
            if (roster != null) roster.ResetPositions();
            else swimmer.ResetPose(swimmerStart.position, swimmerStart.rotation);
            ball.ResetForRestart(ballStart.position);
            previousPosition = ballBody.position;
            previousMotion = ball.MotionRevision;
            if (roster != null && playerSelection != null)
            {
                roster.SetSimulationPaused(false);
                playerSelection.SetInputLocked(false);
            }
            else
            {
                swimmer.enabled = restoreMotor;
                if (movementInput != null) movementInput.enabled = restoreMovementInput;
                if (interactionInput != null) interactionInput.enabled = restoreInteractionInput;
            }
            IsRestarting = false;
            if (gameplayCamera != null) gameplayCamera.SnapToTarget();
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            if (IsRestarting && ball != null && swimmer != null && ballStart != null && swimmerStart != null)
                CompleteRestart();
        }
    }
}
