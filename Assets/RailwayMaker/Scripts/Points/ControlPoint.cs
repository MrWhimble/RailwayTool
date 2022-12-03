using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class ControlPoint : Point
    {
        public AnchorPoint anchorPoint;

        public float distance;
        public bool flipped;

        public void SetAnchorPoint(AnchorPoint p)
        {
            anchorPoint = p;
            distance = Vector3.Distance(position, anchorPoint.position);
        }

        public void UpdatePosition(Vector3 pos)
        {
            position = pos;
            distance = Vector3.Distance(position, anchorPoint.position);
            anchorPoint.UpdateRotation(this);
        }

        public void UpdatePosition()
        {
            Vector3 dir = flipped ? Vector3.back : Vector3.forward;
            dir *= distance;
            dir = anchorPoint.rotation * dir;
            position = anchorPoint.position + dir;
        }
    }
}