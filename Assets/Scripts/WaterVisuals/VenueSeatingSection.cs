using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Authoring measurements for orientation and clearance checks, not thousands of per-seat behaviours.</summary>
    public sealed class VenueSeatingSection : MonoBehaviour
    {
        public int tier,rows,seatsPerRow;
        public float rowRun=.86f,rowRise=.48f,seatSpacing=.64f;
        public Transform fullSeatVisual,coarseSeatVisual;
        public Vector3 SeatFloor(int row,int seat)=>transform.TransformPoint(new Vector3((seat-(seatsPerRow-1)*.5f)*seatSpacing,row*rowRise,-row*rowRun));
        public Vector3 ViewingDirection=>transform.forward;
        public int SeatCount=>rows*seatsPerRow;
    }
}
