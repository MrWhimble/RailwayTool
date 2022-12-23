using System;
using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "RailPathData", menuName = "MrWhimble/Rail Path Data", order = 0)]
    public class RailPathData : ScriptableObject
    {
        public List<PathPoint> pathPoints;
        public List<PathCurve> pathCurves;

        public List<Point> GetPoints()
        {
            List<Point> ret = new List<Point>();

            if (pathPoints == null || pathPoints.Count == 0)
                return ret;

            foreach (var pp in pathPoints)
            {
                switch (pp.pointType)
                {
                    case PathPoint.PathPointTypes.Anchor:
                    {
                        AnchorPoint p = new AnchorPoint();
                        p.position = pp.position;
                        p.rotation = pp.rotation;
                        p.controlPoints = new List<ControlPoint>();
                        ret.Add(p);
                        break;
                    }
                    case PathPoint.PathPointTypes.Control:
                    {
                        ControlPoint p = new ControlPoint();
                        p.position = pp.position;
                        p.flipped = pp.flipped;
                        p.anchorPoint = null;
                        ret.Add(p);
                        break;
                    }
                }
            }
            
            /*
            for (int i = 0; i < ret.Count; i++)
            {
                switch (ret[i])
                {
                    case AnchorPoint p:
                    {
                        foreach (var t in pathPoints[i].connectedPoints)
                        {
                            if (t == -1)
                            {
                                Debug.LogWarning($"point {i} has invalid points!");
                                continue;
                            }
                            p.AddControlPoint((ControlPoint)ret[t]);
                        }

                        break;
                    }
                    default:
                        break;
                }
            }*/

            foreach (var pathCurve in pathCurves)
            {
                AnchorPoint start = ret[pathCurve.start] as AnchorPoint;
                ControlPoint controlStart = ret[pathCurve.controlStart] as ControlPoint;
                ControlPoint controlEnd = ret[pathCurve.controlEnd] as ControlPoint;
                AnchorPoint end = ret[pathCurve.end] as AnchorPoint;
                
                start.AddControlPoint(controlStart);
                end.AddControlPoint(controlEnd);
            }
            
            return ret;
        }

        public List<BezierCurve> GetCurves(List<Point> p)
        {
            List<BezierCurve> ret = new List<BezierCurve>();

            if (pathPoints == null || pathPoints.Count == 0)
                return ret;
            if (pathCurves == null || pathCurves.Count == 0)
                return ret;
            if (p == null || p.Count == 0)
                return ret;

            for (int i = 0; i < pathCurves.Count; i++)
            {
                try
                {
                    ret.Add(new BezierCurve(
                        (AnchorPoint) p[pathCurves[i].start],
                        (ControlPoint) p[pathCurves[i].controlStart],
                        (ControlPoint) p[pathCurves[i].controlEnd],
                        (AnchorPoint) p[pathCurves[i].end]));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Debug.LogError($"{e}\nError for curve {i}");
                }
            }

            return ret;
        }
    }

    [System.Serializable]
    public struct PathPoint
    {
        public enum PathPointTypes
        {
            Anchor,
            Control
        }

        public PathPointTypes pointType;
        public Vector3 position;
        public Quaternion rotation;
        public bool flipped;
        public List<int> connectedPoints;
    }

    [System.Serializable]
    public struct PathCurve
    {
        public int start;
        public int controlStart;
        public int controlEnd;
        public int end;
    }
}