using System;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    [Serializable] public sealed class DivisionDefinition { public string id,title; }
    /// <summary>Static content; IDs/order and historical format IDs are save contracts.</summary>
    [CreateAssetMenu(menuName="Water Polo/Division Catalog")]
    public sealed class DivisionCatalog : ScriptableObject
    {
        public const string FormatId="two_wins_in_three_v1";
        public string formatId=FormatId;
        [Min(1)] public int winsRequired=2;
        [Min(1)] public int matchLimit=3;
        public DivisionDefinition[] divisions={
            new DivisionDefinition{id="division_1",title="Division 1"},
            new DivisionDefinition{id="premium_league",title="Premium League"},
            new DivisionDefinition{id="continental_cup",title="Continental Cup"},
            new DivisionDefinition{id="world_champions_league",title="World Champions League"}};
        public int Index(string id)=>Array.FindIndex(divisions,d=>d.id==id);
        public bool Ended(ChampionshipProgress run)=>run!=null&&(run.wins>=winsRequired||run.matches>=matchLimit||run.wins+matchLimit-run.matches<winsRequired);
    }
}
