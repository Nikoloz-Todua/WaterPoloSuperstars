using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public interface IRefereePresentation { void Animate(float speed,RefereeSignal signal); }
    /// <summary>Replaceable primitive animation, independent of deck movement and officiating decisions.</summary>
    [DisallowMultipleComponent]
    public sealed class RefereeVisual : MonoBehaviour, IRefereePresentation
    {
        [SerializeField] private Transform leftArm,rightArm,leftLeg,rightLeg,torso;
        private float gait;
        public void Animate(float speed,RefereeSignal signal)
        {
            float weight=Mathf.Clamp01(Mathf.Abs(speed)/2.2f);gait+=Mathf.Abs(speed)*Time.deltaTime*5f;
            float swing=Mathf.Sin(gait)*25f*weight;
            leftLeg.localRotation=Quaternion.Euler(swing,0f,0f);rightLeg.localRotation=Quaternion.Euler(-swing,0f,0f);
            Vector3 left=new Vector3(-swing*0.6f,0f,0f),right=new Vector3(swing*0.6f,0f,0f);
            if(signal==RefereeSignal.Foul || signal==RefereeSignal.Exclusion)right=new Vector3(-145f,0f,-15f);
            if(signal==RefereeSignal.Foul || signal==RefereeSignal.Exclusion || signal==RefereeSignal.Period)left=new Vector3(-115f,0f,45f);
            if(signal==RefereeSignal.Exclusion)right=new Vector3(-75f,0f,-80f);
            if(signal==RefereeSignal.Penalty){left=new Vector3(-90f,0f,0f);right=new Vector3(-90f,0f,0f);}
            if(signal==RefereeSignal.Goal)right=new Vector3(-90f,0f,-20f);
            if(signal==RefereeSignal.Period)right=new Vector3(-115f,0f,25f);
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(left),1f-Mathf.Exp(-10f*Time.deltaTime));
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(right),1f-Mathf.Exp(-10f*Time.deltaTime));
            torso.localPosition=new Vector3(0f,1.18f+Mathf.Abs(Mathf.Sin(gait))*0.025f*weight,0f);
        }
    }
}
