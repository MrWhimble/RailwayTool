using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using MrWhimble.ConstantConsole;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Graph
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
            //StartCoroutine(ConstructNetwork());
            StartCoroutine(ConstructNetwork());
        }

        private IEnumerator ConstructNetwork()
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
                        Debug.LogError("Some Error");
                        continue;
                    }
                    
                    bool useNodeAOfCurrent = !otherControl.flipped;
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
                    
                    /*
                    Neighbour thisNeighbour = new Neighbour(
                        railNode,
                        railNode.GetNode(useNodeAOfCurrent),
                        false,
                        curveData.curve.Length);
                    
                    otherRailNode.GetNode(useNodeAOfOther).neighbours.Add(thisNeighbour);
                    */
                }
            }
            
            //DebugRailNodes(railNodes, 10f);

/*
            // look at directions of anchors an controls and determine stuff that way
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
*/
                /*
                foreach (ControlPoint control in current.controlPoints)
                {
                    CurveUtility.CurvePointData curveData = CurveUtility.GetFirstCurveWithPoint(curves, control);
                    BezierCurve.Sides oppositeSide = CurveUtility.Opposite(curveData.side);
                    RailNode neighbourRailNode = railNodes[points.IndexOf(curveData.curve.GetAnchor(oppositeSide))];
                    Neighbour newNeighbour = new Neighbour(neighbourRailNode, control.flipped, curveData.curve.Length);
                    railNodes[i].neighbours.Add(newNeighbour);
                }*/

                /*
                RailNode railNode = railNodes[i];
                
                for (int j = 0; j < current.controlPoints.Count; j++)
                {
                    ControlPoint control = current.controlPoints[j];
                    
                    CurveUtility.CurvePointData curveData = CurveUtility.GetFirstCurveWithPoint(curves, control);
                    BezierCurve.Sides oppositeSide = CurveUtility.Opposite(curveData.side);
                    
                    AnchorPoint otherAnchor = curveData.curve.GetAnchor(oppositeSide);
                    ControlPoint otherControl = curveData.curve.GetControl(oppositeSide);
                    
                    RailNode otherRailNode = railNodes[points.IndexOf(otherAnchor)];
                    
                    bool useNodeAOfCurrent = !otherControl.flipped;
                    bool useNodeAOfOther = control.flipped != otherControl.flipped;

                    Neighbour newNeighbour = new Neighbour(
                        otherRailNode, 
                        otherRailNode.GetNode(useNodeAOfOther), 
                        control.flipped,
                        curveData.curve.Length);
                    //otherRailNode.GetNode(useNodeAOfOther).neighbourData = newNeighbour;

                    railNode.GetNode(useNodeAOfCurrent).neighbours.Add(newNeighbour);
                    
                    otherRailNode.GetNode(useNodeAOfOther).neighbours.Add(new Neighbour(railNode, railNode.GetNode(useNodeAOfCurrent), otherControl.flipped, curveData.curve.Length));*/
                    /*
                    if (!control.flipped)
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
                    }*/
                    /*
                    
                    //DebugDrawStuff(poss, colorIn, Time.deltaTime);
                    //DebugRailNodes(railNodes, Time.deltaTime);
                    
                    //yield return new WaitForEndOfFrame();
                }
                
            }

            for (int i = railNodes.Count - 1; i >= 0; i--)
            {
                if (railNodes[i] == null)
                    railNodes.RemoveAt(i);
            }

            //DebugRailNodes(railNodes, 10f);

            for (int i = 0; i < railNodes.Count; i++)
            {
                foreach (var n in railNodes[i].nodeA.neighbours)
                {
                    Debug.DrawLine(railNodes[i].anchor.position, n.railNode.anchor.position, Color.cyan, 0.1f);
                }

                //yield return new WaitForSeconds(0.025f);
                foreach (var n in railNodes[i].nodeB.neighbours)
                {
                    Debug.DrawLine(railNodes[i].anchor.position, n.railNode.anchor.position, Color.yellow, 0.1f);
                }

                //yield return new WaitForSeconds(0.025f);
            }

            foreach (var railNode in railNodes)
            {
                Debug.DrawRay(railNode.anchor.position, railNode.direction, Color.magenta, 10f);
            }
            
            //DebugDrawStuff(poss, colorIn, 10f);
            //DebugRailNodes(railNodes, 10f);
*/
            /*
            foreach (var railNode in railNodes)
            {
                if (railNode == null)
                    continue;

                foreach (var neighbour in railNode.neighbours)
                {
                    Debug.DrawLine(railNode.anchor.position, neighbour.node.anchor.position, Color.blue, 10f);
                }
            }
            */
            //yield break;

            DebugRailNodes(railNodes, 2f);
            
            yield break;
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

        public Node GetRandomNode()
        {
            if (railNodes == null || railNodes.Count == 0)
                return null;

            int railNodeIndex = Random.Range(0, railNodes.Count);
            Node ret = railNodes[railNodeIndex].GetNode(Random.value > 0.5f);
            Debug.Log(railNodes[railNodeIndex].index);
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
        
        public IEnumerator GetPath(Node startNode, Node endNode)
        {
            //path = null;
            
            if (startNode == null || endNode == null)
                yield break;

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

            float g = 0f;


            int ind = 0;
            while (openList.Count > 0)
            {
                Debug.Log(ind);
                Node currentNode = null;
                for (int i = 0; i < openList.Count; i++)
                {
                    if (currentNode == null || scores[openList[i]].f < scores[currentNode].f)
                    {
                        currentNode = openList[i];
                    }
                }
                
                //closedList.Add(currentNode);
                

                if (currentNode == endNode)
                {
                    List<Node> path = ConstructPath(startNode, endNode, scores);
                    for (int i = 0; i < path.Count-1; i++)
                    {
                        Debug.DrawLine(path[i].GetPosition(), path[i+1].GetPosition(), Color.blue, 20f);
                    }
                    Debug.Log("Path");
                    yield break;
                }

                List<Neighbour> neighbours = currentNode.neighbours;
                
                //foreach (Neighbour n in neighbours)
                //{
                //    Vector3 pos = currentNode.railNode.anchor.position;
                //    Vector3 nPos = n.railNode.anchor.position;
                //    Debug.DrawLine(pos + Vector3.up, nPos + Vector3.up, Color.green, 1f);
                //}

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
                            //closedList.Remove(neighbour.node);
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

                openList.Remove(currentNode);
                
                //foreach (var o in openList)
                //{
                    //Score s = scores[o];
                    //Debug.DrawLine(s.parent.railNode.anchor.position, o.railNode.anchor.position, Color.red, 1);
                //}


                float delta = 0.1f;
                List<Node> p = ConstructPath(startNode, currentNode, scores);
                for (int i = 0; i < p.Count-1; i++)
                {
                    Debug.DrawLine(p[i].GetPosition(), p[i+1].GetPosition(), Color.blue, delta);
                }

                yield return new WaitForSeconds(delta);
                
                ind++;
            }

            
            
            
            /*
            List<Node> openList = new List<Node>();
            openList.Add(startNode);
            List<Node> closedList = new List<Node>();

            Dictionary<Node, float> g = new Dictionary<Node, float>();
            g.Add(startNode, 0);

            Dictionary<Node, Node> parents = new Dictionary<Node, Node>();
            parents.Add(startNode, startNode);

            while (openList.Count > 0)
            {
                Node n = null;

                for (int i = 0; i < openList.Count; i++)
                {
                    if (n == null || g[openList[i]] + 1f < g[n] + 1f)
                    {
                        n = openList[i];
                        //break;
                    }
                }

                if (n == null)
                {
                    Debug.Log("N null");
                    return null;
                }
                    

                if (n == endNode)
                {
                    List<Node> path = new List<Node>();
                    
                    path.Insert(0, endNode);

                    Node p = parents[endNode];

                    while (p != startNode)
                    {
                        path.Insert(0, p);
                        p = parents[p];
                    }
                    path.Insert(0, p);

                    Debug.Log("Path");
                    return path;
                }

                for (int i = 0; i < n.neighbours.Count; i++)
                {
                    Neighbour neighbour = n.neighbours[i];
                    //if (n.neighbourData.flipped != neighbour.flipped)
                    //    continue;
                    Node otherNode = neighbour.node;

                    if (!openList.Contains(otherNode) && !closedList.Contains(otherNode))
                    {
                        openList.Add(otherNode);
                        parents.Add(otherNode, n);
                        g.Add(otherNode, g[n] + neighbour.distance);
                    }
                    else
                    {
                        if (g[otherNode] > g[n] + neighbour.distance)
                        {
                            if (g.ContainsKey(otherNode))
                                g[otherNode] = g[n] + neighbour.distance;
                            else
                                g.Add(otherNode, g[n] + neighbour.distance);
                            parents.Add(otherNode, n);
                            if (closedList.Contains(otherNode))
                            {
                                closedList.Remove(otherNode);
                                openList.Add(otherNode);
                            }
                        }
                    }
                }

                openList.Remove(n);
                closedList.Add(n);
            }
            */
            
            Debug.Log("End");
            yield break;
        }

        private List<Node> ConstructPath(Node start, Node end, Dictionary<Node, Score> scores)
        {
            List<Node> path = new List<Node>();
                    
            path.Insert(0, end);

            Node p = scores[end].parent;

            while (p != start)
            {
                path.Insert(0, p);
                p = scores[p].parent;
            }
            path.Insert(0, p);
            return path;
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