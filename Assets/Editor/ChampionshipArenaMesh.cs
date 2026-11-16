using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Small native authoring utility: shared material batches, no temporary primitive GameObjects.</summary>
    internal sealed class ChampionshipArenaMesh
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<int> indices=new List<int>();
        readonly List<Vector2> uv=new List<Vector2>();
        public void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Vector2? uvA=null,Vector2? uvB=null,Vector2? uvC=null,Vector2? uvD=null)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var p=b;b=d;d=p;var q=uvB;uvB=uvD;uvD=q;}
            int n=vertices.Count;var up=new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));
            Vector2 UV(Vector3 p)=>up.y>up.x&&up.y>up.z?new Vector2(p.x,p.z):up.x>up.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
            vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{uvA??UV(a),uvB??UV(b),uvC??UV(c),uvD??UV(d)});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var p=b;b=c;c=p;}int n=vertices.Count;vertices.AddRange(new[]{a,b,c});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up});indices.AddRange(new[]{n,n+1,n+2});
        }
        public void Box(Vector3 p,Vector3 size,Quaternion rotation=default)
        {
            var r=rotation==default?Quaternion.identity:rotation;var h=size*.5f;
            for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1}){Vector3 a=Vector3.zero,u=Vector3.zero,v=Vector3.zero;a[axis]=h[axis]*sign;u[(axis+1)%3]=h[(axis+1)%3];v[(axis+2)%3]=h[(axis+2)%3];Vector3 normal=Vector3.zero;normal[axis]=sign;Face(p+r*(a-u-v),p+r*(a+u-v),p+r*(a+u+v),p+r*(a-u+v),r*normal);}
        }
        public void Bevel(Vector3 p,Vector3 size,float bevel,Quaternion rotation=default)
        {
            var r=rotation==default?Quaternion.identity:rotation;var h=size*.5f;float b=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.75f);
            var ring=new[]{new Vector2(-h.x+b,-h.z),new Vector2(h.x-b,-h.z),new Vector2(h.x,-h.z+b),new Vector2(h.x,h.z-b),new Vector2(h.x-b,h.z),new Vector2(-h.x+b,h.z),new Vector2(-h.x,h.z-b),new Vector2(-h.x,-h.z+b)};
            for(int i=0;i<8;i++){var a=ring[i];var c=ring[(i+1)%8];Vector3 A=new Vector3(a.x,-h.y,a.y),B=new Vector3(c.x,-h.y,c.y);Face(p+r*A,p+r*B,p+r*(B+Vector3.up*size.y),p+r*(A+Vector3.up*size.y),r*new Vector3(a.x+c.x,0,a.y+c.y).normalized);}
            for(int i=1;i<7;i++)foreach(int sign in new[]{-1,1})Triangle(p+r*new Vector3(ring[0].x,sign*h.y,ring[0].y),p+r*new Vector3(ring[i].x,sign*h.y,ring[i].y),p+r*new Vector3(ring[i+1].x,sign*h.y,ring[i+1].y),r*Vector3.up*sign);
        }
        public void Tube(Vector3 a,Vector3 b,float radius,int sides=6)
        {
            var axis=(b-a).normalized;var u=Vector3.Cross(axis,Vector3.up).normalized;if(u.sqrMagnitude<.01f)u=Vector3.right;var v=Vector3.Cross(axis,u);
            for(int i=0;i<sides;i++){float x=i*Mathf.PI*2/sides,y=(i+1)*Mathf.PI*2/sides;var p=(u*Mathf.Cos(x)+v*Mathf.Sin(x))*radius;var q=(u*Mathf.Cos(y)+v*Mathf.Sin(y))*radius;Face(a+p,b+p,b+q,a+q,(p+q).normalized);Triangle(a,a+q,a+p,-axis);Triangle(b,b+p,b+q,axis);}
        }
        public void Ellipsoid(Vector3 p,Vector3 size,Quaternion r,int sides=8,int rings=4)
        {
            Vector3 P(int ring,int side){float a=Mathf.PI*ring/rings,b=Mathf.PI*2*side/sides;return p+r*Vector3.Scale(size*.5f,new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)));}
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){var a=P(j,i);var b=P(j,i+1);var c=P(j+1,i+1);var d=P(j+1,i);Face(a,b,c,d,((a+b+c+d)*.25f-p).normalized);}
        }
        public void Append(Mesh mesh,Matrix4x4 matrix)
        {
            int n=vertices.Count;foreach(var v in mesh.vertices)vertices.Add(matrix.MultiplyPoint3x4(v));var coords=mesh.uv;if(coords.Length==mesh.vertexCount)uv.AddRange(coords);else for(int i=0;i<mesh.vertexCount;i++)uv.Add(Vector2.zero);foreach(int t in mesh.triangles)indices.Add(t+n);
        }
        public Mesh Mesh(string name)
        {
            var m=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
        }
    }

    internal sealed class ChampionshipArenaBatch
    {
        public readonly Transform root;
        readonly Dictionary<Material,ChampionshipArenaMesh> parts=new Dictionary<Material,ChampionshipArenaMesh>();
        public ChampionshipArenaBatch(Transform root){this.root=root;}
        public ChampionshipArenaMesh Part(Material material){if(!parts.TryGetValue(material,out var p)){p=new ChampionshipArenaMesh();parts.Add(material,p);}return p;}
        public void Box(Material m,Vector3 p,Vector3 size,Quaternion r=default)=>Part(m).Box(p,size,r);
        public void Bevel(Material m,Vector3 p,Vector3 size,float bevel=.04f,Quaternion r=default)=>Part(m).Bevel(p,size,bevel,r);
        public void Tube(Material m,Vector3 a,Vector3 b,float radius,int sides=6)=>Part(m).Tube(a,b,radius,sides);
        public void Floor(Material m,Vector3 center,Vector2 size){var a=center+new Vector3(-size.x*.5f,0,-size.y*.5f);Part(m).Face(a,a+Vector3.right*size.x,a+new Vector3(size.x,0,size.y),a+Vector3.forward*size.y,Vector3.up);}
        public List<Renderer> Save(string directory,bool shadows=false,string assetPrefix=null)
        {
            var list=new List<Renderer>();foreach(var p in parts){string name=(assetPrefix??root.name)+" "+p.Key.name;var mesh=p.Value.Mesh(name);string path=directory+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);}var t=new GameObject(name).transform;t.SetParent(root,false);t.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=p.Key;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=true;GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ReflectionProbeStatic);list.Add(r);}return list;
        }
    }
}
