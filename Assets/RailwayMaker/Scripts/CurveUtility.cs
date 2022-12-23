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

        
    }
}