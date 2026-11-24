using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    [CreateAssetMenu(menuName = "Water Polo/Athlete Kit Style")]
    public sealed class AthleteKitStyle : ScriptableObject
    {
        [SerializeField] Material[] cap;
        [SerializeField] Material[] goalkeeperCap;
        [SerializeField] Material briefs;
        [SerializeField] Material fieldNumber;
        [SerializeField] Material goalkeeperNumber;

        public Material[] Cap(bool goalkeeper) => goalkeeper ? goalkeeperCap : cap;
        public Material Briefs => briefs;
        public Material Number(bool goalkeeper) => goalkeeper ? goalkeeperNumber : fieldNumber;
    }
}

