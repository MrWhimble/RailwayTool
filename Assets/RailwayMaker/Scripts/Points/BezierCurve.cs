using MrWhimble.ConstantConsole;
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

        private bool lengthCached = false;
        private float length;
        public float Length => length;

        public float[] distanceTimeList;
        public Vector3[] distancePosList;

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
            //length = ArcLength(start.position, controlStart.position, controlEnd.position, end.position, 20);
            //lengthCached = true;

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

        // https://stackoverflow.com/questions/29438398/cheap-way-of-calculating-cubic-bezier-length
        private float ArcLength(Vector3 A, Vector3 B, Vector3 C, Vector3 D, int subDiv)
        {
            if (subDiv > 0)
            {
                Vector3 a = A + ((B - A) * 0.5f);
                Vector3 b = A + ((B - A) * 0.5f);
                Vector3 c = A + ((B - A) * 0.5f);
                Vector3 d = A + ((B - A) * 0.5f);
                Vector3 e = A + ((B - A) * 0.5f);
                Vector3 f = A + ((B - A) * 0.5f);

                float value = 0f;
                value += ArcLength(A, a, d, f, subDiv - 1);
                value += ArcLength(f, e, c, D, subDiv - 1);
                return value;
            }
            else
            {
                float controlNetLength = (B - A).magnitude + (C - B).magnitude + (D - C).magnitude;
                float chordLength = (D - A).magnitude;
                return (chordLength + controlNetLength) * 0.5f;
            }
        }

        public void InitDistancePosList(int segments)
        {
            if (!lengthCached)
                CalculateLength();

            if (segments < 2)
                segments = 2;

            distancePosList = new Vector3[segments + 1];

            float tDelta = 0.001f;
            float stepDelta = length / segments;
            Vector3 prevPos = GetPosition(0f);
            float travelled = 0f;
            float nextStep = 0f;
            int index = 0;
            for (float t = 0f; t <= 1f; t+=tDelta)
            {
                Vector3 pos = GetPosition(t);
                float dist = (prevPos - pos).magnitude;
                if (travelled + dist >= nextStep-0.0001f)
                {
                    float p = (nextStep - travelled) / dist;
                    distancePosList[index] = GetPosition(t + tDelta * p);
                    index++;
                    if (index == distancePosList.Length)
                        break;
                    nextStep += stepDelta;
                }
                travelled += dist;
                prevPos = pos;
            }

            foreach (var v in distancePosList)
            {
                Debug.DrawRay(v, Vector3.up, Color.yellow, 20f);
            }
        }

        public Vector3 GetPosFromDistance(float distance)
        {
            float dist = (distance / length) * (distancePosList.Length -1 );
            int index = Mathf.FloorToInt(dist);
            float t = dist % 1f;
            return Vector3.Lerp(distancePosList[index], distancePosList[index + 1], t);
        }
        
        public void InitDistanceTimeList(int segments)
        {
            if (!lengthCached)
                CalculateLength();

            

            if (segments < 2)
                segments = 2;

            distanceTimeList = new float[segments + 1];

            float tDelta = 0.001f;
            float stepDelta = length / (segments-1);
            Vector3 prevPos = GetPosition(0f);
            float travelled = 0f;
            float nextStep = 0f;
            int index = 1;
            distanceTimeList[0] = 0f;
            distanceTimeList[^1] = 1f;
            for (float t = tDelta; t <= 1f; t+=tDelta)
            {
                Vector3 pos = GetPosition(t);
                float dist = (prevPos - pos).magnitude;
                if (travelled + dist >= nextStep)//-0.0001f)
                {
                    float p = (nextStep - travelled) / dist;
                    distanceTimeList[index] = t + tDelta * p;
                    index++;
                    if (index == distanceTimeList.Length-1)
                        break;
                    nextStep += stepDelta;
                }
                travelled += dist;
                prevPos = pos;
            }
        }

        public float GetTFromDistance(float distance)
        {
            float dist = (distance / length) * (distanceTimeList.Length - 1f);
            int index = Mathf.FloorToInt(dist);
            float t = dist % 1f;
            return Mathf.Lerp(distanceTimeList[index], distanceTimeList[index + 1], t);
        }
    }
}