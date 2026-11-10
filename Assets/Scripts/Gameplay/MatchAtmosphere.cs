using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace ToduStudios.WaterPolo
{
    /// <summary>Observes authoritative events only. Optional licensed clips override original procedural cues.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class MatchAtmosphere : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private MatchRules rules;
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private TMP_Text eventLabel, rulesLabel;
        [SerializeField] private AudioMixerGroup output;
        [SerializeField] private AudioClip whistle, goal, save, impact, period, finalWhistle, crowd;
        [SerializeField, Range(0f,1f)] private float volume = 0.35f;
        [SerializeField] private bool compactFeed;
        [SerializeField] private GameObject eventPanel;
        private AudioSource source, crowdSource;
        private float sfxLevel=1f, crowdLevel=1f;
        public bool SupportsCrowdAudio => crowd != null;
        public float SfxVolume => sfxLevel;
        public float CrowdVolume => crowdLevel;
        public void SetVolumes(float effects, float atmosphere)
        { sfxLevel=Mathf.Clamp01(effects);crowdLevel=Mathf.Clamp01(atmosphere);if(source!=null)source.volume=sfxLevel;if(crowdSource!=null)crowdSource.volume=crowdLevel; }
        private AudioClip whistleCue, goalCue, saveCue, impactCue;
        private AudioClip strongWhistleCue;
        private MatchPhase previousPhase;
        private int score, quarter;
        private float messageUntil, nextStatus, nextImpact;
        private string newest = "", older = "";
        private void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
            source.outputAudioMixerGroup = output; source.volume=sfxLevel;
            if(crowd!=null)
            {
                var crowdObject=new GameObject("CrowdAudio");crowdObject.transform.SetParent(transform,false);
                crowdSource=crowdObject.AddComponent<AudioSource>();crowdSource.playOnAwake=false;crowdSource.spatialBlend=0f;
                crowdSource.outputAudioMixerGroup=output;crowdSource.volume=crowdLevel;
            }
            // Generated once; no downloads, streaming, per-frame synthesis or gameplay-dependent audio timing.
            whistleCue = Cue("Original whistle", 1800f, 0.22f, 0.18f);
            strongWhistleCue = Cue("Original stoppage whistle",1950f,0.45f,0.22f);
            goalCue = Cue("Original goal", 660f, 0.42f, 0.2f);
            saveCue = Cue("Original save", 320f, 0.1f, 0.13f);
            impactCue = Cue("Original impact", 130f, 0.055f, 0.1f);
            previousPhase = match.Phase; score = match.BlueScore + match.WhiteScore; quarter = match.Clock.Quarter;
        }
        private void OnEnable()
        { rules.Decided += Decision; match.Changed += PhaseChanged; match.NewMatch += ClearFeed;match.CentreRestartSignalled+=CentreSignal; ball.Saved += Saved; ball.Impact += Impact;ball.Contact+=WaterContact; }
        private void OnDisable()
        { rules.Decided -= Decision; match.Changed -= PhaseChanged; match.NewMatch -= ClearFeed;match.CentreRestartSignalled-=CentreSignal; ball.Saved -= Saved; ball.Impact -= Impact;ball.Contact-=WaterContact; }
        private void ClearFeed() { newest=older=""; messageUntil=0f;score=0;quarter=1;if(eventLabel!=null)eventLabel.text="";if(eventPanel!=null)eventPanel.SetActive(false); }
        private void Say(string message)
        { older=newest;newest=message;messageUntil=match.PresentationTime+5f; if(eventLabel!=null)eventLabel.text=compactFeed?newest:newest+"\n<size=75%>"+older+"</size>"; if(eventPanel!=null)eventPanel.SetActive(true); }
        private void Decision(RefereeEvent e)
        {
            string player=e.Player!=null?$"{e.Player.Team.ToString().ToUpperInvariant()} #{e.Player.CapNumber}":"";
            string message=e.Decision==RefereeDecision.Ordinary?$"FOUL | {player} | FREE THROW {e.AwardedTeam}":
                e.Decision==RefereeDecision.Exclusion?$"EXCLUSION | {player} | {(e.Player.PermanentlyRemoved?"OUT":e.Player.ExclusionRemaining.ToString("0")+"s")}":
                e.Decision==RefereeDecision.Penalty?$"PENALTY | foul by {player}":
                e.Decision==RefereeDecision.Reentry?$"RE-ENTRY | {player}":$"FREE THROW | {player} | B / L to pass";
            Say(message);
            if(e.Decision!=RefereeDecision.Reentry && e.Decision!=RefereeDecision.FreeThrow)Play(whistle,e.Decision==RefereeDecision.Ordinary?whistleCue:strongWhistleCue);
        }
        private void PhaseChanged()
        {
            if(match.BlueScore+match.WhiteScore!=score)
            {score=match.BlueScore+match.WhiteScore;Play(goal,goalCue);if(crowd!=null && crowdSource!=null)crowdSource.PlayOneShot(crowd,volume*0.5f);if(!compactFeed)Say("GOAL");else{if(eventLabel!=null)eventLabel.text="";if(eventPanel!=null)eventPanel.SetActive(false);}}
            else if(match.Phase==MatchPhase.FullTime && previousPhase!=MatchPhase.FullTime){Play(finalWhistle,strongWhistleCue);Say("FULL TIME");}
            else if((match.Phase==MatchPhase.QuarterBreak || match.Phase==MatchPhase.HalfTime) && previousPhase!=match.Phase)Play(period,strongWhistleCue);
            else if(match.Phase==MatchPhase.Playing && (quarter!=match.Clock.Quarter || previousPhase==MatchPhase.PreMatch))Play(period,whistleCue);
            if(match.Phase==MatchPhase.PenaltyReady)Say($"PENALTY | {rules.Taker.Team} #{rules.Taker.CapNumber} | Aim, hold/release SPACE");
            previousPhase=match.Phase;quarter=match.Clock.Quarter;
        }
        private void CentreSignal(SwimmerIdentity taker){Play(whistle,whistleCue);}
        private void Saved(){Play(save,saveCue);Say("GOALKEEPER SAVE");}
        private void Impact(){if(match.PresentationTime<nextImpact)return;nextImpact=match.PresentationTime+0.12f;Play(impact,impactCue);}
        private void WaterContact(BallImpactEvent e){if(e.Kind==BallImpactKind.Water || e.Kind==BallImpactKind.Skip)Impact();}
        private void Play(AudioClip clip,AudioClip fallback){if(source!=null && volume>0f && (clip!=null || fallback!=null))source.PlayOneShot(clip!=null?clip:fallback,volume);}
        private void Update()
        {
            if (match.IsPaused) return;
            if(eventLabel!=null && match.PresentationTime>messageUntil && eventLabel.text.Length>0){eventLabel.text="";if(eventPanel!=null)eventPanel.SetActive(false);}
            if(match.PresentationTime<nextStatus || rulesLabel==null)return;nextStatus=match.PresentationTime+0.2f;
            var player=selection.ActivePlayer;
            string personal=player!=null?$"{player.Team} #{player.CapNumber} PF {player.PersonalFouls}/{rules.PersonalFoulLimit}":"";
            int blue=rules.ActiveFields(PrototypeTeam.Blue),white=rules.ActiveFields(PrototypeTeam.White);
            string status=blue==white?"":$"FIELDS {blue} v {white}  |  {(blue>white?"BLUE":"WHITE")} MAN UP";
            string excluded=""; int count=0;
            foreach(var p in roster.Members)
                if(!p.EligibleForPlay){count++;if(!compactFeed || count<=2)excluded+=$"  {p.Team} #{p.CapNumber} {(p.PermanentlyRemoved?"OUT":Mathf.CeilToInt(p.ExclusionRemaining)+"s")}";}
            rulesLabel.text=compactFeed?$"PF {player?.PersonalFouls ?? 0}/{rules.PersonalFoulLimit}   FIELDS {blue} : {white}"+(status.Length>0?$"  {(blue>white?"BLUE":"WHITE")} +{Mathf.Abs(blue-white)}":"")+"\n"+excluded+(count>2?$" +{count-2} more":""):personal+"  "+status+excluded;
        }
        private static AudioClip Cue(string name,float hz,float seconds,float gain)
        {
            const int rate=22050;var samples=new float[Mathf.CeilToInt(rate*seconds)];
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate;float envelope=Mathf.Min(1f,t*70f)*Mathf.Clamp01((seconds-t)*25f);samples[i]=Mathf.Sin(t*hz*2f*Mathf.PI+Mathf.Sin(t*35f)*0.6f)*envelope*gain;}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private void OnDestroy(){Destroy(whistleCue);Destroy(strongWhistleCue);Destroy(goalCue);Destroy(saveCue);Destroy(impactCue);Destroy(strongWhistleCue);}
    }
}
