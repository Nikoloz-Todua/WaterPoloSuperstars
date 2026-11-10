using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Compressed display time, advanced exclusively by the authoritative live match phase.</summary>
    [Serializable]
    public sealed class MatchClock
    {
        [SerializeField, Min(1)] private int quarters = 4;
        [SerializeField, Min(1f)] private float realSecondsPerQuarter = 80f;
        [SerializeField, Min(1f)] private float displayedSecondsPerQuarter = 480f;
        public int Quarter { get; private set; } = 1;
        public int Quarters => Mathf.Max(1,quarters);
        private double elapsedLiveSeconds;
        public float RealDuration => Mathf.Max(1f,realSecondsPerQuarter);
        public float DisplayRatio => displayedSecondsPerQuarter/RealDuration;
        public float Remaining => (float)Math.Max(0d,RealDuration-elapsedLiveSeconds);
        public float DisplayRemaining => Remaining*DisplayRatio;
        public void Reset() { Quarter=1;elapsedLiveSeconds=0d; }
        public void NextQuarter() { Quarter++;elapsedLiveSeconds=0d; }
        public bool Tick(double seconds)
        {
            if(elapsedLiveSeconds>=RealDuration || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<=0d)return false;
            elapsedLiveSeconds=Math.Min(RealDuration,elapsedLiveSeconds+seconds);
            // Absorb float fixed-step representation error without losing a whole physics step at 80s.
            if(elapsedLiveSeconds>=RealDuration-0.00001d){elapsedLiveSeconds=RealDuration;return true;}
            return false;
        }
    }
}
