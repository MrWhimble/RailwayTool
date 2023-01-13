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

            float tStep = 1f / (float) (segments-1);
            float tTotal = 0f;
            float tDelta = tStep/20f;
            float totalDistance = 0f;
            int index = 0;
            Vector3 prevPos = GetPosition(0f);

            for (float t = 0f; t <= 1.0001f; t += tDelta)
            {
                Vector3 pos = GetPosition(t);
                totalDistance += (prevPos - pos).magnitude;
                if (tTotal <= t)
                {
                    tTotal += tStep;

                    if (totalDistance > length / 2)
                        halfwayIndex = index;
                    distanceList[index] = totalDistance;
                    index++;
                }
                
                prevPos = pos;
            }

            distanceList[0] = 0f;
            distanceList[^1] = length;
        }
        
        public float GetTFromDistance(float distance)
        {
            int index;
            float tLerp;
            (index, tLerp) = GetIndexAtBiggestDistanceBelowInput(distance);
            int lowIndex = index - 1;
            if (lowIndex == -1)
                return 0f;

            float t = 1f * ((float) lowIndex / (float) (distanceList.Length - 1));
            t += tLerp;
            
            return t;
        }

        private (int, float) GetIndexAtBiggestDistanceBelowInput(float dist)
        {
            int distanceListLength = distanceList.Length - 1;
            float tDelta = 1f / (float) distanceList.Length;
            if (dist < length*0.5f)
            {
                
                int largestIndex = halfwayIndex;
                for (int i = largestIndex; i >= 1; i--) 
                {
                    int otherIndex = i - 1;
                    if (distanceList[otherIndex] <= dist && distanceList[i] >= dist)
                    {
                        return (i, tDelta * Mathf.InverseLerp(distanceList[otherIndex], distanceList[i], dist));
                    }
                }
            }
            else
            {
                
                int largestIndex = Mathf.CeilToInt(distanceListLength);
                for (int i = largestIndex; i > 0; i--)
                {
                    int otherIndex = i - 1;
                    if (distanceList[otherIndex] <= dist && distanceList[i] >= dist)
                    {
                        return (i, tDelta * Mathf.InverseLerp(distanceList[otherIndex], distanceList[i], dist));
                    }
                }
            }

            return (-1, 0f);
        }
    }
}