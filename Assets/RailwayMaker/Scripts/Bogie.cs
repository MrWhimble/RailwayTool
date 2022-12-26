using System;
using System.Collections;
using System.Collections.Generic;
using MrWhimble.ConstantConsole;
using MrWhimble.RailwayMaker.Graph;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Train
{
    public class Bogie : MonoBehaviour
    {
        [SerializeField] private RailwayNetwork network;

        private Node _previousNode;
        private Node _currentNode;
        private Node _nextNode;

        private RailwayRoute _route;

        [SerializeField, Min(0f)] private float speed;

        private float _tValue;
        private float _distanceTravelled;
        private float _distanceLeft;
        private BezierCurve _currentCurve;
        private bool _reverseCurve;

        [SerializeField] private Bogie leader;
        private List<Bogie> _followers;
        private bool HasFollowers => _followers != null && _followers.Count > 0;
        private bool IsLeader => leader == null && HasFollowers;
        private bool IsTail => leader != null && !HasFollowers;
        private bool IsSolo => leader == null && !HasFollowers;
        private bool IsFront => leader == null;
        private float _distanceToLeader;

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
            
            
        }
        
        private void Start()
        {
            if (leader != null)
                SetLeader(leader);

            _previousNode = network.GetClosestNode(transform.position, transform.rotation, false);
        }

        private void Update()
        {
            //ConstantDebug.Log($"IsLeader = {IsLeader}\nIsFront = {IsFront}", this);
            bool createNewPath = !MoveAlongPath(Time.deltaTime, IsFront);

            if (createNewPath && IsFront)
            {
                Node endNode = network.GetRandomNode();
                GetRoute(_previousNode, endNode);
                MoveAlongPath(Time.deltaTime, IsFront);
            }
        }

        private void GetRoute(Node start, Node end)
        {
            if (IsLeader || IsSolo)
                network.GetRoute(start, transform.forward, end, ref _route);
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

                _distanceTravelled += speed * delta;
                if (_distanceTravelled >= curve.Length)
                {
                    _previousNode = sectionData.node;
                    _distanceTravelled -= curve.Length;
                    _distanceTravelled += speed * delta;
                    
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
            transform.SetPositionAndRotation(position, rotation);
            return true;
        }
        /*
        private bool MoveAlongPath(float delta)
        {
            if (_route == null || !_route.HasRoute)
                return true;

            _currentNode = _route.sections[0].node;
            _currentCurve = _route.sections[0].curve;
            
            _distanceTravelled += speed * delta;
            if (_distanceTravelled >= _currentCurve.Length)
            {
                _previousNode = _currentNode;
                _distanceTravelled -= _currentCurve.Length;
                _distanceTravelled += speed * delta;
                if (IsLeader)
                {
                    _sharedRoute.sections.Insert(0, _route.sections[0]);
                    
                }

                if (IsLeader || IsSolo)
                {
                    _route.sections.RemoveAt(0);
                }

                if (IsTail)
                {
                    LeaderSharedRoute.sections.RemoveAt(LeaderSharedRoute.sections.Count - 1);
                }
                
                if (_route.sections.Count == 0)
                {
                    return true;
                }

                _currentNode = _route.sections[0].node;
                _currentCurve = _route.sections[0].curve;
                
            }
            
            
            //_currentCurve = _route.sections[0].curve;

            if (_currentCurve == null)
            {
                _previousNode = _currentNode;
                return true;
            }

            
            if (_currentCurve == null)
            {
                _previousNode = _currentNode;
                return true;
            }
            
            _tValue = _currentCurve.GetTFromDistance(_distanceTravelled);
            if (_route.sections[0].reverse)
            {
                _tValue = 1f - _tValue;
            }
            
            //ConstantDebug.Log(_route.sections[0].reverse);

            Quaternion rotation = _currentCurve.GetRotation(_tValue, _route.sections[0].reverse);
            Vector3 position = _currentCurve.GetPosition(_tValue);
            
            transform.SetPositionAndRotation(position, rotation);
            
            return false;
        }
        */

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
            //DebugRoute(_route);
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