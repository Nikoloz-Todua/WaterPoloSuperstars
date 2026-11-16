using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Only the two decorative M18 light galleries may be cut away when blocking the ball/actor.</summary>
    [DefaultExecutionOrder(400),DisallowMultipleComponent]
    public sealed class BroadcastSightlines : MonoBehaviour
    {
        [SerializeField] private Camera output;
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private Transform primaryDisplay;
        [SerializeField] private Renderer[] westGallery,eastGallery;
        private Bounds west,east;
        private bool[] westOriginal,eastOriginal;
        private float westUntil,eastUntil;
        public int HiddenGroups {get;private set;}
        private Bounds Cache(Renderer[] list,out bool[] original){original=new bool[list.Length];var bounds=new Bounds();for(int i=0;i<list.Length;i++){original[i]=list[i].forceRenderingOff;if(i==0)bounds=list[i].bounds;else bounds.Encapsulate(list[i].bounds);}return bounds;}
        private void Awake(){west=Cache(westGallery,out westOriginal);east=Cache(eastGallery,out eastOriginal);}
        private bool Blocks(Bounds bounds,Vector3 target)
        {
            if(bounds.Contains(output.transform.position))return true;
            Vector3 v=output.WorldToViewportPoint(target);if(v.z<=0||v.x<0||v.x>1||v.y<0||v.y>1)return false;
            Vector3 delta=target-output.transform.position;return bounds.IntersectRay(new Ray(output.transform.position,delta.normalized),out float distance)&&distance>0&&distance<delta.magnitude-.5f;
        }
        private void Set(Renderer[] list,bool[] original,bool hidden){for(int i=0;i<list.Length;i++)if(list[i]!=null)list[i].forceRenderingOff=original[i]||hidden;}
        private void LateUpdate()
        {
            Vector3 actor=selection.ActivePlayer!=null?selection.ActivePlayer.Motor.Position+Vector3.up*.45f:ball.PhysicsPosition;
            if(Blocks(west,actor)||Blocks(west,ball.PhysicsPosition)||primaryDisplay!=null&&Blocks(west,primaryDisplay.position))westUntil=Time.unscaledTime+.2f;
            if(Blocks(east,actor)||Blocks(east,ball.PhysicsPosition)||primaryDisplay!=null&&Blocks(east,primaryDisplay.position))eastUntil=Time.unscaledTime+.2f;
            bool w=Time.unscaledTime<westUntil,e=Time.unscaledTime<eastUntil;Set(westGallery,westOriginal,w);Set(eastGallery,eastOriginal,e);HiddenGroups=(w?1:0)+(e?1:0);
        }
        private void OnDisable(){if(westOriginal!=null)Set(westGallery,westOriginal,false);if(eastOriginal!=null)Set(eastGallery,eastOriginal,false);HiddenGroups=0;}
    }
}
