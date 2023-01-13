using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MrWhimble.RailwayMaker.Routing;

namespace MrWhimble.RailwayMaker.Graph
{
    public class RailwayNetwork
    {
        private RailwayManager manager;
        private List<Point> points;
        private List<BezierCurve> curves;

        private List<RailNode> railNodes;

        private Dictionary<string, IWaypoint> _waypoints;
        public Dictionary<string, IWaypoint> Waypoints => _waypoints;

        public RailwayNetwork(RailwayManager m)
        {
            manager = m;

            if (manager == null || manager.PathData == null)
                return;

            points = manager.PathData.GetPoints();
            curves = manager.PathData.GetCurves(points);
            foreach (BezierCurve c in curves)
            {
                c.InitDistanceList(100);
            }
            
            ConstructNetwork();
        }

        private void ConstructNetwork()
        {
            railNodes = new List<RailNode>();

            // Add RailNodes
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] is ControlPoint)
                {
                    railNodes.Add(null);
                    continue;
                }
                RailNode newNode = new RailNode();
                newNode.anchor = (AnchorPoint) points[i];
                newNode.direction = ((AnchorPoint)points[i]).rotation * Vector3.forward;
                newNode.index = i;
                railNodes.Add(newNode);
            }

            for (int i = railNodes.Count - 1; i >= 0; i--)
            {
                if (railNodes[i] == null)
                    railNodes.RemoveAt(i);
            }

            foreach (Point p in points)
            {
                if (p is ControlPoint)
                    continue;

                if (p == null)
                    continue;

                AnchorPoint anchor = p as AnchorPoint;
                RailNode railNode = null;
                for (int i = 0; i < railNodes.Count; i++)
                {
                    if (railNodes[i].anchor == anchor)
                    {
                        railNode = railNodes[i];
                        break;
                    }
                }
                foreach (ControlPoint control in anchor.controlPoints)
                {
                    var curveData = CurveUtility.GetFirstCurveWithPoint(curves, control);
                    BezierCurve.Sides otherSide = CurveUtility.Opposite(curveData.side);
                    ControlPoint otherControl = curveData.curve.GetControl(otherSide);
                    AnchorPoint otherAnchor = curveData.curve.GetAnchor(otherSide);

                    RailNode otherRailNode = null;
                    for (int i = 0; i < railNodes.Count; i++)
                    {
                        if (railNodes[i].anchor == otherAnchor)
                        {
                            otherRailNode = railNodes[i];
                            break;
                        }
                    }

                    if (otherRailNode == null)
                    {
                        Debug.LogError("Other RailNode is Null");
                        continue;
                    }
                    
                    bool useNodeAOfOther = control.flipped != otherControl.flipped;

                    Neighbour otherNeighbour = new Neighbour(
                        otherRailNode,
                        otherRailNode.GetNode(useNodeAOfOther),
                        curveData.curve.Length,
                        railNode.anchor.rotation * Vector3.forward * (control.flipped ? -1f : 1f),
                        otherRailNode.anchor.rotation * Vector3.forward * (otherControl.flipped ? -1f : 1f));
                    railNode.nodeA.neighbours.Add(otherNeighbour);
                    
                    otherNeighbour = new Neighbour(
                        otherRailNode,
                        otherRailNode.GetNode(!useNodeAOfOther),
                        curveData.curve.Length,
                        railNode.anchor.rotation * Vector3.forward * (control.flipped ? -1f : 1f),
                        otherRailNode.anchor.rotation * Vector3.forward * (otherControl.flipped ? -1f : 1f));
                    railNode.nodeB.neighbours.Add(otherNeighbour);
                    
                }
            }
            
            var waypoints = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<IWaypoint>();
            _waypoints = new Dictionary<string, IWaypoint>();
            foreach (var wp in waypoints)
            {
                _waypoints.Add(wp.Name, wp);
            }
        }

        private void DebugRailNodes(List<RailNode> rn, float time)
        {
            foreach (var railNode in rn)
            {
                if (railNode == null)
                    continue;

                Vector3 anchorPos = railNode.anchor.position;
                
                Debug.DrawRay(anchorPos, railNode.anchor.rotation * Vector3.forward, Color.magenta, time);
                
                Vector3 aPos = railNode.anchor.position + (railNode.anchor.rotation * Vector3.right * 0.5f);
                Vector3 bPos = railNode.anchor.position - (railNode.anchor.rotation * Vector3.right * 0.5f);
                
                Debug.DrawRay(railNode.nodeA.GetPosition(), Vector3.up, Color.blue, time);
                Debug.DrawRay(railNode.nodeB.GetPosition(), Vector3.up, Color.yellow, time);
                
                foreach (var neighbour in railNode.nodeA.neighbours)
                {
                    Debug.DrawLine(neighbour.node.GetPosition() + Vector3.up * 0.1f, aPos+ Vector3.up * 0.1f, Color.blue, time);
                }
                foreach (var neighbour in railNode.nodeB.neighbours)
                {
                    Debug.DrawLine(neighbour.node.GetPosition(), bPos, Color.yellow, time);
                }
            }
        }
        
        private void DebugDrawArrow(Vector3 a, Vector3 b, float angle, Color col, float t)
        {
            Debug.DrawLine(a, b, col, t);
            Quaternion rot = Quaternion.LookRotation(b-a, Vector3.up);
            float dist = (b - a).magnitude;
            Vector3 tangent = (b - a) / dist;
            tangent *= 0.1f * dist;
            Vector3 binormal = rot * Vector3.right;
            binormal *= 0.1f * dist;
            Debug.DrawLine(a, a + tangent + binormal, col, t);
            Debug.DrawLine(a, a + tangent - binormal, col, t);
        }

        public Node GetRandomNode()
        {
            if (railNodes == null || railNodes.Count == 0)
                return null;

            int railNodeIndex = Random.Range(0, railNodes.Count);
            Node ret = railNodes[railNodeIndex].GetNode(Random.value > 0.5f);
            return ret;
        }

        public Node GetNodeAtIndex(int index, bool isNodeA)
        {
            return railNodes[index].GetNode(isNodeA);
        }

        private struct Score
        {
            public float f => g + h; // estimated cost
            public float g; // distance from this node to startNode
            public float h; // straight line distance to endNode
            public Node parent;
            public Neighbour nData;

        }

        public enum RouteState
        {
            Failed,
            Complete
        }

        
        public RouteState GetRoute(Node startNode, Vector3 direction, Node endNode, ref RailwayRoute route)
        {
            if (startNode == null || endNode == null)
            {
                Debug.LogError("start or end node is null!");
                return RouteState.Failed;//yield break;
            }

            if (startNode == endNode)
            {
                Debug.LogError("start and end node are the same!");
                return RouteState.Failed;
            }

            List<Node> openList = new List<Node>();
            openList.Add(startNode);
            List<Node> closedList = new List<Node>();

            Vector3 endNodePosition = endNode.railNode.anchor.position;
            
            Dictionary<Node, Score> scores = new Dictionary<Node, Score>();
            scores.Add(startNode, new Score()
            {
                g = 0f,
                h = Vector3.Distance(startNode.railNode.anchor.position, endNodePosition),
                parent = startNode
            });

            while (openList.Count > 0)
            {
                Node currentNode = null;
                for (int i = 0; i < openList.Count; i++)
                {
                    if (currentNode == null || scores[openList[i]].f < scores[currentNode].f)
                    {
                        currentNode = openList[i];
                    }
                }

                if (currentNode == null)
                {
                    return RouteState.Failed;
                }
                
                closedList.Add(currentNode);
                openList.Remove(currentNode);

                if (currentNode == endNode)
                {
                    ConstructRoute(ref route, startNode, endNode, scores);
                    return RouteState.Complete; //yield break;
                }

                List<Neighbour> neighbours = GetNeighbours(scores[currentNode].parent == currentNode ? direction : -scores[currentNode].nData.enteringDir, currentNode.neighbours);

                foreach (var neighbour in neighbours)
                {
                    if (closedList.Contains(neighbour.node))
                        continue;

                    if (scores[currentNode].parent != currentNode)
                    {
                        float dotProduct = Vector3.Dot(scores[currentNode].nData.enteringDir, neighbour.leavingDir);
                        if (dotProduct > 0f)
                        {
                            continue;
                        }
                    }

                    if (openList.Contains(neighbour.node))
                    {
                        if (scores[currentNode].g + scores[neighbour.node].h < scores[currentNode].f)
                        {
                            Score score = scores[neighbour.node];
                            score.g = scores[currentNode].g + Vector3.Distance(neighbour.node.railNode.anchor.position, endNodePosition);
                            score.parent = currentNode;
                            score.nData = neighbour;
                            scores[neighbour.node] = score;
                            closedList.Remove(neighbour.node);
                            openList.Add(neighbour.node);
                        }
                    }
                    else
                    {
                        Score score = new Score()
                        {
                            parent = currentNode,
                            h = Vector3.Distance(neighbour.node.railNode.anchor.position, endNodePosition),
                            g = scores[currentNode].g + neighbour.distance,
                            nData = neighbour
                        };
                        if (scores.ContainsKey(neighbour.node))
                            scores[neighbour.node] = score;
                        else
                            scores.Add(neighbour.node, score);
                        
                        openList.Add(neighbour.node);
                    }
                }
            }
            return RouteState.Failed;
        }

        private List<Neighbour> GetNeighbours(Vector3 direction, List<Neighbour> allNeighbours)
        {
            List<Neighbour> ret = new List<Neighbour>();
            foreach (var neighbour in allNeighbours)
            {
                bool sameDirection = Vector3.Dot(neighbour.leavingDir, direction) > 0;
                if (sameDirection)
                {
                    ret.Add(neighbour);
                }
            }
            return ret;
        }

        private void ConstructRoute(ref RailwayRoute route, Node start, Node end, Dictionary<Node, Score> scores)
        {
            List<RouteSectionData> routeSectionDatas = new List<RouteSectionData>();
                    
            routeSectionDatas.Insert(0, new RouteSectionData()
            {
                node = end,
                curve = null,
                reverse = false
            });

            Node next = end;
            Node p = scores[end].parent;

            BezierCurve c;
            while (p != start)
            {
                c = GetCurveFromNodes(p, next);
                routeSectionDatas.Insert(0, new RouteSectionData()
                {
                    node = p,
                    curve = c,
                    reverse = !IsTravellingForward(p, next, c)
                });
                next = p;
                p = scores[p].parent;
            }
            
            c = GetCurveFromNodes(p, next);
            routeSectionDatas.Insert(0, new RouteSectionData()
            {
                node = p,
                curve = c,
                reverse = !IsTravellingForward(p, next, c)
            });

            if (route == null)
                route = new RailwayRoute();
            route.sections = routeSectionDatas;
        }

        private BezierCurve GetCurveFromNodes(Node a, Node b)
        {
            AnchorPoint anchorA = a.railNode.anchor;
            AnchorPoint anchorB = b.railNode.anchor;

            var curvesList = CurveUtility.GetCurvesWithPoint(curves, anchorA);
            curvesList = CurveUtility.GetCurvesWithPoint(curvesList, anchorB);

            return curvesList[0].curve;
        }


        private bool IsTravellingForward(Node curr, Node next, BezierCurve curve)
        {
            AnchorPoint anchorCurr = curr.railNode.anchor;
            AnchorPoint anchorNext = next.railNode.anchor;

            BezierCurve.Sides currSide = curve.HasPoint(anchorCurr);
            BezierCurve.Sides nextSide = curve.HasPoint(anchorNext);

            return currSide == BezierCurve.Sides.Start && nextSide == BezierCurve.Sides.End;
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

        public Node GetClosestNode(Vector3 position, Quaternion rotation, bool drivingOnRight = false)
        {
            RailNode bestRailNode = null;
            float bestDistance = Mathf.Infinity;
            foreach (RailNode rn in railNodes)
            {
                if (bestRailNode == null)
                {
                    bestRailNode = rn;
                    continue;
                }

                float distance = Vector3.Distance(rn.anchor.position, position);
                if (distance < bestDistance)
                {
                    bestRailNode = rn;
                    bestDistance = distance;
                }
            }

            if (bestRailNode == null)
                return null;
            
            Vector3 side = rotation * (drivingOnRight ? Vector3.right : Vector3.left);
            side += position;
            if (Vector3.Distance(bestRailNode.nodeA.GetPosition(), side) <
                Vector3.Distance(bestRailNode.nodeB.GetPosition(), side))
            {
                return bestRailNode.nodeA;
            }
            else
            {
                return bestRailNode.nodeB;
            }
        }
    }
}