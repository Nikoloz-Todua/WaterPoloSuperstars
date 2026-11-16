using System;
using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Architectural dimensions only. The referenced competition field remains authoritative for play.</summary>
    [CreateAssetMenu(menuName="Water Polo/Championship Venue Layout")]
    public sealed class ChampionshipVenueLayout : ScriptableObject
    {
        public ArenaDimensions competition;
        [Min(30)] public float basinLength=50f;
        [Min(22)] public float basinWidth=25f;
        [Min(2)] public float waterDepth=2.05f;
        public float basinCenterZ;
        public float deckHeight=.70f;
        public float bulkheadDistance=14.2f;
        public Vector2 hallHalfSize=new Vector2(56,70);
        public float roofHeight=36.5f;
        public float tierOffset=11f;
        public float rowRun=.86f;
        public float rowRise=.48f;
        public int[] tierRows={9,9,8};
        public float[] tierHeights={1.2f,8.5f,16f};
        public Color ribbonBase=new Color(.015f,.045f,.13f);
        public Color ribbonAccent=new Color(.04f,.65f,.95f);
        public Texture2D ribbonArtwork;
        [Range(0,1)] public float artworkWeight;
        [Range(0,1)] public float ribbonSpeed=.16f;
        [Range(2,20)] public int ribbonUpdateRate=10;
    }
}
