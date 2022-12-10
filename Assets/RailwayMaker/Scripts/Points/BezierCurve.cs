using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class BezierCurve
    {
        public enum PointTypes
        {
            Start,
            ControlStart,
            ControlEnd,
            End
        }
        public enum Sides
        {
            None,
            Start,
            End
        }
        
        public AnchorPoint start;
        public AnchorPoint end;
        public ControlPoint controlStart;
        public ControlPoint controlEnd;

        public BezierCurve(AnchorPoint a, ControlPoint b, ControlPoint c, AnchorPoint d)
        {
            start = a;
            end = d;
            controlStart = b;
            controlEnd = c;
        }

        public Sides HasPoint(Point p)
        {
            if (start == p || controlStart == p)
                return Sides.Start;
            if (controlEnd == p || end == p)
                return Sides.End;
            return Sides.None;
        }

        public void SetAnchor(AnchorPoint p, Sides s)
        {
            switch (s)
            {
                case Sides.Start:
                    start = p;
                    break;
                case Sides.End:
                    end = p;
                    break;
            }
        }
        public AnchorPoint GetAnchor(Sides s)
        {
            switch (s)
            {
                case Sides.Start:
                    return start;
                case Sides.End:
                    return end;
            }
            return null;
        }

        public void SetControl(ControlPoint p, Sides s)
        {
            switch (s)
            {
                case Sides.Start:
                    controlStart = p;
                    break;
                case Sides.End:
                    controlEnd = p;
                    break;
            }
        }

        public ControlPoint GetControl(Sides s)
        {
            switch (s)
            {
                case Sides.Start:
                    return controlStart;
                case Sides.End:
                    return controlEnd;
            }
            return null;
        }

        // https://stackoverflow.com/questions/4089443/find-the-tangent-of-a-point-on-a-cubic-bezier-curve
        public Vector3 GetVelocity(float t)
        {
            float m = 1f - t;

            return 3 * m * m * (controlStart.position - start.position) +
                   6 * m * t * (controlEnd.position - controlStart.position) +
                   3 * t * t * (end.position - controlEnd.position);
        }

        public Vector3 GetTangent(float t) => GetVelocity(t).normalized;

        
        public Vector3 GetNormal(float t)
        {
            Vector3 tangent = GetTangent(t);
            
            Quaternion lerpRot = Quaternion.Lerp(start.rotation, end.rotation, t);
            Vector3 binormal = Vector3.Cross(tangent, lerpRot * Vector3.down);
            binormal.Normalize();
            
            Vector3 normal = Vector3.Cross(tangent, binormal);

            return normal;
        }

        public Vector3 GetPosition(float t)
        {
            float m = 1f - t;
            return m * m * m * start.position + 
                   3f * t * m * m * controlStart.position +
                   3f * t * t * m * controlEnd.position + 
                   t * t * t * end.position;
        }

        public bool IsInvalid()
        {
            if (start == end)
                return true;
            if (start == null || end == null || controlStart == null || controlEnd == null)
                return true;
            return false;
        }
        /*
        public ControlPoint GetControl(PointTypes t)
        {
            if (t is PointTypes.Start or PointTypes.End) return null;
            return (ControlPoint) this[t];
        }
        public AnchorPoint GetAnchor(PointTypes t)
        {
            if (t is PointTypes.ControlStart or PointTypes.ControlEnd) return null;
            return (AnchorPoint) this[t];
        }
        
        private Point this[PointTypes t]
        {
            get
            {
                switch (t)
                {
                    case PointTypes.Start: return start;
                    case PointTypes.ControlStart: return controlStart;
                    case PointTypes.ControlEnd: return controlEnd;
                    case PointTypes.End: return end;
                    default: return null;
                }
            }
        }*/
    }
}