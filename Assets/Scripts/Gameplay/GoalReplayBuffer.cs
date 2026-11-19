using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo
{
    /// <summary>Bounded transform history and pooled render-only copies of the live ball and rigs.</summary>
    internal sealed class GoalReplayBuffer : IDisposable
    {
        internal struct Pose
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        internal struct Frame
        {
            public float Time;
            public int Owner, Shot;
            public BallState State;
            public BallReleaseKind Release;
            public PassDelivery Delivery;
        }

        sealed class Actor
        {
            public Transform Root;
            public Transform Copy;
            public int RootIndex;
            public SwimmerIdentity Player;
            public Transform Arm, Forearm, Head;
        }

        readonly List<Transform> sources = new();
        readonly List<Transform> copies = new();
        readonly List<Renderer> originals = new();
        readonly List<bool> hidden = new();
        readonly List<Actor> actors = new();
        readonly Dictionary<SwimmerBallInteraction,int> slots = new();
        readonly GameObject stage;
        readonly Pose[] poses;
        readonly Frame[] frames;
        readonly ushort[] appearances;
        int next, count;
        bool showing;
        public int Count => count;
        public int PoseCount => sources.Count;
        public int BufferBytes => poses.Length * 28 + frames.Length * 24 + appearances.Length * 2;
        int Index(int index) => (next - count + index + frames.Length) % frames.Length;
        public float Start => count == 0 ? 0f : frames[Index(0)].Time;
        public float End => count == 0 ? 0f : frames[Index(count-1)].Time;
        public Vector3 BallPosition => copies[actors[0].RootIndex].position;
        public Vector3 ActorHead(int slot) => actors[slot].Head != null ? actors[slot].Head.position : actors[slot].Copy.position+Vector3.up*.25f;
        public Vector3 ActorPosition(int slot) => slot > 0 ? actors[slot].Copy.position : BallPosition;

        public GoalReplayBuffer(BallController ball, PrototypeRoster roster, int capacity, Transform parent)
        {
            stage = new GameObject("Goal replay actors (render only)");
            stage.transform.SetParent(parent,true);
            stage.SetActive(false);
            AddActor(ball.transform,null);
            foreach (var player in roster.Members)
            {
                var visual=player.GetComponentInChildren<WaterVisuals.RiggedSwimmerVisual>();
                if (visual == null || visual.alignmentRoot == null) continue;
                int slot=actors.Count;
                var actor=AddActor(visual.alignmentRoot,player);
                slots.Add(player.Interaction,slot);
                foreach(var source in player.GetComponentsInChildren<Renderer>(true))
                    if(!source.transform.IsChildOf(visual.alignmentRoot))
                    {
                        originals.Add(source);
                        hidden.Add(false);
                    }
                var animator=visual.Animator;
                if (animator != null && animator.isHuman)
                {
                    actor.Head=FindCopy(animator.GetBoneTransform(HumanBodyBones.Head));
                    actor.Arm=FindCopy(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
                    actor.Forearm=FindCopy(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
                }
            }
            poses = new Pose[sources.Count * capacity];
            frames = new Frame[capacity];
            appearances = new ushort[capacity * actors.Count];
        }

        Actor AddActor(Transform root, SwimmerIdentity player)
        {
            var actor = new Actor { Root = root, Player = player, RootIndex = sources.Count };
            var map=new Dictionary<Transform,Transform>();
            actor.Copy=CopyHierarchy(root,stage.transform,map);
            actor.Copy.localScale=root.lossyScale;
            foreach (var source in root.GetComponentsInChildren<Renderer>(false))
            {
                if (source is SkinnedMeshRenderer skin)
                {
                    var destination=map[source.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
                    destination.sharedMesh=skin.sharedMesh;
                    var bones=skin.bones;
                    for (int i=0;i<bones.Length;i++)
                        if (bones[i] != null) map.TryGetValue(bones[i],out bones[i]);
                    destination.bones=bones;
                    if (skin.rootBone != null && map.TryGetValue(skin.rootBone,out var bone)) destination.rootBone=bone;
                    destination.localBounds=skin.localBounds;
                    ConfigureRenderer(source,destination);
                }
                else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter))
                {
                    var destination=map[source.transform].gameObject;
                    destination.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                    ConfigureRenderer(source,destination.AddComponent<MeshRenderer>());
                }
            }
            actors.Add(actor);
            return actor;
        }

        Transform CopyHierarchy(Transform live, Transform parent, Dictionary<Transform,Transform> map)
        {
            var copy = new GameObject(live.name).transform;
            copy.gameObject.layer=live.gameObject.layer;
            copy.SetParent(parent,false);
            copy.SetLocalPositionAndRotation(live.localPosition,live.localRotation);
            copy.localScale=live.localScale;
            map.Add(live,copy);
            sources.Add(live);
            copies.Add(copy);
            for (int i=0;i<live.childCount;i++)
            {
                var child=live.GetChild(i);
                if (child.gameObject.activeSelf) CopyHierarchy(child,copy,map);
            }
            return copy;
        }

        void ConfigureRenderer(Renderer source,Renderer destination)
        {
            destination.sharedMaterials=source.sharedMaterials;
            destination.shadowCastingMode=source.shadowCastingMode;
            destination.receiveShadows=source.receiveShadows;
            destination.enabled=source.enabled;
            originals.Add(source);
            hidden.Add(false);
        }

        Transform FindCopy(Transform source)
        {
            int index=sources.IndexOf(source);
            return index>=0 ? copies[index] : null;
        }

        public int Slot(SwimmerBallInteraction player) => player != null && slots.TryGetValue(player,out var index) ? index : -1;

        public void Capture(float time, BallController ball, bool final = false)
        {
            if (count > 0 && time <= End)
            {
                // LateUpdate can already have sampled this time. Replace that sample atomically.
                next = Index(count-1);
                count--;
            }
            int offset=next*sources.Count;
            for (int i=0;i<sources.Count;i++)
            {
                var t=sources[i];
                if (t != null) poses[offset+i]=new Pose { Position=t.localPosition, Rotation=t.localRotation };
            }
            for (int i=0;i<actors.Count;i++)
            {
                var actor=actors[i];
                int index=actor.RootIndex;
                var position=actor.Root.position;
                if (i==0 && final) position=ball.PhysicsPosition;
                poses[offset+index]=new Pose{Position=position,Rotation=actor.Root.rotation};
                appearances[next*actors.Count+i]=(ushort)(actor.Root.gameObject.activeInHierarchy ? actor.Player != null ? actor.Player.CapNumber : 1 : 0);
            }
            frames[next]=new Frame{Time=time,Owner=Slot(ball.Owner),Shot=ball.ShotSequence,State=ball.State,Release=ball.ReleaseKind,Delivery=ball.Delivery};
            next=(next+1)%frames.Length;
            count=Mathf.Min(count+1,frames.Length);
        }

        public void ShowAt(float time)
        {
            if (count == 0) return;
            if (!showing)
            {
                for (int i=0;i<originals.Count;i++)
                {
                    if(originals[i]==null)continue;
                    hidden[i]=originals[i].forceRenderingOff;
                    originals[i].forceRenderingOff=true;
                }
                stage.SetActive(true);
                showing=true;
            }
            int low=0,high=count-1;
            while (low+1<high)
            {
                int middle=(low+high)/2;
                if (frames[Index(middle)].Time<=time) low=middle; else high=middle;
            }
            int first=Index(low);
            int second=Index(high);
            float weight=Mathf.InverseLerp(frames[first].Time,frames[second].Time,time);
            int a=first*sources.Count,b=second*sources.Count;
            for (int i=0;i<sources.Count;i++)
            {
                Pose from=poses[a+i], to=poses[b+i];
                copies[i].SetLocalPositionAndRotation(Vector3.LerpUnclamped(from.Position,to.Position,weight),
                    Quaternion.SlerpUnclamped(from.Rotation,to.Rotation,weight));
            }
            for (int i=0;i<actors.Count;i++)
            {
                var actor=actors[i];
                ushort oldCap=appearances[first*actors.Count+i], newCap=appearances[second*actors.Count+i];
                actor.Copy.gameObject.SetActive((weight>=.999f?newCap:oldCap)>0);
                if (oldCap!=newCap && weight<.999f)
                {
                    Pose pose=poses[a+actor.RootIndex];
                    actor.Copy.SetPositionAndRotation(pose.Position,pose.Rotation);
                }
            }
        }

        internal int ClosestFieldSlot(PrototypeTeam team, Vector3 location)
        {
            int best=-1;
            float distance=float.PositiveInfinity;
            for(int i=1;i<actors.Count;i++)
            {
                var player=actors[i].Player;
                if (player.Team!=team || !player.IsFieldPlayer || !player.EligibleForPlay) continue;
                float candidate=(actors[i].Root.position-location).sqrMagnitude;
                if (candidate<distance) { best=i;distance=candidate; }
            }
            return best;
        }

        internal void Regret(int slot, float time)
        {
            if (slot<=0 || actors[slot].Head==null) return;
            var actor=actors[slot];
            float down=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(time*2.5f)) *
                (1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(1.65f,2.6f,time)));
            actor.Head.rotation=Quaternion.AngleAxis(27f*down,actor.Copy.right)*actor.Head.rotation;
            if (actor.Arm!=null)
                actor.Arm.rotation=Quaternion.AngleAxis(-12f*down,actor.Copy.forward)*actor.Arm.rotation;
        }

        public void Celebrate(int slot,float time)
        {
            if (slot<=0) return;
            var actor=actors[slot];
            if (actor.Arm == null || actor.Forearm == null) return;
            var lift=Vector3.Slerp(actor.Copy.right,Vector3.up,.92f);
            var arm=actor.Arm;
            var forearm=actor.Forearm;
            var current=forearm.position-arm.position;
            if (current.sqrMagnitude < .0001f) return;
            float liftWeight=Mathf.SmoothStep(0,1,Mathf.Min(1,time*2.6f));
            lift=Quaternion.AngleAxis(Mathf.Sin(time*6f)*18f,actor.Copy.forward)*lift;
            arm.rotation=Quaternion.Slerp(arm.rotation,Quaternion.FromToRotation(current,lift)*arm.rotation,liftWeight);
        }

        public void Hide()
        {
            if(!showing)return;
            showing=false;
            if (stage != null) stage.SetActive(false);
            for (int i=0;i<originals.Count;i++) if (originals[i]!=null) originals[i].forceRenderingOff=hidden[i];
        }

        public void Clear() { Hide(); next=count=0; }
        public void Dispose() { Hide(); if(stage!=null) Object.Destroy(stage); }
    }
}
