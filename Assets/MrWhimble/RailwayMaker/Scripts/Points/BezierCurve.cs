using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class BezierCurve
    {
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

        private bool lengthCached = false;
        private float length;
        public float Length => length;

        public float[] distanceList;
        public int halfwayIndex;

        public BezierCurve(AnchorPoint a, ControlPoint b, ControlPoint c, AnchorPoint d)
        {
            start = a;
            end = d;
            controlStart = b;
            controlEnd = c;
            lengthCached = false;
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

        public void ClearAnchorOnSide(Sides s)
        {
            if (s == Sides.Start)
                start = null;
            else if (s == Sides.End)
                end = null;
        }
        public void ClearControlOnSide(Sides s)
        {
            if (s == Sides.Start)
                controlStart = null;
            else if (s == Sides.End)
                controlEnd = null;
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

        public Quaternion GetRotation(float t, bool flip = false)
        {
            return Quaternion.LookRotation(GetTangent(t) * (flip ? -1f : 1f), GetNormal(t));
        }

        public bool IsInvalid()
        {
            if (start == end)
                return true;
            if (start == null || end == null || controlStart == null || controlEnd == null)
                return true;
            return false;
        }

        public bool IsClear()
        {
            if (start == null && end == null && controlStart == null && controlEnd == null)
                return true;
            return false;
        }

        public void CalculateLength()
        {
            float tDelta = 0.0001f;
            Vector3 prevPos = GetPosition(0f);
            float travelled = 0;
            for (float t = tDelta; t <= 1f; t+= tDelta)
            {
                Vector3 pos = GetPosition(t);
                travelled += (pos - prevPos).magnitude;
                prevPos = pos;
            }

            length = travelled;
            lengthCached = true;
        }

        public void InitDistanceList(int segments)
        {
            if (!lengthCached)
                CalculateLength();

            if (segments < 2)
                segments = 2;

            distanceList = new float[segments];

            float distStep = Length / (float) (segments - 1);
            float totalDistance = 0f;
            int distIndex = 0;
            Vector3 prevPos = GetPosition(0);
            float tDelta = 1 / 200f;
            for (float t = 0f; t <= 1.0001f; t += tDelta)
            {
                Vector3 pos = GetPosition(t);
                Vector3 dir = (prevPos - pos);
                float dirMag = dir.magnitude;
                totalDistance += dirMag;
                if (totalDistance >= distIndex * distStep)
                {
                    float overshoot = totalDistance - (distIndex * distStep);
                    float ratio = overshoot / dirMag;
                    distanceList[distIndex] = t - (tDelta * ratio); 
                    distIndex++;
                }
                prevPos = pos;
            }
            
            distanceList[0] = 0f;
            distanceList[^1] = 1f;
        }
        
        public float GetTFromDistance(float distance)
        {
            int segments = distanceList.Length;
            float distanceRatio = distance / Length;
            int distanceIndex = Mathf.FloorToInt(distanceRatio * (float)(segments-1));
            float distanceStep = Length / (float) (segments - 1);
            float distanceOvershoot = distance - distanceStep * (float)distanceIndex;
            float tRatio = distanceOvershoot / distanceStep;
            float tNext = distanceList[Mathf.Min(distanceList.Length-1, distanceIndex+1)];
            float tNow = distanceList[distanceIndex];
            float tDiff = tNext - tNow;

            return tNow + tDiff * tRatio;
        }

        public Vector3 GetPosFromDistance(float distance)
        {
            int segments = distanceList.Length;
            float distanceRatio = distance / Length;
            int distanceIndex = Mathf.FloorToInt(distanceRatio * (float)(segments-1));
            float distanceStep = Length / (float) (segments - 1);
            float distanceOvershoot = distance - distanceStep * (float)distanceIndex;
            float tRatio = distanceOvershoot / distanceStep;
            Vector3 posNext = GetPosition(distanceList[Mathf.Min(distanceList.Length - 1, distanceIndex + 1)]);
            Vector3 posNow = GetPosition(distanceList[distanceIndex]);
            Vector3 posDiff = posNext - posNow;

            return GetPosition(distanceList[distanceIndex]) + posDiff * tRatio;
        }
    }
}