using System.Collections.Generic;
using MrWhimble.RailwayMaker.Graph;
using MrWhimble.RailwayMaker.Routing;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Train
{
    public class Bogie : MonoBehaviour
    {
        [SerializeField] private RailwayNetworkBehaviour networkBehaviour;

        [SerializeField] private RoutingTable routingTable;

        [SerializeField] private Vector3 offset;
        
        [SerializeField, Min(0.0001f)] private float acceleration; 
        [SerializeField, Min(0f)] private float maxSpeed;
        private float _speed;
        
        [SerializeField] private Bogie leader;

        private int _routingTableIndex;
        private bool _stopAtNextWaypoint;
        private LeaveCondition _waypointLeaveCondition;
        private bool _waiting;
        private float _waitTimer;
        
        private Node _previousNode;
        private Node _currentNode;
        private Node _nextNode;

        private RailwayRoute _route;

        private float _tValue;
        private float _distanceTravelled;
        private float _distanceLeft;
        private BezierCurve _currentCurve;
        private bool _reverseCurve;

        
        
        
        
        private float _distanceToLeader;
        private List<Bogie> _followers;
        private bool HasFollowers => _followers != null && _followers.Count > 0;
        private bool IsLeader => leader == null && HasFollowers;
        private bool IsTail => leader != null && !HasFollowers;
        private bool IsSolo => leader == null && !HasFollowers;
        private bool IsFront => leader == null;
        
        private float StoppingDistance => (_speed * _speed) / (2 * acceleration);
        

        private RouteSectionData previousSectionData;

        private float DistanceToFront
        {
            get
            {
                if (IsFront)
                {
                    return 0f;
                }
                else
                {
                    return leader.DistanceToFront + _distanceToLeader;
                }
            }
        }

        private float DistanceLeftAtFront
        {
            get
            {
                if (IsFront)
                    return _distanceLeft;
                return leader.DistanceLeftAtFront;
            }
        }

        private RailwayRoute _sharedRoute;

        private RailwayRoute LeaderSharedRoute
        {
            get
            {
                if (IsLeader)
                    return _sharedRoute;
                return leader.LeaderSharedRoute;
            }
        }

        private void Awake()
        {
            _followers = new List<Bogie>();
            _sharedRoute = new RailwayRoute();
            _routingTableIndex = 0;

            _speed = 0f;
        }
        
        private void Start()
        {
            if (leader != null)
                SetLeader(leader);

            if (routingTable == null)
                _previousNode = networkBehaviour.railwayNetwork.GetClosestNode(transform.position, transform.rotation, false);
            else
            {
                RoutingTableElement element = routingTable.elements[_routingTableIndex];
                IWaypoint prevWaypoint = networkBehaviour.railwayNetwork.Waypoints[element.waypointName];
                _previousNode = networkBehaviour.railwayNetwork.GetNodeAtIndex(prevWaypoint.RailNodeIndex, element.side);
                transform.forward = _previousNode.railNode.direction *
                                    (_previousNode.isNodeA ? 1f : -1f);
                _routingTableIndex = (_routingTableIndex + 1) % routingTable.elements.Count;
            }
        }

        private void Update()
        {
            bool createNewPath = !MoveAlongPath(Time.deltaTime, IsFront);



            if (IsFront)
            {
                if (createNewPath)
                {
                    Node endNode = null;
                    if (routingTable == null)
                    {
                        endNode = networkBehaviour.railwayNetwork.GetRandomNode();
                    }
                    else
                    {
                        RoutingTableElement element = routingTable.elements[_routingTableIndex];
                        IWaypoint nextWaypoint = networkBehaviour.railwayNetwork.Waypoints[element.waypointName];
                        endNode = networkBehaviour.railwayNetwork.GetNodeAtIndex(nextWaypoint.RailNodeIndex,
                            element.side);
                        _stopAtNextWaypoint = nextWaypoint.IsStoppedAt;
                        _waypointLeaveCondition = element.leaveCondition;
                        if (_waypointLeaveCondition == LeaveCondition.AfterTime)
                            _waitTimer = element.waitTime;
                        _routingTableIndex = (_routingTableIndex + 1) % routingTable.elements.Count;
                    }

                    GetRoute(_previousNode, endNode);
                    MoveAlongPath(Time.deltaTime, IsFront);
                }

                float distLeft = _route.GetDistanceToEnd(_distanceTravelled + _speed * Time.deltaTime)-0.2f;
                if (_stopAtNextWaypoint && !_waiting && distLeft < 0.1f)
                {
                    _stopAtNextWaypoint = false;
                    _waiting = true;
                }

                if (_waiting && _waypointLeaveCondition == LeaveCondition.AfterTime && _speed <= 0f)
                {
                    _waitTimer -= Time.deltaTime;
                    if (_waitTimer <= 0f)
                    {
                        _waiting = false;
                    }
                }
                bool slowDown = (_stopAtNextWaypoint && distLeft <= StoppingDistance) || _waiting;
                float acc = slowDown ? -acceleration : acceleration;
                _speed += acc * Time.deltaTime;
                _speed = Mathf.Clamp(_speed, 0, maxSpeed);
            }
        }

        private void GetRoute(Node start, Node end)
        {
            if (IsLeader || IsSolo)
                networkBehaviour.railwayNetwork.GetRoute(start, transform.forward, end, ref _route);
            if (HasFollowers)
            {
                foreach (var f in _followers)
                {
                    f._route = LeaderSharedRoute;
                }
            }
            _distanceTravelled = 0f;
        }

        public void ClearLeader()
        {
            if (leader == null)
                return;
            
            leader.RemoveFollower(this);
        }
        public void SetLeader(Bogie lead)
        {
            if (lead == null)
            {
                ClearLeader();
                return;
            }
            SetLeader(lead, Vector3.Distance(lead.transform.position, transform.position));
        }
        public void SetLeader(Bogie lead, float distance)
        {
            if (lead == null)
            {
                ClearLeader();
                return;
            }
            
            if (leader != null && leader != lead)
                leader.RemoveFollower(this);
            
            leader = lead;
            leader.AddFollower(this);
            _distanceToLeader = distance;
        }
        public void AddFollower(Bogie follower)
        {
            if (_followers == null)
                _followers = new List<Bogie>();
            if (_followers.Contains(follower)) return;
            _followers.Add(follower);
        }
        public void RemoveFollower(Bogie follower)
        {
            if (_followers == null)
                return;
            if (!_followers.Contains(follower)) return;
            _followers.Remove(follower);
        }

        // return true if the route as ended
        private bool MoveAlongPath(float delta, bool asFront = true)
        {
            RailwayRoute route;
            RouteSectionData sectionData;
            BezierCurve curve;
            Node node;
            float t;
            
            if (asFront)
            {
                route = _route;

                if (route == null || !route.HasRoute)
                    return false;

                sectionData = route.sections[0];
                curve = sectionData.curve;

                _distanceTravelled += _speed * delta;
                if (_distanceTravelled >= curve.Length)
                {
                    _previousNode = sectionData.node;
                    _distanceTravelled -= curve.Length;
                    _distanceTravelled += _speed * delta;
                    
                    route.sections.RemoveAt(0);
                    
                    if (_route.sections.Count == 0)
                    {
                        return false;
                    }
                }
                
                
                sectionData = route.sections[0];
                curve = sectionData.curve;
                node = sectionData.node;
                
                if (IsLeader)
                    if (sectionData.curve != null && (_sharedRoute.sections.Count == 0 || !_sharedRoute.sections[0].Equals(sectionData))) _sharedRoute.sections.Insert(0, sectionData);
                
                if (curve == null)
                {
                    _previousNode = node;
                    return false;
                }

                _distanceLeft = curve.Length - _distanceTravelled;
            }
            else
            {
                route = LeaderSharedRoute;

                if (route == null || !route.HasRoute)
                    return false;
                
                float distanceFromFront = DistanceToFront + DistanceLeftAtFront;
                route.GetSectionDataAtDistance(distanceFromFront, out sectionData, out _distanceTravelled);

                if (IsTail && !previousSectionData.Equals(sectionData))
                {
                    route.sections.RemoveAt(route.sections.Count - 1);
                }

                previousSectionData = sectionData;
                
                curve = sectionData.curve;
                node = sectionData.node;
                
                
            }

            

            t = curve.GetTFromDistance(_distanceTravelled);
            if (sectionData.reverse)
                t = 1f - t;
            Vector3 position = curve.GetPosition(t);
            Quaternion rotation = curve.GetRotation(t, sectionData.reverse);
            position += (rotation * offset);
            transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        public void Go()
        {
            _waiting = false;
        }

        private void DebugPath(Node start, Node end, List<Node> p, float t)
        {
            Debug.DrawRay(start.GetPosition(), Vector3.up, Color.red, t);

            Debug.DrawRay(end.GetPosition(), Vector3.up, Color.green, t);

            if (p == null)
            {
                Debug.LogError("Path is null!!");
                return;
            }
            
            for (int i = 0; i < p.Count-1; i++)
            {
                Node current = p[i];
                Node next = p[i + 1];
                Vector3 currentPos = current.GetPosition();
                Vector3 nextPos = next.GetPosition();
                
                Debug.DrawLine(currentPos, nextPos, Color.cyan, t);
            }
        }

        private void OnDrawGizmosSelected()
        {
            DebugRoute(_sharedRoute);
        }

        private void DebugRoute(RailwayRoute r)
        {
            if (r == null || !r.HasRoute)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(r.sections[0].node.GetPosition(), 0.25f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(r.sections[^1].node.GetPosition(), 0.25f);

            Gizmos.color = Color.blue;
            for (int i = 0; i < r.sections.Count-1; i++)
            {
                Gizmos.DrawLine(r.sections[i].node.GetPosition(), r.sections[i+1].node.GetPosition());
            }
        }
    }
}