using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo
{
    // Replace this adapter with a presentation animator when the deck animation set exists.
    internal sealed class PrematchCast : IDisposable
    {
        internal sealed class Athlete
        {
            private readonly Transform[] joints=new Transform[(int)HumanBodyBones.LastBone];
            private readonly Quaternion[] rest=new Quaternion[(int)HumanBodyBones.LastBone];
            public readonly SwimmerIdentity Identity;
            public readonly Transform Root;
            public readonly Vector3 Mark;
            public readonly Vector3 Entry;
            public readonly bool Captain;
            public readonly string DisplayName;
            public Transform Hand => Joint(HumanBodyBones.RightHand);

            public Athlete(SwimmerIdentity player, Transform parent, Transform mark, Vector3 entry, bool captain, HubCatalog catalog)
            {
                Identity=player;
                Mark=mark.position;
                Entry=entry;
                Captain=captain;
                DisplayName=catalog?.Player(player.CollectionDefinitionId)?.name;
                var visual=player.GetComponentInChildren<RiggedSwimmerVisual>();
                var bones=new Dictionary<Transform,Transform>();
                Root=PresentationVisualCopy.Create(visual.alignmentRoot,parent,bones);
                for(int i=0;i<joints.Length;i++)
                {
                    var key=visual.Animator.GetBoneTransform((HumanBodyBones)i);
                    if(key!=null && bones.TryGetValue(key,out var joint))
                    {
                        joints[i]=joint;
                        rest[i]=joint.localRotation;
                    }
                }
            }

            Transform Joint(HumanBodyBones bone)=>joints[(int)bone];
            void Point(HumanBodyBones parent,HumanBodyBones child,Vector3 direction)
            {
                var joint=Joint(parent);
                var next=Joint(child);
                if (joint==null || next==null) return;
                Vector3 current=next.position-joint.position;
                if (current.sqrMagnitude>.00001f) joint.rotation=Quaternion.FromToRotation(current,direction)*joint.rotation;
            }

            public void Pose(Vector3 feet,Vector3 facing,float time,float gait,float offer=0f)
            {
                Root.SetPositionAndRotation(feet,Quaternion.LookRotation(facing));
                foreach(var bone in PoseJoints)
                {
                    var joint=Joint(bone);
                    if(joint!=null) joint.localRotation=rest[(int)bone];
                }
                float swing=Mathf.Sin(time*8f)*gait;
                Vector3 right=Root.right, forward=Root.forward;
                Point(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,-Vector3.up-right*.10f+forward*swing*.40f);
                Point(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,-Vector3.up+right*.10f-forward*swing*.40f);
                Point(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,-Vector3.up-forward*Mathf.Max(0,swing)*.48f);
                Point(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,-Vector3.up-forward*Mathf.Max(0,-swing)*.48f);
                Point(HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,forward);
                Point(HumanBodyBones.RightFoot,HumanBodyBones.RightToes,forward);
                Point(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,-Vector3.up-right*.22f-forward*swing*.27f);
                Point(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,
                    Vector3.Lerp(-Vector3.up+right*.20f+forward*swing*.27f,forward-Vector3.up*.55f,offer));
                Point(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,-Vector3.up+forward*.08f);
                Point(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
                    Vector3.Lerp(-Vector3.up+forward*.14f,forward*.85f+Vector3.up*.28f,offer));
                var lf=Joint(HumanBodyBones.LeftFoot);
                var rf=Joint(HumanBodyBones.RightFoot);
                if (lf!=null&&rf!=null) Root.position+=Vector3.up*(feet.y+.05f-Mathf.Min(lf.position.y,rf.position.y));
                var head=Joint(HumanBodyBones.Head);
                if (head!=null) head.rotation=Quaternion.AngleAxis(Mathf.Sin(time*1.45f)*2f,Root.up)*head.rotation;
            }

            private static readonly HumanBodyBones[] PoseJoints=
            {
                HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,
                HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,
                HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.Head
            };
        }

        private readonly GameObject stage;
        private readonly Mesh pennantMesh;
        private readonly Transform[] pennants;
        private readonly Transform[] officials;
        private readonly Athlete[][] teams;
        private readonly Vector3 centre;
        public IReadOnlyList<Athlete> Blue=>teams[0];
        public IReadOnlyList<Athlete> White=>teams[1];
        public Athlete BlueCaptain=>teams[0][6];
        public Athlete WhiteCaptain=>teams[1][6];
        public int Count=>teams[0].Length+teams[1].Length;

        public static bool Ready(PrototypeRoster roster,VenueIntroductionAnchors anchors)
        {
            if (roster==null || roster.Members.Count!=14 || anchors==null || !HasMarks(anchors.blueLineup,7) || !HasMarks(anchors.whiteLineup,7) ||
                !HasMarks(anchors.blueEntranceRoute,3) || !HasMarks(anchors.whiteEntranceRoute,3)) return false;
            int blue=0, white=0;
            foreach(var p in roster.Members)
            {
                if (p==null) continue;
                var visual=p.GetComponentInChildren<RiggedSwimmerVisual>();
                if (visual==null || visual.Animator==null || !visual.Animator.isHuman || visual.alignmentRoot==null) continue;
                if(p.Team==PrototypeTeam.Blue) blue++; else white++;
            }
            return blue==7 && white==7;
        }

        private static bool HasMarks(Transform[] anchors,int count)
        {
            if (anchors==null || anchors.Length<count) return false;
            for(int i=0;i<count;i++) if (anchors[i]==null) return false;
            return true;
        }

        public PrematchCast(PrototypeRoster roster,VenueIntroductionAnchors anchors,RefereeVisual[] referees,
            Material blue,Material white)
        {
            stage=new GameObject("Prematch visual cast");
            stage.SetActive(false);
            try
            {
                centre=(anchors.blueLineup[6].position+anchors.whiteLineup[6].position)*.5f;
                var catalog=HubApplication.Instance?.Progression?.Catalog;
                teams=new Athlete[2][];
                for(int side=0;side<2;side++)
                {
                    var players=roster.Members.Where(p=>(int)p.Team==side).OrderBy(p=>p.IsFieldPlayer).ThenBy(p=>p.CapNumber).ToList();
                    var captain=players.FirstOrDefault(p=>p.IsFieldPlayer && p.FieldPosition==FieldPositionRole.CentreForward) ?? players.First(p=>p.IsFieldPlayer);
                    players.Remove(captain);
                    players.Add(captain);
                    var marks=side==0?anchors.blueLineup:anchors.whiteLineup;
                    var lane=side==0?anchors.blueEntranceRoute:anchors.whiteEntranceRoute;
                    var direction=(lane[2].position-lane[1].position).normalized;
                    teams[side]=new Athlete[7];
                    for(int i=0;i<7;i++)
                    {
                        var entry=lane[1].position-direction*(i*.77f);
                        teams[side][i]=new Athlete(players[i],stage.transform,marks[i],entry,i==6,catalog);
                    }
                }

                officials=new Transform[Mathf.Min(2,referees.Length)];
                for(int i=0;i<officials.Length;i++)
                {
                    officials[i]=PresentationVisualCopy.Create(referees[i].transform,stage.transform,new Dictionary<Transform,Transform>());
                    officials[i].SetPositionAndRotation(centre+new Vector3(i==0?-.35f:.35f,0,-.66f),Quaternion.identity);
                }
                pennantMesh=CreatePennant();
                pennants=new[]{AddPennant("Blue pennant",blue),AddPennant("White pennant",white)};
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        Transform AddPennant(string name,Material material)
        {
            var prop=new GameObject(name);
            prop.transform.SetParent(stage.transform,false);
            prop.AddComponent<MeshFilter>().sharedMesh=pennantMesh;
            prop.AddComponent<MeshRenderer>().sharedMaterial=material;
            return prop.transform;
        }

        static Mesh CreatePennant()
        {
            // Two-sided cloth silhouette and stem. Team colours come from the existing kit materials.
            var mesh=new Mesh{name="Ceremonial pennant"};
            var face=new[]{new Vector3(-.015f,-.10f,0),new Vector3(.012f,-.10f,0),new Vector3(.012f,.28f,0),
                new Vector3(-.015f,.28f,0),new Vector3(.012f,.25f,0),new Vector3(.27f,.25f,0),new Vector3(.20f,.065f,0)};
            mesh.vertices=face.Concat(face).ToArray();
            mesh.triangles=new[]{0,1,2,0,2,3,7,9,8,7,10,9,4,5,6,11,13,12};
            mesh.RecalculateNormals();
            return mesh;
        }

        internal void Show(PreMatchStage phase,float progress,float timeline)
        {
            if(stage==null)return;
            stage.SetActive(phase!=PreMatchStage.ArenaIntro && phase!=PreMatchStage.MatchStartTransition);
            if (!stage.activeSelf) return;
            float step=phase==PreMatchStage.CaptainExchange
                ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.24f,progress))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.74f,1,progress))) : 0;
            for(int side=0;side<2;side++)
            for(int i=0;i<7;i++)
            {
                var athlete=teams[side][i];
                Vector3 feet=athlete.Mark, facing=Vector3.forward;
                float gait=0, offer=0;
                if (phase==PreMatchStage.TeamWalkout)
                {
                    float p=Mathf.Clamp01(progress*1.30f-i*.047f);
                    float bend=.58f;
                    Vector3 doorway=new Vector3(athlete.Entry.x,athlete.Mark.y,athlete.Mark.z-.95f-i*.09f);
                    feet=p<bend?Vector3.Lerp(athlete.Entry,doorway,p/bend):Vector3.Lerp(doorway,athlete.Mark,(p-bend)/(1-bend));
                    var movement=p<bend?doorway-athlete.Entry:athlete.Mark-doorway;
                    movement.y=0;
                    facing=Vector3.Slerp(movement.normalized,Vector3.forward,Mathf.InverseLerp(.84f,1,p));
                    gait=p<1?.92f:0f;
                }
                if (athlete.Captain && phase==PreMatchStage.CaptainExchange)
                {
                    feet=Vector3.Lerp(feet,centre+new Vector3(side==0?-.64f:.64f,0,1.05f),step);
                    facing=Vector3.Slerp(facing,side==0?Vector3.right:Vector3.left,step*.88f);
                    gait=progress<.24f||progress>.74f?.55f*step:0f;
                    offer=step;
                }
                if (phase==PreMatchStage.TeamHuddle)
                {
                    float angle=(Mathf.PI*2*i)/7;
                    var group=centre+new Vector3(side==0?-4.0f:4.0f,0,.45f);
                    var offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    float gather=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.62f,progress));
                    float depart=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.79f,1,progress));
                    feet=Vector3.Lerp(feet,group+offset*1.10f,gather)+Vector3.forward*depart*.90f;
                    facing=Vector3.Slerp(Vector3.Slerp(Vector3.forward,-offset,gather),Vector3.forward,depart);
                    gait=(progress<.62f||progress>.79f)?.68f:0;
                }
                athlete.Pose(feet,facing,timeline+i*.11f,gait,offer);
            }

            foreach(var official in officials) official.gameObject.SetActive(phase!=PreMatchStage.TeamHuddle);
            float exchange=phase>PreMatchStage.CaptainExchange ? 1f : phase==PreMatchStage.CaptainExchange
                ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.44f,.61f,progress)) : 0f;
            bool props=phase>=PreMatchStage.CaptainExchange && phase<PreMatchStage.TeamHuddle;
            for(int team=0;team<2;team++)
            {
                var prop=pennants[team];
                prop.gameObject.SetActive(props);
                if(!props)continue;
                var sender=teams[team][6].Hand;
                var recipient=teams[1-team][6].Hand;
                if(sender==null || recipient==null)continue;
                var handoff=Vector3.Lerp(sender.position,recipient.position,exchange);
                handoff.y+=.075f+Mathf.Sin(exchange*Mathf.PI)*(team==0?.13f:0f);
                prop.SetPositionAndRotation(handoff,Quaternion.Euler(0f,team==0?16f:-16f,team==0?-8f:8f));
            }
        }

        public void Dispose()
        {
            if(stage!=null) {stage.SetActive(false);Object.Destroy(stage);}
            if(pennantMesh!=null) Object.Destroy(pennantMesh);
        }
    }
}
