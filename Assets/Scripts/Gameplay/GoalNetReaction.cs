using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Small visual-only deformation of existing net strands. BallController contains the scored ball.</summary>
    [DisallowMultipleComponent]
    public sealed class GoalNetReaction : MonoBehaviour
    {
        [SerializeField] private GoalMouth goal;
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private Transform[] strands=new Transform[0];
        [SerializeField,Range(0f,0.4f)] private float maximumDeformation=0.24f;
        private Vector3[] rest;
        private float elapsed=10f,amplitude;
        public bool Reacting=>elapsed<1.2f;
        private void Awake(){rest=new Vector3[strands.Length];for(int i=0;i<rest.Length;i++)rest[i]=strands[i].localPosition;}
        private void OnEnable(){ball.Netted+=Goal;match.Changed+=PhaseChanged;}
        private void OnDisable(){ball.Netted-=Goal;match.Changed-=PhaseChanged;ResetNet();}
        private void Goal(GoalMouth mouth,Vector3 point,float speed)
        {if(mouth!=goal)return;elapsed=0f;amplitude=maximumDeformation*Mathf.Clamp01(speed/20f);}
        private void PhaseChanged(){if(match.Phase!=MatchPhase.GoalRestart)ResetNet();}
        private void ResetNet(){elapsed=10f;if(rest!=null)for(int i=0;i<rest.Length;i++)if(strands[i]!=null)strands[i].localPosition=rest[i];}
        private void Update()
        {
            if(!Reacting)return;elapsed+=Time.deltaTime;
            float displacement=Mathf.Sin(elapsed*15f)*Mathf.Exp(-elapsed*4f)*amplitude;
            for(int i=0;i<rest.Length;i++)strands[i].localPosition=rest[i]+Vector3.forward*displacement;
            if(!Reacting)ResetNet();
        }
    }
}
