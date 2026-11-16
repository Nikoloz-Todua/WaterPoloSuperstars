using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Reusable cradle and sprinter cues; the visible ball is always the original BallController object.</summary>
    public sealed class SprintBallReleaseVisual : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private Transform cradle,blueCue,whiteCue;
        [SerializeField] private Transform leftJaw,rightJaw,cable;
        [SerializeField,Range(.8f,1.5f)] private float retractionDuration=1.05f;
        private float releasedAt;
        private Vector3 rest,leftRest,rightRest;
        public float RetractionDuration=>retractionDuration;
        private void Awake(){rest=cradle.localPosition;if(leftJaw!=null)leftRest=leftJaw.localPosition;if(rightJaw!=null)rightRest=rightJaw.localPosition;}
        private void OnEnable(){match.SprintStarted+=Released;match.Changed+=Prepared;}
        private void OnDisable(){if(match!=null){match.SprintStarted-=Released;match.Changed-=Prepared;}if(cradle!=null)cradle.gameObject.SetActive(false);if(blueCue!=null)blueCue.gameObject.SetActive(false);if(whiteCue!=null)whiteCue.gameObject.SetActive(false);}
        private void Prepared()
        {
            if(match.Phase!=MatchPhase.SprintSetup)return;
            cradle.gameObject.SetActive(true);cradle.localPosition=rest;
            if(leftJaw!=null)leftJaw.localPosition=leftRest;if(rightJaw!=null)rightJaw.localPosition=rightRest;
            if(cable!=null)cable.localRotation=Quaternion.identity;Cue(blueCue,PrototypeTeam.Blue);Cue(whiteCue,PrototypeTeam.White);
        }
        private void Released(){releasedAt=match.PresentationTime;}
        private void LateUpdate()
        {
            if(match.IsPaused)return;
            bool setup=match.Phase==MatchPhase.SprintSetup;
            float elapsed=match.PresentationTime-releasedAt;
            bool visible=setup||match.IsSprintRace&&elapsed<retractionDuration;
            if(cradle.gameObject.activeSelf!=visible)cradle.gameObject.SetActive(visible);
            if(visible)
            {
                // The jaws open at the whistle. The independent real ball drops immediately;
                // the slower cable/cradle recovery remains readable without gating play.
                float opened=setup?0f:Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.10f));
                float lowered=setup?0f:Mathf.SmoothStep(0f,1.5f,Mathf.Clamp01((elapsed-.08f)/(retractionDuration-.08f)));
                cradle.localPosition=rest-Vector3.up*lowered;
                if(leftJaw!=null)leftJaw.localPosition=leftRest-Vector3.right*(opened*.16f);
                if(rightJaw!=null)rightJaw.localPosition=rightRest+Vector3.right*(opened*.16f);
                if(cable!=null)cable.localRotation=Quaternion.Euler(0f,0f,setup?0f:Mathf.Sin(elapsed*9f)*2f*Mathf.Exp(-elapsed*3f));
            }
            Cue(blueCue,PrototypeTeam.Blue);Cue(whiteCue,PrototypeTeam.White);
        }
        private void Cue(Transform cue,PrototypeTeam team)
        {
            bool active=match.IsPeriodSprint&&match.SprintOff.Sprinter(team)!=null;
            if(cue.gameObject.activeSelf!=active)cue.gameObject.SetActive(active);
            if(active){Vector3 p=match.SprintOff.Sprinter(team).Motor.Position;p.y+=.025f;cue.position=p;}
        }
    }
}
