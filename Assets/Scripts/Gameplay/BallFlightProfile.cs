using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum ShotType { Normal, Powerful, High, Skip }
    public enum BallImpactKind { Water, Skip, Frame, Save, Net }
    public readonly struct BallImpactEvent
    {
        public readonly BallImpactKind Kind;
        public readonly Vector3 Position, Incoming, Outgoing;
        public float Intensity => Mathf.Clamp01(Incoming.magnitude / 22f);
        public BallImpactEvent(BallImpactKind kind, Vector3 position, Vector3 incoming, Vector3 outgoing)
        { Kind=kind;Position=position;Incoming=incoming;Outgoing=outgoing; }
    }

    [Serializable]
    public sealed class BallFlightProfile
    {
        [Min(0.05f)] public float normalReleaseHeight=0.22f;
        [Min(0.05f)] public float powerfulReleaseHeight=0.35f;
        [Min(0.1f)] public float highReleaseHeight=0.45f;
        [Min(0.1f)] public float skipReleaseHeight=0.55f;
        [Min(0f)] public float airDrag=0.015f;
        [Range(0f,1f)] public float skipRestitution=0.42f;
        [Range(0f,1f)] public float skipRetention=0.9f;
        [Range(0f,1f)] public float shotWaterRetention=0.94f;
        [Range(0f,1f)] public float lobWaterRetention=0.32f;
        [Min(0.1f)] public float maximumReceivingSpeed=14f;
        [Min(0f)] public float maximumPrediction=1.6f;
    }

    /// <summary>Pure ballistic helpers. They predict intent, never move or acquire the ball.</summary>
    public static class BallTrajectory
    {
        public static float TimeToHeight(float height, float verticalSpeed, float gravity)
        { return (verticalSpeed+Mathf.Sqrt(verticalSpeed*verticalSpeed+2f*gravity*Mathf.Max(0f,height)))/gravity; }
        public static Vector3 Position(Vector3 origin,Vector3 velocity,float seconds,bool gravity)
        { return origin+velocity*seconds+(gravity?Physics.gravity*(seconds*seconds*0.5f):Vector3.zero); }
    }
}
