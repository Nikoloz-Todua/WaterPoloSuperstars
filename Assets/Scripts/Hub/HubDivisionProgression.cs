using System;
using System.IO;
using System.Linq;

namespace ToduStudios.WaterPolo.Hub
{
    public sealed partial class HubProgression
    {
        public DivisionCatalog Divisions {get;}
        public bool DivisionUnlocked(string id)=>Unlocked(profile,id);
        bool Unlocked(LocalProfile p,string id)
        {int index=Divisions==null?-1:Divisions.Index(id);return index>=0&&(index==0||p.completedDivisions.Contains(Divisions.divisions[index-1].id));}
        void ValidateDivisions(LocalProfile p)
        {
            if(Divisions==null)
            {
                if(p.completedDivisions.Count>0||p.championship!=null||p.activeMatch?.mode=="division")throw new InvalidDataException("Division configuration unavailable; progress retained.");
                return;
            }
            if(Divisions.divisions.Length!=4||Divisions.winsRequired<1||Divisions.matchLimit<Divisions.winsRequired)throw new InvalidDataException("Invalid championship format.");
            if(p.completedDivisions.Distinct().Count()!=p.completedDivisions.Count||p.completedDivisions.Any(id=>Divisions.Index(id)<0||!Unlocked(p,id)))throw new InvalidDataException("Invalid division unlock history.");
            var run=p.championship;
            if(run!=null&&(string.IsNullOrWhiteSpace(run.id)||!Unlocked(p,run.divisionId)||run.formatId!=Divisions.formatId||run.wins<0||run.matches<run.wins||run.matches>Divisions.matchLimit||run.wins>Divisions.winsRequired))throw new InvalidDataException("Unsupported championship progress; retain the previous format definition before changing it.");
            var session=p.activeMatch;
            if(session!=null)
            {
                if(string.IsNullOrWhiteSpace(session.id)||session.lineup==null||session.lineup.Count!=7||!new[]{"quick","training","division"}.Contains(session.mode))throw new InvalidDataException("Invalid match session.");
                if(session.mode=="division"&&(run==null||Divisions.Ended(run)||session.divisionId!=run.divisionId||session.championshipId!=run.id||session.round!=run.matches+1))throw new InvalidDataException("Invalid official championship session.");
            }
        }
        public bool BeginDivisionMatch(string id,out string message)=>Transaction("launch:"+Guid.NewGuid().ToString("N"),p=>
        {
            if(!Unlocked(p,id))throw new InvalidOperationException("Win the preceding championship to unlock this division.");
            var run=p.championship;
            if(run==null||run.divisionId!=id||Divisions.Ended(run))
                p.championship=run=new ChampionshipProgress{id=Guid.NewGuid().ToString("N"),divisionId=id,formatId=Divisions.formatId};
            p.activeMatch=new LocalMatchSession{id=Guid.NewGuid().ToString("N"),mode="division",divisionId=id,championshipId=run.id,round=run.matches+1,lineup=p.lineup.ToList()};p.pendingResult=null;
        },out message);
        void ApplyDivisionResult(LocalProfile p,MatchReceipt receipt,bool won)
        {
            var session=p.activeMatch;var run=p.championship;
            if(Divisions==null||run==null||Divisions.Ended(run)||session.championshipId!=run.id||session.divisionId!=run.divisionId||session.round!=run.matches+1||!Unlocked(p,run.divisionId))throw new InvalidOperationException("No eligible official championship result.");
            run.matches++;if(won)run.wins++;receipt.divisionId=run.divisionId;
            receipt.divisionMessage=$"Championship: {run.wins}/{Divisions.winsRequired} wins • {run.matches}/{Divisions.matchLimit} matches";
            if(run.wins>=Divisions.winsRequired)
            {
                bool first=!p.completedDivisions.Contains(run.divisionId);if(first)p.completedDivisions.Add(run.divisionId);
                int next=Divisions.Index(run.divisionId)+1;
                receipt.divisionMessage="CHAMPIONSHIP WON"+(first&&next<4?" • "+Divisions.divisions[next].title.ToUpperInvariant()+" UNLOCKED":" • Division complete");
            }
            else if(Divisions.Ended(run))receipt.divisionMessage+="\nChampionship ended. Start a fresh attempt from Game Modes.";
        }
    }
}
