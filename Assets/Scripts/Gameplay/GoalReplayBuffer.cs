using System;
using System.Collections.Generic;
using UnityEngine;
using ToduStudios.WaterPolo.WaterVisuals;
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
            public Transform LiveHead;
            public Renderer Number;
            public int LastDisplayedCap = -1;
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
        MaterialPropertyBlock numberProperties;
        int next, count;
        bool showing;
        public int Count => count;
        public int PoseCount => sources.Count;
        public int BufferBytes => poses.Length * 28 + frames.Length * 24 + appearances.Length * 2;
        int Index(int index) => (next - count + index + frames.Length) % frames.Length;
        public float Start => count == 0 ? 0f : frames[Index(0)].Time;
        public float End => count == 0 ? 0f : frames[Index(count-1)].Time;
        public Vector3 BallPosition => copies[actors[0].RootIndex].position;
        internal SwimmerIdentity AthleteAt(int slot) => slot > 0 ? actors[slot].Player : null;
        internal Vector3 LiveHead(int slot) => actors[slot].LiveHead != null ?
            actors[slot].LiveHead.position : actors[slot].Root.position + Vector3.up * .25f;

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
                    actor.LiveHead = animator.GetBoneTransform(HumanBodyBones.Head);
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
            actor.Copy=PresentationVisualCopy.Create(root,stage.transform,map,
                (source,copy)=>{sources.Add(source);copies.Add(copy);},
                source=>{originals.Add(source);hidden.Add(false);});
            var kit=root.GetComponentInChildren<AthleteEquipment>();
            if (kit != null && kit.NumberRenderer != null && map.TryGetValue(kit.NumberRenderer.transform,out var label))
                actor.Number=label.GetComponent<Renderer>();
            actors.Add(actor);
            return actor;
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
                int cap=weight>=.999f?newCap:oldCap;
                actor.Copy.gameObject.SetActive(cap>0);
                if (cap>0 && actor.Number!=null && actor.LastDisplayedCap!=cap)
                {
                    numberProperties ??= new MaterialPropertyBlock();
                    AthleteEquipment.SetNumber(actor.Number,cap,numberProperties);
                    actor.LastDisplayedCap=cap;
                }
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
