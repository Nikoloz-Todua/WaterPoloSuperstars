using UnityEngine;
using TMPro;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>A physical end-corner facility. Future substitutions may consume its route, never its own rules.</summary>
    public sealed class VenueTeamFacility : MonoBehaviour
    {
        public int defendedEnd;
        public PrototypeMatch match;
        public PrototypeTeamTactics blue,white;
        public Transform bench,waterEntry;
        public Transform[] seats=new Transform[0];
        public Transform[] accessRoute=new Transform[0];
        public TMP_Text teamLabel;
        public PrototypeTeam CurrentTeam {get;private set;}
        public bool Serves(PrototypeTeam team)=>CurrentTeam==team;
        void OnEnable(){if(match!=null)match.Changed+=Refresh;Refresh();}
        void OnDisable(){if(match!=null)match.Changed-=Refresh;}
        void Refresh()
        {
            if(blue==null||blue.DefendedGoal==null)return;
            CurrentTeam=Mathf.Sign(blue.DefendedGoal.transform.position.z)==Mathf.Sign(defendedEnd)?PrototypeTeam.Blue:PrototypeTeam.White;
            if(teamLabel!=null){teamLabel.text=CurrentTeam==PrototypeTeam.Blue?"BLUE  /  TEAM FACILITY":"WHITE  /  TEAM FACILITY";teamLabel.color=CurrentTeam==PrototypeTeam.Blue?new Color(.22f,.78f,1):new Color(.9f,.95f,1);}
        }
    }
}
