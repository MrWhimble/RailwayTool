using System.Collections;
using System.Collections.Generic;
using MrWhimble.ConstantConsole;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [RequireComponent(typeof(RailwayManager))]
    public class RailwayNetwork : MonoBehaviour
    {
        private RailwayManager manager;
        private List<Point> points;
        private List<BezierCurve> curves;

        private List<RailNode> railNodes;

        public void Init()
        {
            manager = GetComponent<RailwayManager>();

            if (manager == null || manager.PathData == null)
                return;

            points = manager.PathData.GetPoints();
            curves = manager.PathData.GetCurves(points);
            foreach (BezierCurve c in curves)
            {
                c.InitDistanceTimeList(50);
            }
        }

        private void Start()
        {
            Init();
            StartCoroutine(ConstructNetwork());
        }

        private IEnumerator ConstructNetwork()
        {
            railNodes = new List<RailNode>();

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] is ControlPoint)
                {
                    railNodes.Add(null);
                    continue;
                }
                RailNode newNode = new RailNode((AnchorPoint)points[i]);
                railNodes.Add(newNode);
            }

            // look at directions of anchors an controls and determine stuff that way
            List<Vector3> poss = new List<Vector3>();
            List<int> colorIn = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] is ControlPoint)
                    continue;
                
                AnchorPoint current = points[i] as AnchorPoint;
                if (current == null)
                {
                    Debug.LogError($"AnchorPoint is null : {i}");
                    continue;
                }

                
                for (int j = 0; j < current.controlPoints.Count; j++)
                {
                    ControlPoint currentControl = current.controlPoints[j];
                    bool controlFlipped = currentControl.flipped;
                    CurveUtility.CurvePointData curveData = CurveUtility.GetFirstCurveWithPoint(curves, currentControl);
                    BezierCurve.Sides otherSide = CurveUtility.Opposite(curveData.side);
                    AnchorPoint otherAnchor = curveData.curve.GetAnchor(otherSide);
                    ControlPoint otherControl = curveData.curve.GetControl(otherSide);
                    int otherIndex = points.IndexOf(otherAnchor);
                    if (!controlFlipped)
                    {
                        if (!otherControl.flipped)
                        {
                            railNodes[i].a.AddNode(railNodes[otherIndex].b);
                            poss.Add(railNodes[i].anchor.position + (railNodes[otherIndex].anchor.position - railNodes[i].anchor.position) * 0.4f);
                            colorIn.Add(0);
                        }
                        else
                        {
                            railNodes[i].b.AddNode(railNodes[otherIndex].a);
                            poss.Add(railNodes[i].anchor.position + (railNodes[otherIndex].anchor.position - railNodes[i].anchor.position) * 0.4f);
                            colorIn.Add(1);
                        }
                    }
                    else
                    {
                        if (!otherControl.flipped)
                        {
                            railNodes[i].a.AddNode(railNodes[otherIndex].a);
                            poss.Add(railNodes[i].anchor.position + (railNodes[otherIndex].anchor.position - railNodes[i].anchor.position) * 0.4f);
                            colorIn.Add(2);
                        }
                        else
                        {
                            railNodes[i].b.AddNode(railNodes[otherIndex].b);
                            poss.Add(railNodes[i].anchor.position + (railNodes[otherIndex].anchor.position - railNodes[i].anchor.position) * 0.4f);
                            colorIn.Add(3);
                        }
                    }

                    
                    DebugDrawStuff(poss, colorIn, Time.deltaTime);
                    DebugRailNodes(railNodes, Time.deltaTime);
                    
                    yield return new WaitForEndOfFrame();
                }
            }

            DebugDrawStuff(poss, colorIn, 10f);
            DebugRailNodes(railNodes, 10f);

        }

        private void DebugDrawStuff(List<Vector3> vs, List<int> cs, float t)
        {
            for (int i = 0; i < vs.Count; i++)
            {
                if (cs[i] == 0)
                    Debug.DrawRay(vs[i], Vector3.up, Color.red, t);
                else if (cs[i] == 1)
                    Debug.DrawRay(vs[i], Vector3.up, Color.blue, t);
                else if (cs[i] == 2)
                    Debug.DrawRay(vs[i], Vector3.up, Color.green, t);
                else if (cs[i] == 3)
                    Debug.DrawRay(vs[i], Vector3.up, Color.magenta, t);
            }
        }
        private void DebugRailNodes(List<RailNode> rn, float time)
        {
            foreach (var railNode in rn)
            {
                if (railNode == null)
                    continue;
                
                Debug.DrawRay(railNode.anchor.position, railNode.anchor.rotation * Vector3.forward, Color.magenta, time);
                
                Vector3 aPos = railNode.anchor.position + (railNode.anchor.rotation * Vector3.right * 0.5f);
                Vector3 bPos = railNode.anchor.position - (railNode.anchor.rotation * Vector3.right * 0.5f);
                
                Debug.DrawRay(aPos, Vector3.up, Color.cyan, time);
                Debug.DrawRay(bPos, Vector3.up, Color.yellow, time);
                
                foreach (var node in railNode.a.toNodes)
                {
                    Vector3 a;
                    if (node.isNodeA)
                        a = node.railNode.anchor.position + (node.railNode.anchor.rotation * Vector3.right * 0.5f);
                    else
                        a = node.railNode.anchor.position - (node.railNode.anchor.rotation * Vector3.right * 0.5f);
                    //DebugDrawArrow(a, aPos, 0, Color.cyan, time);
                    Debug.DrawLine(a, aPos, Color.cyan, time);
                }
                foreach (var node in railNode.b.toNodes)
                {
                    Vector3 a;
                    if (node.isNodeA)
                        a = node.railNode.anchor.position - (node.railNode.anchor.rotation * Vector3.right * 0.5f);
                    else
                        a = node.railNode.anchor.position + (node.railNode.anchor.rotation * Vector3.right * 0.5f);
                    //DebugDrawArrow(a, bPos, 0, Color.yellow, time);
                    Debug.DrawLine(a, bPos, Color.yellow, time);
                }
            }
        }

        private void DebugDrawArrow(Vector3 a, Vector3 b, float angle, Color col, float t)
        {
            Debug.DrawLine(a, b, col, t);
            //Quaternion rot = Quaternion.AngleAxis(angle, b-a);
            Quaternion rot = Quaternion.LookRotation(b-a, Vector3.up);
            float dist = (b - a).magnitude;
            Vector3 tangent = (b - a) / dist;
            tangent *= 0.1f * dist;
            Vector3 binormal = rot * Vector3.right;
            binormal *= 0.1f * dist;
            Debug.DrawLine(a, a + tangent + binormal, col, t);
            Debug.DrawLine(a, a + tangent - binormal, col, t);
        }

        private struct PathCurvePointData
        {
            public PathCurve curve;
            public BezierCurve.Sides side;

            public PathCurvePointData(PathCurve c, BezierCurve.Sides s)
            {
                curve = c;
                side = s;
            }
            public int GetControl() => side == BezierCurve.Sides.Start ? curve.controlStart : curve.controlEnd;
            public int GetAnchor() => side == BezierCurve.Sides.Start ? curve.start : curve.end;
            public int GetAnchor(BezierCurve.Sides s) => s == BezierCurve.Sides.Start ? curve.start : curve.end;
        }

        private List<PathCurvePointData> GetPathCurvesWithPoint(List<PathCurve> pathCurves, int p)
        {
            List<PathCurvePointData> ret = new List<PathCurvePointData>();
            foreach (var c in pathCurves)
            {
                if (c.start == p || c.controlStart == p)
                    ret.Add(new PathCurvePointData(c, BezierCurve.Sides.Start));
                else if (c.end == p || c.controlEnd == p)
                    ret.Add(new PathCurvePointData(c, BezierCurve.Sides.End));
            }

            return ret;
        }
    }
}