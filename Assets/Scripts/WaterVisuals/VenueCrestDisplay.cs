using UnityEngine;
using TMPro;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Presentation-only future team-introduction input; no match events or cinematic director.</summary>
    public sealed class VenueCrestDisplay : MonoBehaviour
    {
        public Renderer image;
        public TMP_Text fallback,teamCaption;
        public Sprite blueCrest,whiteCrest;
        public PrototypeTeam team;
        public Vector2 frameSize=new Vector2(3.8f,3.8f);
        MaterialPropertyBlock block;
        static readonly int Map=Shader.PropertyToID("_BaseMap"),ST=Shader.PropertyToID("_BaseMap_ST");
        void Start()=>Present(team,team==PrototypeTeam.Blue?blueCrest:whiteCrest);
        public void Present(PrototypeTeam value,Sprite crest)
        {
            team=value;if(crest==null){SetTexture(null,Vector4.zero,1);return;}
            Rect r=crest.textureRect;SetTexture(crest.texture,new Vector4(r.width/crest.texture.width,r.height/crest.texture.height,r.x/crest.texture.width,r.y/crest.texture.height),r.width/r.height);
        }
        public void PresentTexture(PrototypeTeam value,Texture crest)
        {team=value;SetTexture(crest,new Vector4(1,1,0,0),crest!=null?(float)crest.width/crest.height:1);}
        void SetTexture(Texture texture,Vector4 uv,float aspect)
        {
            if(teamCaption!=null)teamCaption.text=team==PrototypeTeam.Blue?"BLUE TEAM  /  INTRODUCTION":"WHITE TEAM  /  INTRODUCTION";
            if(fallback!=null)fallback.gameObject.SetActive(texture==null);if(image==null)return;image.enabled=texture!=null;if(texture==null)return;
            if(block==null)block=new MaterialPropertyBlock();block.Clear();block.SetTexture(Map,texture);block.SetVector(ST,uv);image.SetPropertyBlock(block);
            float width=Mathf.Min(frameSize.x,frameSize.y*aspect);image.transform.localScale=new Vector3(width,width/Mathf.Max(.01f,aspect),1);
        }
    }
}
