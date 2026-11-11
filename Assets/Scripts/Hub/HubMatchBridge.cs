using System.Linq;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Observes the existing phase authority. Direct scene tests never initialize hub progress.</summary>
    public sealed class HubMatchBridge : MonoBehaviour
    {
        PrototypeMatch match;string sessionId,mode;bool completed;
        public static bool Active=>HubApplication.Instance!=null&&HubApplication.Instance.Progression!=null;
        void Start()
        {
            if(!Active){enabled=false;return;}
            match=GetComponent<PrototypeMatch>();var p=HubApplication.Instance.Progression.Snapshot;
            if(p.activeMatch==null){enabled=false;return;}sessionId=p.activeMatch.id;mode=p.activeMatch.mode;
            var roster=FindAnyObjectByType<PrototypeRoster>();var blue=roster.Members.Where(x=>x.Team==PrototypeTeam.Blue).OrderBy(x=>x.IsFieldPlayer?1:0).ThenBy(x=>(int)x.FieldPosition).ToArray();
            for(int i=0;i<blue.Length&&i<p.activeMatch.lineup.Count;i++)blue[i].SetCollectionDefinition(p.activeMatch.lineup[i]);
            foreach(var tactics in FindObjectsByType<PrototypeTeamTactics>())if(tactics.Team==PrototypeTeam.Blue)tactics.SetDefense(p.tactics=="Press"?DefenseStrategy.Press:DefenseStrategy.Zone);
            match.Changed+=Observe;match.NewMatch+=Restarted;Observe();
        }
        void Restarted()
        {
            var service=HubApplication.Instance.Progression;
            if(service.BeginMatch(mode,out var message)){sessionId=service.Snapshot.activeMatch.id;completed=false;}else Debug.LogError(message);
        }
        void Observe()
        {
            if(!completed&&match.Phase==MatchPhase.FullTime)TrySaveResult();
        }
        public bool TrySaveResult()
        {
            if(completed)return true;
            if(match==null||match.Phase!=MatchPhase.FullTime)return false;
            if(HubApplication.Instance.Progression.CompleteMatch(sessionId,match.BlueScore,match.WhiteScore,out var message))completed=true;
            else Debug.LogWarning(message);
            return completed;
        }
        void OnDestroy(){if(match!=null){match.Changed-=Observe;match.NewMatch-=Restarted;}}
    }
}
