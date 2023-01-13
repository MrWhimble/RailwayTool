using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class CurveUtility
    {
        public static BezierCurve.Sides Opposite(BezierCurve.Sides s)
        {
            if (s == BezierCurve.Sides.End)
                return BezierCurve.Sides.Start;
            if (s == BezierCurve.Sides.Start)
                return BezierCurve.Sides.End;
            return BezierCurve.Sides.None;
        }
        
        public struct CurvePointData
        {
            public BezierCurve curve;
            public BezierCurve.Sides side;

            public CurvePointData(BezierCurve c, BezierCurve.Sides s)
            {
                curve = c;
                side = s;
            }
            public ControlPoint GetControl() => side == BezierCurve.Sides.Start ? curve.controlStart : curve.controlEnd;
            public AnchorPoint GetAnchor() => side == BezierCurve.Sides.Start ? curve.start : curve.end;
        }

        public static CurvePointData GetFirstCurveWithPoint(List<BezierCurve> curves, Point p)
        {
            foreach (var c in curves)
            {
                BezierCurve.Sides s = c.HasPoint(p);
                if (s is BezierCurve.Sides.None)
                    continue;
                return new CurvePointData(c, s);
            }
            return new CurvePointData(null, BezierCurve.Sides.None);
        }
        public static List<CurvePointData> GetCurvesWithPoint(List<BezierCurve> curves, Point p)
        {
            List<CurvePointData> ret = new List<CurvePointData>();
            foreach (var c in curves)
            {
                BezierCurve.Sides s = c.HasPoint(p);
                if (s is BezierCurve.Sides.None)
                    continue;
                ret.Add(new CurvePointData(c, s));
            }
            return ret;
        }
        public static List<CurvePointData> GetCurvesWithPoint(List<CurvePointData> curves, Point p)
        {
            List<CurvePointData> ret = new List<CurvePointData>();
            foreach (var c in curves)
            {
                BezierCurve.Sides s = c.curve.HasPoint(p);
                if (s is BezierCurve.Sides.None)
                    continue;
                ret.Add(new CurvePointData(c.curve, s));
            }
            return ret;
        }
        public static int GetCurvesCountWithPoint(List<BezierCurve> curves, Point p)
        {
            int ret = 0;
            foreach (var c in curves)
            {
                BezierCurve.Sides s = c.HasPoint(p);
                if (s is BezierCurve.Sides.None)
                    continue;
                ret++;
            }
            return ret;
        }

        public struct CurveDistanceData
        {
            public int curveIndex;
            public float distance;
            public float t;

            public CurveDistanceData(int c, float d, float t)
            {
                curveIndex = c;
                distance = d;
                this.t = t;
            }
        }

        public static CurveDistanceData GetClosestCurveToPointUsingPathData(RailPathData pathData, Vector3 point)
        {
            if (pathData == null || !pathData.IsValid())
                return default;
            
            float originalDelta = 1f / 20f;
            int bestCurveIndex = -1;
            float bestT = -1f;
            float bestDist = Mathf.Infinity;

            for (var i = 0; i < pathData.pathCurves.Count; i++)
            {
                PathCurve pathCurve = pathData.pathCurves[i];
                float delta = originalDelta;
                float cBestT = 0;
                float cBestDist = Mathf.Infinity;

                Vector3[] curvePoints = GetCurvePointsUsingPathData(pathCurve, pathData.pathPoints);

                for (float t = 0f; t <= 1.0001f; t += delta)
                {
                    Vector3 cPos = GetPointFromTOnCubicBezierCurve(curvePoints, t);
                    float dist = Vector3.Distance(cPos, point);
                    if (dist < cBestDist)
                    {
                        cBestDist = dist;
                        cBestT = t;
                    }
                }

                cBestT -= delta * 0.5f;
                float lastT = cBestT + delta;
                delta *= delta;
                cBestT = Mathf.Clamp01(cBestT);

                cBestDist = Mathf.Infinity;
                for (float t = cBestT; t <= lastT; t += delta)
                {
                    if (t > 1)
                        break;
                    Vector3 cPos = GetPointFromTOnCubicBezierCurve(curvePoints, t);
                    float dist = Vector3.Distance(cPos, point);
                    if (dist < cBestDist)
                    {
                        cBestDist = dist;
                        cBestT = t;
                    }
                }

                cBestT = Mathf.Clamp01(cBestT);

                
                if (cBestDist < bestDist)
                {
                    bestCurveIndex = i;
                    bestDist = cBestDist;
                    bestT = cBestT;
                }
            }

            return new CurveDistanceData(bestCurveIndex, bestDist, bestT);
        }

        public static int GetClosestRailNodeIndexToPointUsingPathData(RailPathData pathData, Vector3 point)
        {
            int index = 0;
            float bestDist = Mathf.Infinity;
            int bestIndex = -1;
            
            foreach (var a in pathData.pathPoints)
            {
                if (a.pointType == PathPoint.PathPointTypes.Control)
                    continue;

                float dist = Vector3.Distance(point, a.position);
                if (dist < bestDist)
                {
                    bestIndex = index;
                    bestDist = dist;
                }

                index++;
            }

            return bestIndex;
        }

        public static int GetAnchorPointIndexFromRailNodeIndex(List<PathPoint> pathPoints, int railNodeIndex)
        {
            int index = 0;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i].pointType == PathPoint.PathPointTypes.Control)
                    continue;

                if (railNodeIndex == index)
                {
                    return i;
                }

                index++;
            }

            return -1;
        }

        public static Vector3 GetPointFromTOnCubicBezierCurve(Vector3 start, Vector3 controlStart, Vector3 controlEnd, Vector3 end, float t)
        {
            float m = 1f - t;
            return m * m * m * start + 
                   3f * t * m * m * controlStart +
                   3f * t * t * m * controlEnd + 
                   t * t * t * end;
        }
        public static Vector3 GetPointFromTOnCubicBezierCurve(Vector3[] positions, float t)
        {
            return GetPointFromTOnCubicBezierCurve(positions[0], positions[1], positions[2], positions[3], t);
        }

        public static Vector3[] GetCurvePointsUsingPathData(PathCurve pathCurve, List<PathPoint> pathPoints)
        {
            return new[]
            {
                pathPoints[pathCurve.start].position,
                pathPoints[pathCurve.controlStart].position,
                pathPoints[pathCurve.controlEnd].position,
                pathPoints[pathCurve.end].position
            };
        }
    }
}