using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum WaterEffectsQuality { Off, Low, Standard, High }
    /// <summary>Bounded cosmetic pool. No effect colliders, ownership changes, timers or scoring decisions.</summary>
    [DefaultExecutionOrder(330), DisallowMultipleComponent]
    public sealed class WaterInteractionEffects : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private Material material;
        [SerializeField] private TrailRenderer ballTrail;
        [SerializeField] private WaterEffectsQuality quality=WaterEffectsQuality.Standard;
        [SerializeField] private Material interactiveWaterMaterial;
        [SerializeField] private bool useContactSampling;
        [SerializeField, Range(8, WaterContactMesh.MaximumSurfaces)] private int surfaceBudget = 96;
        [SerializeField, Range(8, WaterContactMesh.MaximumParticles)] private int dropletBudget = 144;
        [SerializeField, Range(25f, 100f)] private float contactViewRange = 66f;
        private WaterContactPresentation contacts;
        public bool UsesContactSampling => useContactSampling && interactiveWaterMaterial != null;
        public int LeftHandEntries => contacts?.LeftEntries ?? 0;
        public int RightHandEntries => contacts?.RightEntries ?? 0;
        public int WaterImpacts => contacts?.ImpactCount ?? 0;
        public int VisibleBallImpacts => contacts?.ImpactSurfaceCount ?? 0;
        public int WakeEmissions => contacts?.WakeCount ?? 0;
        public int TreadEmissions => contacts?.TreadCount ?? 0;
        public int RecordedContacts => contacts?.HistoryCount ?? 0;
        public bool PlayingRecordedContacts => contacts?.IsPlayback ?? false;
        public Vector3 LastWaterImpactPoint => contacts?.LatestImpact.Point ?? Vector3.zero;
        public float LastWaterImpactStrength => contacts?.LatestImpact.Strength ?? 0f;
        public Vector3 LastHandEntryPoint => contacts?.LatestEntry.Point ?? Vector3.zero;
        private const int Capacity=48, BallCapacity=16;
        private sealed class Ripple
        { public Transform root,splash; public MeshRenderer ringRenderer,splashRenderer; public float age=10f,size,strength; }
        private Ripple[] ripples;
        private LineRenderer[] wakes;
        private SwimmerStamina[] wakeStamina;
        private Mesh ringMesh,splashMesh;
        private MaterialPropertyBlock colors;
        private int cursor;
        private int athleteCursor;
        private ParticleSystem bubbles;
        private Camera view;
        public int AthleteEmissions {get;private set;}
        public int BubbleEmissions {get;private set;}
        public int BubbleCount=>contacts?.ParticleCount ?? (bubbles!=null?bubbles.particleCount:0);
        private WaterEffectsQuality appliedQuality;
        private static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        public int ActiveEffects {get;private set;}
        public int PoolCapacity=>contacts!=null?surfaceBudget:Capacity;
        public WaterEffectsQuality Quality {get=>quality;set{if(quality==value)return;quality=appliedQuality=value;Clear();}}
        private void Awake()
        {
            if (UsesContactSampling && ball != null && match != null && roster != null && pool != null)
            {
                contacts = new WaterContactPresentation(transform, ball, match, roster, pool, interactiveWaterMaterial);
                appliedQuality = quality;
                DisableTrail();
                if (ball.TryGetComponent<BallHeightCue>(out var heightCue)) heightCue.UseSubtleWaterMarker(material);
                return;
            }
            appliedQuality=quality;colors=new MaterialPropertyBlock();ringMesh=Ring();splashMesh=Splash();ripples=new Ripple[Capacity];
            for(int i=0;i<Capacity;i++)
            {
                var root=new GameObject("PooledRipple"+i).transform;root.SetParent(transform,false);
                var ring=root.gameObject.AddComponent<MeshFilter>();ring.sharedMesh=ringMesh;
                var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var splash=new GameObject("Splash",typeof(MeshFilter),typeof(MeshRenderer)).transform;splash.SetParent(root,false);
                splash.GetComponent<MeshFilter>().sharedMesh=splashMesh;splash.GetComponent<MeshRenderer>().sharedMaterial=material;
                splash.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                ripples[i]=new Ripple {root=root,splash=splash,ringRenderer=renderer,splashRenderer=splash.GetComponent<MeshRenderer>()};root.gameObject.SetActive(false);
            }
            view=Camera.main;
            var bubbleObject=new GameObject("Pooled limb microbubbles");bubbleObject.transform.SetParent(transform,false);
            bubbles=bubbleObject.AddComponent<ParticleSystem>();bubbles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=bubbles.main;main.loop=true;main.playOnAwake=false;main.maxParticles=128;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=.48f;main.startSize=.025f;main.startSpeed=0;main.startColor=new Color(.78f,.96f,1,.35f);main.gravityModifier=-.025f;
            var emission=bubbles.emission;emission.enabled=false;var shape=bubbles.shape;shape.enabled=false;
            var fade=bubbles.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.4f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            var bubbleRenderer=bubbles.GetComponent<ParticleSystemRenderer>();bubbleRenderer.sharedMaterial=material;bubbleRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            wakes=new LineRenderer[roster!=null?roster.Members.Count:0];
            wakeStamina=new SwimmerStamina[wakes.Length];
            for(int i=0;i<wakes.Length;i++)
            {
                var go=new GameObject("SwimmerWake"+i);go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=3;line.useWorldSpace=true;
                line.widthMultiplier=0.08f;line.numCapVertices=1;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.enabled=false;wakes[i]=line;
                wakeStamina[i]=roster.Members[i].GetComponent<SwimmerStamina>();
            }
        }
        private void OnEnable()
        {
            if(ball!=null){ball.Contact+=Impact;ball.Released+=Release;}
            if(match!=null){match.Changed+=Phase;match.NewMatch+=Clear;}
        }
        private void OnDisable()
        {
            if(ball!=null){ball.Contact-=Impact;ball.Released-=Release;}
            if(match!=null){match.Changed-=Phase;match.NewMatch-=Clear;}
            Clear();
        }
        private void Release(SwimmerBallInteraction owner, BallReleaseKind kind)
        {
            if(quality!=WaterEffectsQuality.Off) contacts?.Release(kind);
        }
        private void Phase()
        {
            if(contacts!=null) contacts.PhaseChanged();
            else if(match.Phase!=MatchPhase.GoalRestart) Clear();
        }
        private void DisableTrail()
        {
            if(ballTrail==null)return;
            ballTrail.emitting=false;
            ballTrail.Clear();
        }
        private void Clear()
        {
            contacts?.Clear();
            if(ripples!=null)foreach(var r in ripples){r.age=10f;r.root.gameObject.SetActive(false);}
            if(wakes!=null)foreach(var w in wakes)w.enabled=false;
            DisableTrail();ActiveEffects=0;
            if(bubbles!=null)bubbles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void Impact(BallImpactEvent e)
        {
            if (contacts != null)
            {
                if (quality != WaterEffectsQuality.Off) contacts.Contact(e);
                return;
            }
            if(quality==WaterEffectsQuality.Off || ripples==null)return;
            int limit=quality==WaterEffectsQuality.Low?8:BallCapacity;var r=ripples[cursor++%limit];
            Vector3 point=e.Position;point.y=pool.Waterline+0.018f;
            r.root.position=point;r.root.rotation=Quaternion.identity;r.age=0f;r.strength=Mathf.Max(0.12f,e.Intensity);
            r.size=e.Kind==BallImpactKind.Skip?1.2f:e.Kind==BallImpactKind.Save?0.8f:0.65f;
            r.splash.gameObject.SetActive(e.Kind==BallImpactKind.Water || e.Kind==BallImpactKind.Skip || e.Kind==BallImpactKind.Save || e.Kind==BallImpactKind.Frame);
            r.root.gameObject.SetActive(true);
        }
        bool DetailVisible(Vector3 point)
        {
            if(quality==WaterEffectsQuality.Off||ripples==null||(match!=null&&match.IsPaused))return false;
            if(view==null)view=Camera.main;if(view==null)return true;
            if((point-view.transform.position).sqrMagnitude>(quality==WaterEffectsQuality.Low?45*45:65*65))return false;
            Vector3 v=view.WorldToViewportPoint(point);return v.z>0&&v.x>-.1f&&v.x<1.1f&&v.y>-.1f&&v.y<1.1f;
        }
        public void EmitAthleteSurface(Vector3 point,float strength,bool splash)
        {
            if(contacts!=null)
            {
                if(quality!=WaterEffectsQuality.Off)contacts.ExternalSurface(point,Mathf.Clamp01(strength),splash);
                return;
            }
            if(!DetailVisible(point))return;
            int limit=quality==WaterEffectsQuality.Low?12:Capacity-BallCapacity;
            var r=ripples[BallCapacity+athleteCursor++%limit];point.y=(pool!=null?pool.Waterline:0)+.022f;
            r.root.position=point;r.root.rotation=Quaternion.identity;r.age=0;r.strength=strength;r.size=splash?.52f:.6f;
            r.splash.gameObject.SetActive(splash);r.root.gameObject.SetActive(true);AthleteEmissions++;
        }
        public void EmitAthleteBubbles(Vector3 point,float speed)
        {
            if(quality!=WaterEffectsQuality.Standard||!DetailVisible(point)||bubbles==null)return;
            if(view!=null&&(point-view.transform.position).sqrMagnitude>32*32)return;
            // Underwater only. Manual emission reuses one bounded native particle buffer.
            if(point.y>(pool!=null?pool.Waterline:0)+.03f)return;
            var e=new ParticleSystem.EmitParams{position=point,velocity=Vector3.up*.2f,startSize=.018f+Mathf.Min(speed,6)*.003f,startLifetime=.4f};
            if(!bubbles.isPlaying)bubbles.Play();bubbles.Emit(e,2);BubbleEmissions+=2;
        }
        private void LateUpdate()
        {
            if (contacts != null)
            {
                if (appliedQuality != quality) { appliedQuality = quality; Clear(); }
                int surfaces = Mathf.Min(surfaceBudget, quality == WaterEffectsQuality.Low ? 32 :
                    quality == WaterEffectsQuality.Standard ? 64 : WaterContactMesh.MaximumSurfaces);
                int droplets = Mathf.Min(dropletBudget, quality == WaterEffectsQuality.Low ? 38 :
                    quality == WaterEffectsQuality.Standard ? 84 : WaterContactMesh.MaximumParticles);
                contacts.Tick(quality, surfaces, droplets, quality == WaterEffectsQuality.Low ? Mathf.Min(44f, contactViewRange) : contactViewRange);
                ActiveEffects = contacts.SurfaceCount;
                return;
            }
            if (match!=null&&match.IsPaused){if(bubbles!=null&&!bubbles.isPaused)bubbles.Pause();return;}
            if(bubbles!=null&&bubbles.isPaused)bubbles.Play();
            if(appliedQuality!=quality){appliedQuality=quality;Clear();}
            ActiveEffects=0;
            if(quality==WaterEffectsQuality.Off)return;
            for(int i=0;i<ripples.Length;i++)
            {
                var r=ripples[i];if(r.age>=0.9f)continue;r.age+=Time.deltaTime;
                if(r.age>=0.9f){r.root.gameObject.SetActive(false);continue;}ActiveEffects++;
                float size=(0.15f+r.age*1.8f)*r.size*(0.6f+r.strength);
                r.root.localScale=new Vector3(size,1f,size);
                r.splash.localScale=new Vector3(0.65f,Mathf.Sin(Mathf.Clamp01(r.age/0.6f)*Mathf.PI)*r.strength,0.65f);
                colors.SetColor(BaseColor,new Color(0.7f,0.94f,1f,(1f-r.age/0.9f)*0.75f));r.ringRenderer.SetPropertyBlock(colors);r.splashRenderer.SetPropertyBlock(colors);
            }
            for(int i=0;i<wakes.Length;i++)
            {
                var player=roster.Members[i];Vector3 velocity=player.Motor.Velocity;float speed=velocity.magnitude;
                bool visible=match.CanSwim && player.EligibleForPlay && speed>0.3f && (quality==WaterEffectsQuality.Standard || speed>4f)&&DetailVisible(player.Motor.Position);
                wakes[i].enabled=visible;if(!visible)continue;
                Vector3 point=player.Motor.Position;point.y=pool.Waterline+0.015f;
                Vector3 behind=-velocity.normalized,right=Vector3.Cross(Vector3.up,behind);float length=0.45f+speed*0.1f;
                wakes[i].SetPosition(0,point+behind*length+right*0.26f);wakes[i].SetPosition(1,point+behind*0.35f);wakes[i].SetPosition(2,point+behind*length-right*0.26f);
                Color color=new Color(0.72f,0.94f,1f,Mathf.Clamp01(speed/6f)*0.6f);wakes[i].startColor=color;wakes[i].endColor=color;
                wakes[i].widthMultiplier=wakeStamina[i]!=null&&wakeStamina[i].IsSprinting?.105f:.065f;
            }
            if(ballTrail!=null)ballTrail.emitting=match.BallInMotion && ball.Owner==null && ball.ReleaseKind==BallReleaseKind.Shot && ball.Velocity.magnitude>9f;
        }
        private static Mesh Ring()
        {
            const int count=24;var vertices=new Vector3[count*4];var triangles=new int[count*12];
            for(int ring=0;ring<2;ring++)for(int i=0;i<count;i++)
            {
                int offset=ring*count*2;float radius=ring==0?.5f:.32f;
                float a=i*Mathf.PI*2f/count;var v=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));vertices[offset+i*2]=v*radius;vertices[offset+i*2+1]=v*(radius-.025f);
                int n=(i+1)%count;int t=(ring*count+i)*6;triangles[t]=offset+i*2;triangles[t+1]=offset+n*2;triangles[t+2]=offset+i*2+1;triangles[t+3]=offset+i*2+1;triangles[t+4]=offset+n*2;triangles[t+5]=offset+n*2+1;
            }
            var mesh=new Mesh {name="Reusable water ring",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Splash()
        {
            var v=new Vector3[24];var t=new int[24];
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4f;Vector3 radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a)),side=Vector3.Cross(Vector3.up,radial)*0.035f;
                v[i*3]=radial*0.15f-side;v[i*3+1]=radial*0.15f+side;v[i*3+2]=radial*0.32f+Vector3.up*(i%2==0?0.45f:0.3f);
                t[i*3]=i*3;t[i*3+1]=i*3+1;t[i*3+2]=i*3+2;
            }
            var mesh=new Mesh {name="Reusable splash spokes",vertices=v,triangles=t};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        internal void ShowRecordedContacts(float playbackTime) => contacts?.SetPlayback(playbackTime);
        internal void EndRecordedContacts() => contacts?.EndPlayback();
        internal void Celebrate(Vector3 point, float strength) => EmitAthleteSurface(point, strength, true);
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || contacts == null || pool == null) return;
            var plane = pool.transform.position;
            plane.y = pool.Waterline;
            Gizmos.color = new Color(.1f, .9f, 1f, .55f);
            Gizmos.DrawWireCube(plane, new Vector3(pool.Width, .002f, pool.Length));
            if (LeftHandEntries + RightHandEntries > 0) Gizmos.DrawWireSphere(LastHandEntryPoint, .13f);
            Gizmos.color = new Color(1f, .83f, .20f, .9f);
            if (WaterImpacts > 0) Gizmos.DrawWireSphere(LastWaterImpactPoint, .13f);
        }
#endif
        private void OnDestroy(){contacts?.Dispose();Destroy(ringMesh);Destroy(splashMesh);}
    }
}
