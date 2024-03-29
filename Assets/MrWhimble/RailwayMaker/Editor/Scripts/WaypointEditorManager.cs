﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace MrWhimble.RailwayMaker.Routing
{
    [InitializeOnLoad]
    public static class WaypointEditorManager
    {
        private static RailwayManager _railwayManager;
        
        private static List<Vector3> _prevWaypointPositions;
        private static List<IWaypoint> _waypoints;
        public static List<IWaypoint> Waypoints => _waypoints;

        static WaypointEditorManager()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static void OnAfterAssemblyReload()
        {
            Init();
        }

        private static void OnHierarchyChanged()
        {
            Init();
        }
        
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint) 
                return;
            
            if (_waypoints == null || _waypoints.Count == 0) 
                return;

            List<int> changedIndexes = new List<int>();
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i].Equals(null)) 
                    continue;
                
                GameObject go = _waypoints[i].gameObject;
                if (go == null) 
                    continue;
                
                Vector3 pos = go.transform.position;
                if (_prevWaypointPositions[i] != pos)
                {
                    changedIndexes.Add(i);
                }

                _prevWaypointPositions[i] = pos;
            }

            if (changedIndexes.Count != 0)
            {

                foreach (var index in changedIndexes)
                {
                    int railNodeIndex = CurveUtility.GetClosestRailNodeIndexToPointUsingPathData(
                        _railwayManager.PathData,
                        _waypoints[index].gameObject.transform.position);

                    _waypoints[index].RailNodeIndex = railNodeIndex;
                    EditorUtility.SetDirty(_waypoints[index].gameObject);
                }
            }

            DrawRailPath();

            Handles.color = RailwayEditorSettings.Instance.WaypointConnectionLineColor;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i].Equals(null)) 
                    continue;
                
                GameObject go = _waypoints[i].gameObject;
                if (go == null) 
                    continue;
                
                Vector3 pos = go.transform.position;

                int anchorIndex = CurveUtility.GetAnchorPointIndexFromRailNodeIndex(_railwayManager.PathData.pathPoints,
                    _waypoints[i].RailNodeIndex);
                Vector3 point = _railwayManager.PathData.pathPoints[anchorIndex].position;
                
                Handles.DrawLine(pos, point, RailwayEditorSettings.Instance.WaypointConnectionLineThickness);
            }
        }

        private static void Init()
        {
            _railwayManager = GameObject.FindObjectOfType<RailwayManager>();
            FindSceneWaypoints();
        }

        private static void FindSceneWaypoints()
        {
            var points = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<IWaypoint>();
            _waypoints = new List<IWaypoint>();
            _prevWaypointPositions = new List<Vector3>();
            foreach (var w in points)
            {
                _waypoints.Add(w);
                _prevWaypointPositions.Add(w.gameObject.transform.position);
            }
        }

        private static void DrawRailPath()
        {
            if (_railwayManager == null || _railwayManager.PathData == null || !_railwayManager.PathData.IsValid())
                return;

            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<RailwayManager>() != null)
                return;
            
            foreach (var c in _railwayManager.PathData.pathCurves)
            {
                Vector3[] p = CurveUtility.GetCurvePointsUsingPathData(c, _railwayManager.PathData.pathPoints);
                Handles.DrawBezier(
                    p[0], 
                    p[3], 
                    p[1], 
                    p[2], 
                    RailwayEditorSettings.Instance.RailLineColor, 
                    null, 
                    RailwayEditorSettings.Instance.RailLineThickness);
            }
        }
    }
}