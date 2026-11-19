using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Copies a visible model without its Animator, scripts, colliders or gameplay state.</summary>
    internal static class PresentationVisualCopy
    {
        internal static Transform Create(Transform source, Transform parent, Dictionary<Transform,Transform> map,
            Action<Transform,Transform> register = null, Action<Renderer> observe = null)
        {
            var root=CopyHierarchy(source,parent,map,register);
            root.localScale=source.lossyScale;
            foreach (var live in source.GetComponentsInChildren<Renderer>(false))
            {
                if (!map.TryGetValue(live.transform,out var node)) continue;
                Renderer copy;
                if (live is SkinnedMeshRenderer skin)
                {
                    var mesh=node.gameObject.AddComponent<SkinnedMeshRenderer>();
                    mesh.sharedMesh=skin.sharedMesh;
                    var bones=skin.bones;
                    for (int i=0;i<bones.Length;i++)
                        if (bones[i]!=null) map.TryGetValue(bones[i],out bones[i]);
                    mesh.bones=bones;
                    if (skin.rootBone!=null && map.TryGetValue(skin.rootBone,out var bone)) mesh.rootBone=bone;
                    mesh.localBounds=skin.localBounds;
                    copy=mesh;
                }
                else if (live is MeshRenderer && live.TryGetComponent<MeshFilter>(out var filter))
                {
                    node.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                    copy=node.gameObject.AddComponent<MeshRenderer>();
                }
                else continue;

                copy.sharedMaterials=live.sharedMaterials;
                copy.shadowCastingMode=live.shadowCastingMode;
                copy.receiveShadows=live.receiveShadows;
                copy.enabled=live.enabled;
                observe?.Invoke(live);
            }
            return root;
        }

        private static Transform CopyHierarchy(Transform live, Transform parent, Dictionary<Transform,Transform> map,
            Action<Transform,Transform> register)
        {
            var copy=new GameObject(live.name).transform;
            copy.gameObject.layer=live.gameObject.layer;
            copy.SetParent(parent,false);
            copy.SetLocalPositionAndRotation(live.localPosition,live.localRotation);
            copy.localScale=live.localScale;
            map.Add(live,copy);
            register?.Invoke(live,copy);
            for(int i=0;i<live.childCount;i++)
            {
                var child=live.GetChild(i);
                if(child.gameObject.activeSelf) CopyHierarchy(child,copy,map,register);
            }
            return copy;
        }
    }
}
