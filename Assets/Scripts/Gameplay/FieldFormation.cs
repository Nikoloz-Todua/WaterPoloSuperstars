using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Pool-centred team coordinates: X is lateral and Y is progress toward the attacking goal.</summary>
    [Serializable]
    public struct FieldFormation
    {
        public FieldPositionRole role;
        public Vector2 periodStart;
        public Vector2 attack;
        public Vector2 zone;

        public FieldFormation(FieldPositionRole role, Vector2 start, Vector2 attack, Vector2 zone)
        { this.role=role;periodStart=start;this.attack=attack;this.zone=zone; }

        public static FieldFormation[] Defaults() => new[]
        {
            new FieldFormation(FieldPositionRole.CentreForward,new Vector2(0f,-3.5f),new Vector2(0f,11f),new Vector2(0f,-6f)),
            new FieldFormation(FieldPositionRole.CentreBack,new Vector2(0f,-10f),new Vector2(0f,3.5f),new Vector2(0f,-10f)),
            new FieldFormation(FieldPositionRole.LeftWing,new Vector2(-7f,-5.8f),new Vector2(-7f,9f),new Vector2(-6f,-9f)),
            new FieldFormation(FieldPositionRole.RightWing,new Vector2(7f,-5.8f),new Vector2(7f,9f),new Vector2(6f,-9f)),
            new FieldFormation(FieldPositionRole.LeftFlat,new Vector2(-3.8f,-7.6f),new Vector2(-4f,6.5f),new Vector2(-3.5f,-6.5f)),
            new FieldFormation(FieldPositionRole.RightFlat,new Vector2(3.8f,-7.6f),new Vector2(4f,6.5f),new Vector2(3.5f,-6.5f))
        };
    }
}
