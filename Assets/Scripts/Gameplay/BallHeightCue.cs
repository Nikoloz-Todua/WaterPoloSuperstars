using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>One pooled ground marker makes actual airborne height readable without a shader or particles.</summary>
    [DisallowMultipleComponent]
    public sealed class BallHeightCue : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private Transform groundMarker;
        private void LateUpdate()
        {
            bool visible=ball.IsAirborne;
            if(groundMarker.gameObject.activeSelf!=visible)groundMarker.gameObject.SetActive(visible);
            if(!visible)return;
            Vector3 point=ball.transform.position;float height=point.y-ball.SurfaceHeight;point.y=ball.SurfaceHeight-0.09f;groundMarker.position=point;
            float size=0.3f+Mathf.Clamp(height,0f,4f)*0.1f;groundMarker.localScale=new Vector3(size,0.003f,size);
        }
        private void OnDisable(){if(groundMarker!=null)groundMarker.gameObject.SetActive(false);}
    }
}
