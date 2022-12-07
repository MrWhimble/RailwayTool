using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "RailPathData", menuName = "MrWhimble/Rail Path Data", order = 0)]
    public class RailPathData : ScriptableObject
    {
        public List<PathPoint> pathPoints;

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
            
            for (int i = 0; i < ret.Count; i++)
            {
                switch (ret[i])
                {
                    case AnchorPoint p:
                    {
                        foreach (var t in pathPoints[i].connectedPoints)
                        {
                            p.AddControlPoint((ControlPoint)ret[t]);
                        }

                        break;
                    }
                    default:
                        break;
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
}