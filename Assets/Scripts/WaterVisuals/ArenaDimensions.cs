using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    [CreateAssetMenu(menuName = "Water Polo/Arena Dimensions")]
    public sealed class ArenaDimensions : ScriptableObject
    {
        [Min(12)] public float fieldWidth = 20f;
        [Min(20)] public float fieldLength = 25f;
        [Min(2)] public float depth = 2.05f;
        [Min(1)] public float sideWaterBuffer = 1.25f;
        [Min(1.5f)] public float endWaterBuffer = 1.8f;
        [Min(6)] public float deckWidth = 7f;
        [Min(0)] public float deckElevation = .18f;
        public float PoolWidth => fieldWidth + sideWaterBuffer * 2;
        public float PoolLength => fieldLength + endWaterBuffer * 2;
    }
}
