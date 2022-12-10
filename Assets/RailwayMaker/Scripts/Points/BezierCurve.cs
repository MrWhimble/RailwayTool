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
        public Vector3 GetTangent(float t)
        {
            float m = 1f - t;
            return (-3f * m * m * start.position + 
                    3f * m * m * controlStart.position -
                    6f * t * m * controlStart.position - 
                    3f * t * t * controlEnd.position +
                    6f * t * m * controlEnd.position + 
                    3f * t * t * end.position).normalized;
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