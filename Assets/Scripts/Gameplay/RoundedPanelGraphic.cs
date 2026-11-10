using UnityEngine;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo
{
    /// <summary>Small reusable uGUI mesh; no textures, extra materials or runtime asset generation.</summary>
    [AddComponentMenu("UI/Water Polo Rounded Panel"),RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedPanelGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float radius = 18f;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r=rectTransform.rect;
            float arc=Mathf.Min(radius,Mathf.Min(r.width,r.height)*0.5f);
            mesh.AddVert(r.center,color,Vector2.zero);
            const int steps=6, vertices=4*(steps+1);
            for(int corner=0;corner<4;corner++)
            {
                Vector2 center=new Vector2(corner==0 || corner==3?r.xMax-arc:r.xMin+arc,corner<2?r.yMax-arc:r.yMin+arc);
                for(int i=0;i<=steps;i++)
                {
                    float angle=(corner*90f+i*90f/steps)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*arc,color,Vector2.zero);
                }
            }
            for(int i=0;i<vertices;i++)mesh.AddTriangle(0,i+1,(i+1)%vertices+1);
        }
    }
}
