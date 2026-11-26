using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo
{
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
            public bool UsesDeckWalk => deckWalk != null;
            readonly Animator deckAnimator;
            readonly AnimationClip deckWalk;
            readonly (Transform Joint, Vector3 Position, Quaternion Rotation)[] deckRest;
            readonly DeckFootPlacement footPlacement;
            PlayableGraph walkGraph;
            AnimationClipPlayable walkPlayable;
            Vector3 previousFeet;
            float previousTime, routeDistance;

            public Athlete(SwimmerIdentity player, Transform parent, Transform mark, Vector3 entry, bool captain,
                HubCatalog catalog, bool refinedWalking)
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
                if (visual.deckWalk != null && visual.deckWalk.humanMotion &&
                    bones.TryGetValue(visual.transform, out var modelRoot))
                {
                    deckWalk = visual.deckWalk;
                    deckRest = modelRoot.GetComponentsInChildren<Transform>(true)
                        .Select(t => (t, t.localPosition, t.localRotation)).ToArray();
                    deckAnimator = modelRoot.gameObject.AddComponent<Animator>();
                    deckAnimator.avatar = visual.Animator.avatar;
                    deckAnimator.applyRootMotion = false;
                    deckAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    if (refinedWalking) footPlacement = new DeckFootPlacement(Joint, modelRoot, .065f, .016f);
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

            public void Pose(Vector3 feet,Vector3 facing,float time,float gait,float offer,bool useWalk)
            {
                Root.SetPositionAndRotation(feet,Quaternion.LookRotation(facing));
                Vector3 travel = Vector3.ProjectOnPlane(feet - previousFeet, Vector3.up);
                float elapsed = time - previousTime;
                if (elapsed <= 0f || elapsed > .25f || travel.sqrMagnitude > .25f)
                    travel = Vector3.zero;
                float movement = gait > .01f ? Mathf.InverseLerp(.03f, .55f, travel.magnitude / Mathf.Max(.001f, elapsed)) : 0f;
                if (gait > .01f) routeDistance += travel.magnitude;
                else routeDistance = 0f;
                previousFeet = feet;
                previousTime = time;
                if (deckAnimator != null && useWalk && gait > .01f)
                {
                    Walk(feet, travel, movement);
                    return;
                }
                if (walkGraph.IsValid())
                {
                    Dispose();
                    foreach (var bone in deckRest)
                        bone.Joint.SetLocalPositionAndRotation(bone.Position, bone.Rotation);
                }
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
                if (footPlacement == null)
                {
                    Point(HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,forward);
                    Point(HumanBodyBones.RightFoot,HumanBodyBones.RightToes,forward);
                }
                Point(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,-Vector3.up-right*.22f-forward*swing*.27f);
                Point(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,
                    Vector3.Lerp(-Vector3.up+right*.20f+forward*swing*.27f,forward-Vector3.up*.55f,offer));
                Point(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,-Vector3.up+forward*.08f);
                Point(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
                    Vector3.Lerp(-Vector3.up+forward*.14f,forward*.85f+Vector3.up*.28f,offer));
                var lf=Joint(HumanBodyBones.LeftFoot);
                var rf=Joint(HumanBodyBones.RightFoot);
                if (footPlacement != null)
                    footPlacement.Apply(Root, feet, travel, routeDistance / 1.36f, 1.36f, movement);
                else if (lf!=null&&rf!=null) Root.position+=Vector3.up*(feet.y+.05f-Mathf.Min(lf.position.y,rf.position.y));
                var head=Joint(HumanBodyBones.Head);
                if (head!=null) head.rotation=Quaternion.AngleAxis(Mathf.Sin(time*1.45f)*2f,Root.up)*head.rotation;
            }

            void Walk(Vector3 feet, Vector3 travel, float movement)
            {
                if (!walkGraph.IsValid())
                {
                    walkGraph = PlayableGraph.Create("Ceremony athlete walk");
                    walkGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    walkPlayable = AnimationClipPlayable.Create(walkGraph, deckWalk);
                    walkPlayable.SetApplyFootIK(footPlacement == null);
                    AnimationPlayableOutput.Create(walkGraph, "Deck athlete", deckAnimator).SetSourcePlayable(walkPlayable);
                    walkGraph.Play();
                }

                // Match the in-place stride to distance along the existing deck route.
                const float cycleDistance = 1.36f;
                walkPlayable.SetTime(Mathf.Repeat(routeDistance / cycleDistance, 1f) * deckWalk.length);
                walkGraph.Evaluate(0f);
                if (footPlacement != null)
                {
                    footPlacement.Apply(Root, feet, travel, routeDistance / cycleDistance, cycleDistance, movement);
                    return;
                }
                float floor = Mathf.Min(EstimateSole(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes),
                    EstimateSole(HumanBodyBones.RightFoot, HumanBodyBones.RightToes));
                if (!float.IsPositiveInfinity(floor)) Root.position += Vector3.up * Mathf.Clamp(feet.y - floor, -.13f, .13f);
            }

            float EstimateSole(HumanBodyBones ankleBone, HumanBodyBones toeBone)
            {
                var ankle = Joint(ankleBone);
                var toe = Joint(toeBone);
                if (ankle == null || toe == null) return float.PositiveInfinity;
                return Mathf.Min(ankle.position.y - .065f, toe.position.y - .016f);
            }

            public void Dispose()
            {
                if (walkGraph.IsValid()) walkGraph.Destroy();
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
        private readonly bool refinedWalking;
        private readonly Mesh pennantMesh;
        private readonly Transform[] pennants;
        private readonly Transform[] officials;
        private readonly Athlete[][] teams;
        private readonly Dictionary<Material, Material> castMaterials = new();
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
            Material blue,Material white,bool useRefinedWalking)
        {
            refinedWalking = useRefinedWalking;
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
                        teams[side][i]=new Athlete(players[i],stage.transform,marks[i],entry,i==6,catalog,refinedWalking);
                    }
                }
                if (teams.Any(team => team.Any(athlete => athlete.UsesDeckWalk)))
                    SetCastExposure();

                officials=new Transform[Mathf.Min(2,referees.Length)];
                for(int i=0;i<officials.Length;i++)
                {
                    referees[i].StandForCeremony();
                    officials[i]=PresentationVisualCopy.Create(referees[i].transform,stage.transform,new Dictionary<Transform,Transform>());
                    officials[i].SetPositionAndRotation(centre+new Vector3(i==0?-.35f:.35f,0,-.66f),Quaternion.identity);
                    if (referees[i].UsesProfessional) SetCastExposure(officials[i]);
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

        void SetCastExposure()
        {
            foreach (var team in teams)
                foreach (var athlete in team)
                    SetCastExposure(athlete.Root);
        }

        void SetCastExposure(Transform root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty("_EmissionColor") ||
                        material.IsKeywordEnabled("_ALPHATEST_ON")) continue;
                    if (!castMaterials.TryGetValue(material, out var exposure))
                    {
                        // Share stage-only exposure copies across the cast.
                        exposure = new Material(material) { name = material.name + " (ceremony)" };
                        exposure.EnableKeyword("_EMISSION");
                        exposure.SetTexture("_EmissionMap", material.GetTexture("_BaseMap"));
                        exposure.SetColor("_EmissionColor", material.GetColor("_BaseColor") * .37f);
                        if (material.HasProperty("_EnvironmentReflections"))
                        {
                            exposure.SetFloat("_EnvironmentReflections", 0f);
                            exposure.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                        }
                        castMaterials.Add(material, exposure);
                    }
                    materials[i] = exposure;
                }
                renderer.sharedMaterials = materials;
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
                    if (refinedWalking)
                    {
                        const float brakeAt = .84f;
                        float t = Mathf.InverseLerp(brakeAt, 1f, p);
                        float route = p < brakeAt ? p : Mathf.Lerp(brakeAt, 1f, t + t * t - t * t * t);
                        feet = WalkoutPoint(athlete.Entry, doorway, athlete.Mark, route);
                        movement = WalkoutPoint(athlete.Entry, doorway, athlete.Mark, Mathf.Min(1f, route + .006f)) -
                            WalkoutPoint(athlete.Entry, doorway, athlete.Mark, Mathf.Max(0f, route - .006f));
                    }
                    movement.y=0;
                    facing=Vector3.Slerp(movement.normalized,Vector3.forward,Mathf.InverseLerp(refinedWalking ? .94f : .84f,1,p));
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
                athlete.Pose(feet,facing,timeline+i*.11f,gait,offer,phase==PreMatchStage.TeamWalkout);
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

        private static Vector3 WalkoutPoint(Vector3 entry, Vector3 corner, Vector3 mark, float route)
        {
            const float bend = .58f, before = bend - .06f, after = bend + .06f;
            if (route < before) return Vector3.Lerp(entry, corner, route / bend);
            if (route > after) return Vector3.Lerp(corner, mark, (route - bend) / (1f - bend));
            Vector3 a = Vector3.Lerp(entry, corner, before / bend);
            Vector3 b = Vector3.Lerp(corner, mark, (after - bend) / (1f - bend));
            float t = Mathf.InverseLerp(before, after, route);
            return Vector3.Lerp(Vector3.Lerp(a, corner, t), Vector3.Lerp(corner, b, t), t);
        }

        public void Dispose()
        {
            if (teams != null)
                foreach (var team in teams)
                    if (team != null)
                        foreach (var athlete in team) athlete?.Dispose();
            if(stage!=null) {stage.SetActive(false);Object.Destroy(stage);}
            if(pennantMesh!=null) Object.Destroy(pennantMesh);
            foreach (var material in castMaterials.Values) Object.Destroy(material);
            castMaterials.Clear();
        }
    }
}
