using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Stable future staging references. Never moves athletes or interrupts the period-opening sprint.</summary>
    public sealed class VenueIntroductionAnchors : MonoBehaviour
    {
        public Transform[] blueLineup=new Transform[0],whiteLineup=new Transform[0];
        public Transform[] blueEntranceRoute=new Transform[0],whiteEntranceRoute=new Transform[0];
        public Transform introductionCamera;
        public VenueCrestDisplay crest;
    }
}
