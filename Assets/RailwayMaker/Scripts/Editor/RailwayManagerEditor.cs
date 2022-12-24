using System;
using System.Collections.Generic;
using MrWhimble.ConstantConsole;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailwayManager))]
    public class RailwayManagerEditor : Editor
    {
        private bool _updateInspector;
        private bool _updateScene;
        
        private List<int> _pointControlIds;
        private List<Point> _points;
        private List<BezierCurve> _curves;

        private SerializedProperty _pathDataProp;

        private int _lastPointSelectedIndex;
        private int _selectedPointIndex;
        private Vector3 _selectedPointRotation;

        private CurveDistanceData _closestCurveData;
        
        private int _closestPointIndex;
        private int _newPointIndex;
        private int _movingPointIndex;

        private bool _updateNewCurve;
        private AnchorPoint _oldAnchor;
        private ControlPoint _newControlStart;
        private ControlPoint _newControlEnd;
        private AnchorPoint _newAnchor;
        
        private bool _canRemove;

        private bool[] _constrainedAxis;
        private bool _snapping;
        private Vector3 _snapAmounts;

        private bool HasNewHotControl => _lastPointSelectedIndex != _pointControlIds.IndexOf(GUIUtility.hotControl);
        private bool HasHotControlNow => GUIUtility.hotControl != 0;
        private bool WasSelectingPoint => _lastPointSelectedIndex != -1;
        private bool HasShift => Event.current.shift;
        private bool HasControl => Event.current.control;
        private bool HasLeftMouse => Event.current.button == 0;
        
        
        private enum RailTools
        {
            MoveAdd,
            Split,
            Remove
        }
        private int _railToolsCount;
        private RailTools _currentRailTool = RailTools.MoveAdd;

        private void OnEnable()
        {
            _pathDataProp = serializedObject.FindProperty("pathData");
            
            LoadPointsAndCurves(_pathDataProp);

            _selectedPointIndex = -1;
            
            _currentRailTool = RailTools.MoveAdd;
            _railToolsCount = Enum.GetNames(typeof(RailTools)).Length;

            GUIUtility.hotControl = 0;
            _lastPointSelectedIndex = -1;
            _newPointIndex = -1;
            _closestPointIndex = -1;

            _canRemove = true;

            _constrainedAxis = new bool[3];

            _snapping = false;
        }

        private void OnDisable()
        {
            if (_pathDataProp == null)
                return;
            SavePointsAndCurves(_pathDataProp.objectReferenceValue);
        }

        public override void OnInspectorGUI()
        {
            Color originalColor = GUI.backgroundColor;
            //Color offsetColor = originalColor - new Color(0.1f, 0.1f, 0.1f, 0);
            Color selectedColor = Color.grey;
            
            GUIStyle style = new GUIStyle(GUI.skin.window);
            style.wordWrap = true;
            style.stretchHeight = false;
            style.padding = new RectOffset(3, 3, 3, 3);
            
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            Object before = _pathDataProp.objectReferenceValue;
            EditorGUILayout.PropertyField(_pathDataProp);
            if (EditorGUI.EndChangeCheck())
            {
                SavePointsAndCurves(before);
                LoadPointsAndCurves(_pathDataProp);
            }
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();

            if (GUILayout.Button("Create New Curve"))
            {
                CreateNewCurve();
            }
            
            EditorGUILayout.BeginVertical(style);

            if (_points == null || _points.Count == 0)
            {
                EditorGUILayout.LabelField("No Points");
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < _railToolsCount; i++)
                {
                    GUI.backgroundColor = _currentRailTool == (RailTools)i ? selectedColor : originalColor;
                    if (GUILayout.Button(((RailTools)i).ToString()))
                    {
                        _currentRailTool = (RailTools) i;
                        if (_currentRailTool == RailTools.Remove)
                            ClearSelected();
                        _updateScene = true;
                    }
                }
                GUI.backgroundColor = originalColor;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                SetSelectedPoint(EditorGUILayout.IntField("Selected Point Index", _selectedPointIndex));
                if (GUILayout.Button("Deselect", GUILayout.Width(64)))
                {
                    ClearSelected();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(GUI.skin.box);
                Point selectedPoint = GetSelectedPoint();
                if (selectedPoint != null)
                {
                    switch (selectedPoint)
                    {
                        case AnchorPoint anchor:
                        {
                            EditorGUILayout.LabelField("Anchor Point:");
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            
                            Vector3 position = EditorGUILayout.Vector3Field("Position", anchor.position);
                            if (anchor.position != position)
                            {
                                anchor.UpdatePosition(GetConstrainedPosition(anchor.position, position, _constrainedAxis));
                                _updateScene = true;
                            }

                            _selectedPointRotation = EditorGUILayout.Vector3Field("Rotation", _selectedPointRotation);
                            Quaternion rotation = Quaternion.Euler(_selectedPointRotation);
                            if (anchor.rotation != rotation)
                            {
                                anchor.SetRotation(rotation, true);
                                _updateScene = true;
                            }

                            EditorGUILayout.EndVertical();
                            
                            break;
                        }
                        case ControlPoint control:
                        {
                            EditorGUILayout.LabelField("Control Point:");
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            
                            Vector3 position = EditorGUILayout.Vector3Field("Position", control.position);
                            if (control.position != position)
                            {
                                control.UpdatePosition(GetConstrainedPosition(control.position, position, _constrainedAxis));
                                _updateScene = true;
                            }

                            float distance = EditorGUILayout.FloatField("Distance", control.distance);
                            if (distance != control.distance)
                            {
                                control.SetDistance(distance);
                                _updateScene = true;
                            }

                            bool flipped = EditorGUILayout.Toggle("Flipped", control.flipped);
                            if (flipped != control.flipped)
                            {
                                control.Flip(flipped);
                                _updateScene = true;
                            }

                            EditorGUILayout.EndVertical();
                            
                            break;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No Selected Point");
                }
                
                

                EditorGUILayout.EndVertical();
                
                
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical(style);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Move Constraints", EditorStyles.boldLabel);
            _constrainedAxis[0] = EditorGUILayout.Toggle("X", _constrainedAxis[0]);
            _constrainedAxis[1] = EditorGUILayout.Toggle("Y", _constrainedAxis[1]);
            _constrainedAxis[2] = EditorGUILayout.Toggle("Z", _constrainedAxis[2]);
            EditorGUILayout.EndVertical();
            
            //EditorGUILayout.BeginVertical(GUI.skin.box);
            //_snapping = EditorGUILayout.BeginToggleGroup("Snapping", _snapping);
            //_snapAmounts = EditorGUILayout.Vector3Field("Snap Amount", _snapAmounts);
            //EditorGUILayout.EndToggleGroup();
            //EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();

            if (_updateScene)
            {
                SceneView.RepaintAll();
                _updateScene = false;
            }
        }

        private void OnSceneGUI()
        {

            _closestPointIndex = -1;
            if (_movingPointIndex != -1)
            {
                _closestPointIndex = GetClosestAnchor(_points[_movingPointIndex].position, RailwayEditorSettings.Instance.AnchorSize/2f, _movingPointIndex);
            }
            
            
            switch (_currentRailTool)
            {
                case RailTools.MoveAdd:
                {
                    if (Event.current.rawType == EventType.Repaint)
                    {
                        foreach (var c in _curves)
                        {
                            DrawHandleLines(c);
                            DrawCurve(c);
                        }
                    }
                    
                    DrawPointHandles(true, true);
                    TryUpdateNewCurve();
                    TryCombiningAnchors(_closestPointIndex, _movingPointIndex);

                    if (HasNewHotControl && HasLeftMouse)
                    {
                        if (HasHotControlNow)
                        {
                            if (HasShift)
                            {
                                SetSelectedPoint(_pointControlIds.IndexOf(GUIUtility.hotControl), false);
                                _updateInspector = true;
                            } else if (HasControl && !WasSelectingPoint)
                            {
                                int pointIndex = _pointControlIds.IndexOf(GUIUtility.hotControl);
                                if (pointIndex != -1 && _points[pointIndex] is AnchorPoint anchor)
                                    _newPointIndex = AddCurveConnectedTo(anchor);

                                _updateInspector = true;
                            }
                        }
                        else
                        {
                            if (_updateNewCurve)
                            {
                                _oldAnchor = null;
                                _newControlStart = null;
                                _newControlEnd = null;
                                _newAnchor = null;
                                _updateNewCurve = false;
                            }
                        }
                    }

                    if (HasHotControlNow)
                    {
                        _movingPointIndex = _pointControlIds.IndexOf(GUIUtility.hotControl);
                        if (_movingPointIndex != -1 && _points[_movingPointIndex] is ControlPoint)
                            _movingPointIndex = -1;
                    }
                    else
                    {
                        _movingPointIndex = -1;
                    }
                    
                    break;
                }

                case RailTools.Split:
                {
                    if (Event.current.rawType == EventType.Repaint)
                    {
                        foreach (var c in _curves)
                        {
                            DrawCurve(c);
                        }
                        
                        DrawPointHandles(false, false);
                        
                        _closestCurveData = GetClosestCurve(Event.current.mousePosition);
                        if (_closestCurveData.curve != null)
                        {
                            Vector3 pos = _closestCurveData.curve.GetPosition(_closestCurveData.t);
                            Handles.color = RailwayEditorSettings.Instance.SplitLineColor;
                            Handles.DrawLine(_closestCurveData.rayPoint, pos, RailwayEditorSettings.Instance.SplitLineThickness);
                        }

                        SceneView.RepaintAll();
                    }
                    
                    if (HasLeftMouse)
                    {
                        if (HasShift && Event.current.rawType == EventType.MouseDown)
                        {
                            if (_closestCurveData.curve != null)
                            {
                                SplitCurve(_closestCurveData.curve, _closestCurveData.t);
                            }
                        }
                
                    }
                    
                    
                    break;
                }

                case RailTools.Remove:
                {
                    if (Event.current.rawType == EventType.Repaint)
                    {
                        foreach (var c in _curves)
                        {
                            DrawCurve(c);
                        }
                    }

                    DrawPointHandles(false, false);

                    if (!_canRemove && Event.current.rawType == EventType.MouseUp)
                        _canRemove = true;
                    
                    if (_canRemove && HasLeftMouse)
                    {
                        if (HasHotControlNow)
                        {
                            if (HasShift)
                            {

                                int pointIndex = _lastPointSelectedIndex;
                                if (pointIndex != -1 && _points[pointIndex] is AnchorPoint)
                                {
                                    if (_selectedPointIndex == pointIndex)
                                        ClearSelected();
                                    RemovePoint(_points[pointIndex]);
                                    _updateInspector = true;
                                    _canRemove = false;
                                    GUIUtility.hotControl = 0;
                                }
                                
                            }
                        }
                    }
                    
                    break;
                }
            }

            if (_updateInspector)
            {
                Repaint();
                _updateInspector = false;
            }

            _lastPointSelectedIndex = _pointControlIds.IndexOf(GUIUtility.hotControl);
        }

        private void SavePointsAndCurves(Object obj)
        {
            if (obj == null)
                return;

            SerializedObject railPathObj = new SerializedObject(obj);
            
            if (railPathObj == null)
                return;
            
            railPathObj.Update();

            //SerializedProperty railPathPointsProp = railPathProp.FindPropertyRelative("pathPoints");
            
            //SerializedProperty railPathPointsProp = railPathObj.GetIterator();
            //do
            //{
            //    if (railPathPointsProp.name == "pathPoints")
            //    {
            //        Debug.Log(railPathPointsProp.name);
            //        break;
            //    }
            //} while (railPathPointsProp.Next(true));
            
            
            SerializedProperty railPathPointsProp = railPathObj.FindProperty("pathPoints");
            SerializedProperty railPathCurvesProp = railPathObj.FindProperty("pathCurves");

            if (railPathPointsProp == null)
            {
                Debug.Log("railPathPointsProp == null");
                railPathPointsProp.managedReferenceValue = new List<PathPoint>();
            }
                
            
            railPathPointsProp.ClearArray();

            for (int i = 0; i < _points.Count; i++)
            {
                railPathPointsProp.InsertArrayElementAtIndex(i);
                SerializedProperty pointProp = railPathPointsProp.GetArrayElementAtIndex(i);
                SerializedProperty pointPointTypeProp = pointProp.FindPropertyRelative("pointType");
                SerializedProperty pointPositionProp = pointProp.FindPropertyRelative("position");
                SerializedProperty pointRotationProp = pointProp.FindPropertyRelative("rotation");
                SerializedProperty pointFlippedProp = pointProp.FindPropertyRelative("flipped");
                SerializedProperty pointConnectedPointsProp = pointProp.FindPropertyRelative("connectedPoints");

                switch (_points[i])
                {
                    case AnchorPoint p:
                    {
                        pointPointTypeProp.enumValueIndex = 0;
                            pointPositionProp.vector3Value = p.position;
                            pointRotationProp.quaternionValue = p.rotation;
                            pointConnectedPointsProp.ClearArray();
                            for (int j = 0; j < p.controlPoints.Count; j++)
                            {
                                pointConnectedPointsProp.InsertArrayElementAtIndex(j);
                                SerializedProperty pointConnectedPointsIndexProp = pointConnectedPointsProp.GetArrayElementAtIndex(0);
                                pointConnectedPointsIndexProp.intValue = _points.IndexOf(p.controlPoints[j]);
                            }

                            break;
                    }
                    case ControlPoint p:
                    {
                        pointPointTypeProp.enumValueIndex = 1;
                            pointPositionProp.vector3Value = p.position;
                            pointFlippedProp.boolValue = p.flipped;
                            pointConnectedPointsProp.ClearArray();
                            pointConnectedPointsProp.InsertArrayElementAtIndex(0);
                            SerializedProperty pointConnectedPointsIndexProp = pointConnectedPointsProp.GetArrayElementAtIndex(0);
                            pointConnectedPointsIndexProp.intValue = _points.IndexOf(p.anchorPoint);
                            break;
                    }
                }
            }

            railPathCurvesProp.ClearArray();
            
            for (int i = 0; i < _curves.Count; i++)
            {
                railPathCurvesProp.InsertArrayElementAtIndex(i);
                SerializedProperty curveProp = railPathCurvesProp.GetArrayElementAtIndex(i);
                SerializedProperty curveStartProp = curveProp.FindPropertyRelative("start");
                SerializedProperty curveControlStartProp = curveProp.FindPropertyRelative("controlStart");
                SerializedProperty curveControlEndProp = curveProp.FindPropertyRelative("controlEnd");
                SerializedProperty curveEndProp = curveProp.FindPropertyRelative("end");

                curveStartProp.intValue = _points.IndexOf(_curves[i].start);
                curveControlStartProp.intValue = _points.IndexOf(_curves[i].controlStart);
                curveControlEndProp.intValue = _points.IndexOf(_curves[i].controlEnd);
                curveEndProp.intValue = _points.IndexOf(_curves[i].end);
            }

            railPathObj.ApplyModifiedProperties();
        }

        private void LoadPointsAndCurves(SerializedProperty prop)
        {
            if (prop == null)
                return;

            RailPathData railPathData = prop.objectReferenceValue as RailPathData;
            if (railPathData == null)
                return;

            _points = railPathData.GetPoints();
            _curves = railPathData.GetCurves(_points);
            _pointControlIds = new List<int>(_points.Count);
            for (int i = 0; i < _points.Count; i++)
            {
                _pointControlIds.Add(-1);
            }
        }
        
        
        private void AddPoint_Internal(Point point)
        {
            if (_points.Contains(point))
                return;
            _points.Add(point);
            _pointControlIds.Add(-1);
        }

        private void RemovePoint_Internal(Point point)
        {
            int index = _points.IndexOf(point);
            _points.Remove(point);
            _pointControlIds.RemoveAt(index);
        }

        private void AddCurve_Internal(BezierCurve curve)
        {
            AddPoint_Internal(curve.start);
            AddPoint_Internal(curve.controlStart);
            AddPoint_Internal(curve.controlEnd);
            AddPoint_Internal(curve.end);
            _curves.Add(curve);
        }

        private void RemoveCurve_Internal(BezierCurve curve)
        {
            curve.start = null;
            curve.controlStart = null;
            curve.controlEnd = null;
            curve.end = null;
            _curves.Remove(curve);
        }

        private void AddPoint(Point point)
        {
            AddPoint_Internal(point);
        }

        private void RemovePoint(Point point, bool removeSingleAnchors = true)
        {
            var curveDatas = CurveUtility.GetCurvesWithPoint(_curves, point);

            foreach (var data in curveDatas)
            {
                RemovePoint_Internal(data.curve.controlStart);
                RemovePoint_Internal(data.curve.controlEnd);

                if (removeSingleAnchors)
                {
                    BezierCurve.Sides otherSide = CurveUtility.Opposite(data.side);
                    Point otherAnchor = data.curve.GetAnchor(otherSide);
                    data.curve.ClearAnchorOnSide(otherSide);
                    if (CurveUtility.GetCurvesCountWithPoint(_curves, otherAnchor) <= 0)
                    {
                        RemovePoint_Internal(otherAnchor);
                    }
                }
                RemoveCurve_Internal(data.curve);
            }
            
            RemovePoint_Internal(point);
        }

        private void RemoveCurve(BezierCurve curve, bool removeSingleAnchors = true)
        {
            RemovePoint_Internal(curve.controlStart);
            RemovePoint_Internal(curve.controlEnd);

            if (removeSingleAnchors)
            {
                Point startAnchor = curve.start;
                Point endAnchor = curve.end;
                curve.start = null;
                curve.end = null;

                if (CurveUtility.GetCurvesCountWithPoint(_curves, startAnchor) <= 0)
                {
                    RemovePoint_Internal(startAnchor);
                }

                if (CurveUtility.GetCurvesCountWithPoint(_curves, endAnchor) <= 0)
                {
                    RemovePoint_Internal(endAnchor);
                }
            }

            RemoveCurve_Internal(curve);
        }

        private int AddCurveConnectedTo(AnchorPoint anchor)
        {
            _oldAnchor = anchor;
            _newAnchor = new AnchorPoint(anchor.position, anchor.rotation);
            _newControlStart = new ControlPoint(anchor, 1f, false);
            _newControlEnd = new ControlPoint(_newAnchor, 1f, true);
            BezierCurve curve = new BezierCurve(_oldAnchor, _newControlStart, _newControlEnd, _newAnchor);
            AddCurve_Internal(curve);
            _updateNewCurve = true;
            return _points.Count - 1;
        }

        private void SplitCurve(BezierCurve curve, float t)
        {
            Vector3 E = Vector3.Lerp(curve.start.position, curve.controlStart.position, t);
            Vector3 F = Vector3.Lerp(curve.controlStart.position, curve.controlEnd.position, t);
            Vector3 G = Vector3.Lerp(curve.controlEnd.position, curve.end.position, t);

            Vector3 H = Vector3.Lerp(E, F, t);
            Vector3 J = Vector3.Lerp(F, G, t);

            Vector3 K = Vector3.Lerp(H, J, t);

            AnchorPoint middleAnchor = new AnchorPoint(K, curve.GetRotation(t));

            ControlPoint controlLow = new ControlPoint(middleAnchor, H);
            ControlPoint controlHigh = new ControlPoint(middleAnchor, J);
            controlLow.Flip();

            BezierCurve curveLow = new BezierCurve(curve.start, curve.controlStart, controlLow, middleAnchor);
            BezierCurve curveHigh = new BezierCurve(middleAnchor, controlHigh, curve.controlEnd, curve.end);
            
            curve.controlStart.UpdatePosition(E);
            curve.controlEnd.UpdatePosition(G);
            
            AddCurve_Internal(curveLow);
            AddCurve_Internal(curveHigh);
            RemoveCurve_Internal(curve);
        }

        private void Combine(AnchorPoint anchorA, AnchorPoint anchorB)
        {
            // Combine B with A, so anchorA is left
            
            // Move anchorB to the position of anchorA
            anchorB.UpdatePosition(anchorA.position);
            
            // Get the curves that contain the anchors
            var anchorACurves = CurveUtility.GetCurvesWithPoint(_curves, anchorA);
            var anchorBCurves = CurveUtility.GetCurvesWithPoint(_curves, anchorB);
            var sharedCurves = CurveUtility.GetCurvesWithPoint(anchorBCurves, anchorA);

            // Remove the shared curves
            for (int i = sharedCurves.Count - 1; i >= 0; i--)
            {
                RemoveCurve(sharedCurves[i].curve, false);
            }

            // Check if the anchors are facing the same direction (used for flipping ControlPoints)
            bool sameDirection = Quaternion.Dot(anchorA.rotation, anchorB.rotation) > 0.5f;
            
            // change the anchor and control of the curves with anchorB to anchorA
            foreach (var data in anchorBCurves)
            {
                if (data.curve.IsClear())
                    continue;
                data.curve.SetAnchor(anchorA, data.side);
                ControlPoint control = data.curve.GetControl(data.side);
                control.anchorPoint.RemoveControlPoint(control);
                anchorA.AddControlPoint(control);
                if (!sameDirection) control.Flip();
                control.UpdatePosition();
            }
            
            // Remove anchorB
            RemovePoint_Internal(anchorB);
        }

        private void CreateNewCurve()
        {
            AnchorPoint start = new AnchorPoint(new Vector3(0, 0, 0), Quaternion.identity);
            AnchorPoint end = new AnchorPoint(new Vector3(4, 0, 1), Quaternion.Euler(0, 90, 0));
            ControlPoint controlStart = new ControlPoint(start, 1f, false);
            ControlPoint controlEnd = new ControlPoint(end, 1f, true);

            BezierCurve curve = new BezierCurve(start, controlStart, controlEnd, end);
            
            AddCurve_Internal(curve);

            _updateScene = true;
            _updateInspector = true;
        }

        private Point GetSelectedPoint()
        {
            if (_selectedPointIndex < 0 || _selectedPointIndex >= _points.Count)
                return null;
            return _points[_selectedPointIndex];
        }

        private void SetSelectedPoint(int index, bool allowMinusOne = true)
        {
            if (_selectedPointIndex == index)
                return;

            if (!allowMinusOne && index == -1)
                return;
                
            
            _selectedPointIndex = index;
            if (_selectedPointIndex < -1)
                _selectedPointIndex = -1;

            _updateInspector = true;
            _updateScene = true;

            if (_selectedPointIndex < 0 || _selectedPointIndex >= _points.Count)
                return;

            Point p = GetSelectedPoint();
            if (p is AnchorPoint ap)
            {
                _selectedPointRotation = ap.rotation.eulerAngles;
            }
        }
        
        private void ClearSelected()
        {
            SetSelectedPoint(-1);
        }

        private void DrawCurve(BezierCurve curve)
        {
            Handles.DrawBezier(
                curve.start.position, 
                curve.end.position, 
                curve.controlStart.position, 
                curve.controlEnd.position, 
                RailwayEditorSettings.Instance.RailLineColor, 
                null, 
                RailwayEditorSettings.Instance.RailLineThickness);

            if (!RailwayEditorSettings.Instance.RailNormalShow) 
                return;
            
            Handles.color = RailwayEditorSettings.Instance.RailNormalColor;
            int normalCount = RailwayEditorSettings.Instance.RailNormalCount;
            float normalLength = RailwayEditorSettings.Instance.RailNormalLength;
            float normalThickness = RailwayEditorSettings.Instance.RailNormalThickness;
            float delta = 1f / (float) (normalCount-1);
            for (int i = 0; i < normalCount; i++)
            {
                float t = ((float) i) * delta;
                Vector3 position = curve.GetPosition(t);
                Vector3 normal = curve.GetNormal(t);
                normal *= normalLength;
                Handles.DrawLine(position, position + normal, normalThickness);
            }
        }

        private void DrawHandleLines(BezierCurve curve)
        {
            if (RailwayEditorSettings.Instance.ControlLineShow)
            {
                Handles.color = RailwayEditorSettings.Instance.ControlLineColor;
                
                Vector3 a = curve.start.position;
                Vector3 b = curve.controlStart.position;
                Handles.DrawLine(a, b, RailwayEditorSettings.Instance.ControlLineThickness);
                
                a = curve.end.position;
                b = curve.controlEnd.position;
                Handles.DrawLine(a, b, RailwayEditorSettings.Instance.ControlLineThickness);
            }
            
            if (RailwayEditorSettings.Instance.InterControlLineShow)
            {
                Handles.color = RailwayEditorSettings.Instance.InterControlLineColor;
                
                Vector3 a = curve.controlStart.position;
                Vector3 b = curve.controlEnd.position;
                Handles.DrawLine(a, b, RailwayEditorSettings.Instance.InterControlLineThickness);
            }
        }

        private void DrawPointHandles(bool drawControls = false, bool updatePositions = true)
        {
            Vector3 handlePos;

            for (int i = 0; i < _points.Count; i++)
            {
                switch (_points[i])
                {
                    case AnchorPoint p:
                    {
                        Handles.color = _closestPointIndex == i ? 
                            RailwayEditorSettings.Instance.AnchorHighlightColor : 
                            RailwayEditorSettings.Instance.AnchorColor;
                        
                        if (_selectedPointIndex == i)
                        {
                            handlePos = Handles.PositionHandle(p.position, Tools.pivotRotation == PivotRotation.Local ? p.rotation : Quaternion.identity);
                            _pointControlIds[i] = -1;
                        }
                        else
                        {
                            _pointControlIds[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(_pointControlIds[i], p.position, p.rotation, RailwayEditorSettings.Instance.AnchorSize, _snapping ? _snapAmounts : Vector3.zero,
                                Handles.SphereHandleCap);
                            if (_newPointIndex == i)
                            {
                                GUIUtility.hotControl = _pointControlIds[i];
                                _newPointIndex = -1;
                            }
                        }

                        if (updatePositions && p.position != handlePos)
                        {
                            p.UpdatePosition(GetConstrainedPosition(p.position, handlePos, _constrainedAxis));
                            //selectedPointRot = p.rotation.eulerAngles;
                            _updateInspector = true;
                        }
                        break;
                    }
                    case ControlPoint p:
                    {
                        if (!drawControls)
                        {
                            _pointControlIds[i] = -1;
                            continue;
                        }
                        
                        Handles.color = RailwayEditorSettings.Instance.ControlColor;
                        if (_selectedPointIndex == i)
                        {
                            handlePos = Handles.PositionHandle(p.position, Quaternion.identity);
                            _pointControlIds[i] = -1;
                        }
                        else
                        {
                            _pointControlIds[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(_pointControlIds[i], p.position, Quaternion.identity, RailwayEditorSettings.Instance.ControlSize, _snapping ? _snapAmounts : Vector3.zero,
                                Handles.SphereHandleCap);
                        }

                        if (updatePositions && p.position != handlePos)
                        {
                            p.UpdatePosition(GetConstrainedPosition(p.position, handlePos, _constrainedAxis));
                            //selectedPointRot = p.anchorPoint.rotation.eulerAngles;
                            _updateInspector = true;
                        }
                        
                        break;
                    }
                }
            }
        }

        private void TryUpdateNewCurve()
        {
            if (!_updateNewCurve)
                return;
            Vector3 originForward = _oldAnchor.rotation * Vector3.forward;
            Vector3 dir = (_newAnchor.position - _oldAnchor.position).normalized;
            float dot = Vector3.Dot(originForward, dir);
            bool sameDirection = dot > 0;
            _newControlStart.Flip(!sameDirection);
            _newControlEnd.Flip(sameDirection);
        }

        private void TryCombiningAnchors(int indexA, int indexB)
        {
            if (GUIUtility.hotControl != 0)
                return;

            if (indexA == -1 || indexB == -1)
                return;
            
            AnchorPoint a = _points[indexA] as AnchorPoint;
            AnchorPoint b = _points[indexB] as AnchorPoint;
            Combine(a, b);
        }

        private Vector3 GetConstrainedPosition(Vector3 oldPos, Vector3 newPos, bool[] constraints)
        {
            Vector3 ret = oldPos;
            for (int i = 0; i < 3; i++)
            {
                if (!constraints[i])
                    ret[i] = newPos[i];
            }

            return ret;
        }

        private int GetClosestAnchor(Vector3 pos, float minDistance = Mathf.Infinity, int ignoreIndex = -1)
        {
            float closestDist = Mathf.Infinity;
            int closestIndex = -1;
            for (int i = 0; i < _points.Count; i++)
            {
                if (i == ignoreIndex)
                    continue;
                switch (_points[i])
                {
                    case ControlPoint:
                        break;
                    case AnchorPoint ap:
                    {
                        float dist = Vector3.Distance(ap.position, pos);
                        if (dist > minDistance)
                            break;
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestIndex = i;
                        }
                        break;
                    }
                }
            }

            return closestIndex;
        }
        
        private struct CurveDistanceData
        {
            public BezierCurve curve;
            public float distance;
            public float t;
            public Vector3 rayPoint;

            public CurveDistanceData(BezierCurve c, float d, float t, Vector3 rp)
            {
                curve = c;
                distance = d;
                this.t = t;
                rayPoint = rp;
            }
        }

        private CurveDistanceData GetClosestCurve(Vector2 screenPos)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(screenPos);
            float originalDelta = 1f / (float)RailwayEditorSettings.Instance.SplitDistantSearchCount;
            BezierCurve bestCurve = null;
            float bestT = 0;
            float bestDist = Mathf.Infinity;
            Vector3 bestRayPoint = Vector3.zero;
            foreach (var c in _curves)
            {
                float delta = originalDelta;
                float cBestT = 0;
                float cBestDist = Mathf.Infinity;
                Vector3 cBestRayPoint = Vector3.zero;
                for (float t = 0f; t <= 1f; t+=delta)
                {
                    Vector3 cPos = c.GetPosition(t);
                    Vector3 rPos = GetClosestPointOnRay(ray, cPos);
                    float dist = (cPos - rPos).magnitude;
                    if (dist < cBestDist)
                    {
                        cBestDist = dist;
                        cBestT = t;
                        cBestRayPoint = rPos;
                    }
                }

                cBestT -= delta * 0.5f;
                float oldBest = delta;
                delta *= delta;
                cBestT = Mathf.Clamp01(cBestT);
                
                cBestDist = Mathf.Infinity;
                for (float t = cBestT; t <= cBestT+oldBest; t += delta)
                {
                    if (t > 1)
                        break;
                    Vector3 cPos = c.GetPosition(t);
                    Vector3 rPos = GetClosestPointOnRay(ray, cPos);
                    float dist = (cPos - rPos).magnitude;
                    if (dist < cBestDist)
                    {
                        cBestDist = dist;
                        cBestT = t;
                        cBestRayPoint = rPos;
                    }
                }
                
                cBestT = Mathf.Clamp01(cBestT);

                if (cBestDist < bestDist)
                {
                    bestCurve = c;
                    bestDist = cBestDist;
                    bestT = cBestT;
                    bestRayPoint = cBestRayPoint;
                }
            }

            return new CurveDistanceData(bestCurve, bestDist, bestT, bestRayPoint);
        }

        private Vector3 GetClosestPointOnRay(Ray r, Vector3 p)
        {
            return r.origin + Vector3.Project(p - r.origin, r.direction);
        }







        /*
        private SerializedProperty railPathProp;
        private SerializedObject railPathObj;

        private List<Point> points;
        private List<BezierCurve> curves;

        private int selectedPoint = -1;
        private Vector3 selectedPointRot;
        
        private int closestPoint;

        private int movingPoint = -1;

        //private int pointControlIDIndex;
        private List<int> pointControlIDs;
        private int prevHotControl;

        private bool prevMousePressed;

        private int newPointIndex;
        private bool updateNewCurve;
        private AnchorPoint oldAnchor;
        private ControlPoint newControlStart;
        private ControlPoint newControlEnd;
        private AnchorPoint newAnchor;

        private CurveDistanceData closestCurve;
        
        private bool updateInspector;

        private Tool _prevTool;

        private enum RailTools
        {
            MoveAdd,
            Split,
            Remove
        }

        private int railToolsCount;

        private RailTools currentRailTool = RailTools.MoveAdd;

        private void OnEnable()
        {
            railPathProp = serializedObject.FindProperty("pathData");
            LoadPointsAndCurves();

            _prevTool = Tools.current;
            Tools.current = Tool.Custom;

            currentRailTool = RailTools.MoveAdd;
            railToolsCount = Enum.GetNames(typeof(RailTools)).Length;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            bool updateScene = false;
            
            Color originalColor = GUI.backgroundColor;
            //Color offsetColor = originalColor - new Color(0.1f, 0.1f, 0.1f, 0);
            Color selectedColor = Color.grey;
            
            GUIStyle style = new GUIStyle(GUI.skin.window);
            style.wordWrap = true;
            style.stretchHeight = false;
            style.padding = new RectOffset(3, 3, 3, 3);


            var prev = railPathProp.objectReferenceValue;
            EditorGUILayout.PropertyField(railPathProp);
            if (prev != railPathProp.objectReferenceValue)
            {
                SavePathsAndCurves();
                LoadPointsAndCurves();
            }
            
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Add New Curve"))
            {
                AddNewCurve();
            }
            
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < railToolsCount; i++)
            {
                GUI.backgroundColor = ((int) currentRailTool) == i ? selectedColor : originalColor;
                if (GUILayout.Button(((RailTools)i).ToString()))
                {
                    currentRailTool = (RailTools) i;
                    updateScene = true;
                }
            }
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();
            switch (currentRailTool)
            {
                case RailTools.MoveAdd:
                    EditorGUILayout.LabelField("Shift-Click Point : Select Point");
                    EditorGUILayout.LabelField("Ctrl-Click-Drag Anchor : New Point");
                    break;
                case RailTools.Split:
                    EditorGUILayout.LabelField("Shift-Click Curve : Split Curve");
                    break;
                case RailTools.Remove:
                    EditorGUILayout.LabelField("Shift-Click Anchor : Remove Anchor");
                    break;
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            int prevSelectedPoint = selectedPoint;
            selectedPoint = EditorGUILayout.IntField("Current Point Index", selectedPoint);
            if (GUILayout.Button("Deselect", GUILayout.Width(64f)))
            {
                selectedPoint = -1;
            }
            if (prevSelectedPoint != selectedPoint)
            {
                if (selectedPoint >= 0 && selectedPoint < points.Count)
                {
                    if (points[selectedPoint].GetType() == typeof(AnchorPoint))
                        selectedPointRot = ((AnchorPoint) points[selectedPoint]).rotation.eulerAngles;
                }

                updateScene = true;
                // update current curve
            }
            EditorGUILayout.EndHorizontal();

            Vector3 prevV3;
            Vector3 newV3;
            float prevFloat;
            float newFloat;
            bool prevBool;
            bool newBool;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (selectedPoint >= 0 && selectedPoint < points.Count)
            {
                switch (points[selectedPoint])
                {
                    case AnchorPoint p:
                    {
                        EditorGUILayout.LabelField("Type: Anchor");
                        
                        prevV3 = p.position;
                        newV3 = EditorGUILayout.Vector3Field("Position", p.position);
                        if (prevV3 != newV3)
                        {
                            p.UpdatePosition(newV3);
                            updateScene = true;
                        }

                        prevV3 = selectedPointRot;
                        newV3 = EditorGUILayout.Vector3Field("Rotation", selectedPointRot);
                        if (prevV3 != newV3)
                        {
                            selectedPointRot = newV3;
                            p.rotation = Quaternion.Euler(newV3);
                            p.UpdateControls();
                            updateScene = true;
                        }
                        
                        break;
                    }
                    
                    case ControlPoint p:
                    {
                        EditorGUILayout.LabelField("Type: Control");
                        prevV3 = p.position;
                        newV3 = EditorGUILayout.Vector3Field("Position", p.position);
                        if (prevV3 != newV3)
                        {
                            p.UpdatePosition(newV3);
                            updateScene = true;
                        }
                        
                        prevFloat = p.distance;
                        newFloat = EditorGUILayout.FloatField("Distance", p.distance);
                        if (prevFloat != newFloat)
                        {
                            p.distance = newFloat;
                            p.UpdatePosition();
                            updateScene = true;
                        }

                        prevBool = p.flipped;
                        newBool = EditorGUILayout.Toggle("Flipped", p.flipped);
                        if (prevBool != newBool)
                        {
                            p.flipped = newBool;
                            p.UpdatePosition();
                            updateScene = true;
                        }
                        break;
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Invalid Selected Point");
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            

            if (updateScene)
            {
                SceneView.RepaintAll();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void LoadPointsAndCurves()
        {
            if (railPathProp == null)
            {
                points = new List<Point>();
                curves = new List<BezierCurve>();
                pointControlIDs = new List<int>();
                return;
            }
                
            railPathObj = new SerializedObject(railPathProp.objectReferenceValue);

            if (railPathProp == null)
            {
                Debug.Log("railPathProp == null");
                return;
            }

            RailPathData data = (RailPathData) railPathObj.targetObject;
            points = data.GetPoints();
            curves = data.GetCurves(points);
            
            //Debug.Log(points.Count);
            //Debug.Log(curves.Count);

            pointControlIDs = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                pointControlIDs.Add(-1);
            }

            newPointIndex = -1;
        }

        private void OnSceneGUI()
        {
            
            
            
            updateInspector = false;
            closestPoint = -1;

            switch (currentRailTool)
            {
                case RailTools.MoveAdd:
                {
                    MoveAddTool_Draw();
                    MoveAddTool_HandleInput();
                    break;
                }
                case RailTools.Split:
                {
                    SplitTool_Draw();
                    SplitTool_HandleInput();
                    break;
                }
                case RailTools.Remove:
                {
                    RemoveTool_Draw();
                    RemoveTool_HandleInput();
                    break;
                }
            }
            
            prevHotControl = GUIUtility.hotControl;
            
            
            if (updateInspector)
            {
                Repaint();
            }
            
        }

        private void AddPoint_Internal(Point p)
        {
            if (points.Contains(p))
                return;
            
            points.Add(p);
            pointControlIDs.Add(-1);
        }

        private void RemovePoint_Internal(Point p)
        {
            if (p == null)
                return;
            if (!points.Contains(p))
                return;

            
            
            if (p is AnchorPoint a)
            {
                List<CurveUtility.CurvePointData> data = CurveUtility.GetCurvesWithPoint(curves, p);
                for (int i = data.Count - 1; i >= 0; i--)
                {
                    RemoveCurve_Internal(data[i].curve);
                }
                
                for (int i = a.controlPoints.Count -1; i >= 0; i--)
                {
                    RemovePoint_Internal(a.controlPoints[i]);
                }
                points.Remove(p);
                pointControlIDs.RemoveAt(0);
            } else if (p is ControlPoint c)
            {
                c.anchorPoint.RemoveControlPoint(c);
                points.Remove(p);
                pointControlIDs.RemoveAt(0);
            }
        }

        // Handles add the curve and points to respective lists
        private void AddCurve_Internal(BezierCurve c)
        {
            if (curves.Contains(c))
                return;
            
            AddPoint_Internal(c.start);
            AddPoint_Internal(c.controlStart);
            AddPoint_Internal(c.controlEnd);
            AddPoint_Internal(c.end);
            curves.Add(c);
        }
        
        // Handles removing the curve and points to respective lists
        private void RemoveCurve_Internal(BezierCurve c, bool removeLonely = true)
        {
            if (c == null)
                return;
            if (!curves.Contains(c))
                return;
            
            

            if (removeLonely)
            {
                Point p = c.start;
                c.start = null;
                if (CurveUtility.GetCurvesCountWithPoint(curves, p) == 0)
                    RemovePoint_Internal(p);

                p = c.end;
                c.end = null;
                if (CurveUtility.GetCurvesCountWithPoint(curves, p) == 0)
                    RemovePoint_Internal(p);
            }
            else
            {
                RemovePoint_Internal(c.controlStart);
                RemovePoint_Internal(c.controlEnd);
            }

            curves.Remove(c);
        }

        private void RemovePointAtIndex_Internal(int index)
        {
            if (index < 0 || index >= points.Count)
            {
                Debug.LogWarning($"Trying to remove point at {index}, points Count = {points.Count}");
                return;
            }
            points.RemoveAt(index);
            pointControlIDs.RemoveAt(0);
        }

        private void MoveAddTool_Draw()
        {
            Vector3 handlePos;

            if (movingPoint != -1)
            {
                closestPoint = GetClosestAnchor(points[movingPoint].position, RailwayEditorSettings.Instance.AnchorSize/2f, movingPoint);
            }
            
            for (int i = 0; i < points.Count; i++)
            {
                switch (points[i])
                {
                    case AnchorPoint p:
                    {
                        Handles.color = closestPoint == i ? 
                            RailwayEditorSettings.Instance.AnchorHighlightColor : 
                            RailwayEditorSettings.Instance.AnchorColor;
                        
                        if (selectedPoint == i)
                        {
                            handlePos = Handles.PositionHandle(p.position, Tools.pivotRotation == PivotRotation.Local ? p.rotation : Quaternion.identity);
                            pointControlIDs[i] = -1;
                        }
                        else
                        {
                            pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(pointControlIDs[i], p.position, p.rotation, RailwayEditorSettings.Instance.AnchorSize, Vector3.zero,
                                Handles.SphereHandleCap);
                            if (newPointIndex == i)
                            {
                                GUIUtility.hotControl = pointControlIDs[i];
                                newPointIndex = -1;
                            }
                        }

                        if (p.position != handlePos)
                        {
                            p.UpdatePosition(handlePos);
                            selectedPointRot = p.rotation.eulerAngles;
                            updateInspector = true;
                        }
                        break;
                    }
                    case ControlPoint p:
                    {
                        Handles.color = RailwayEditorSettings.Instance.ControlColor;
                        if (selectedPoint == i)
                        {
                            handlePos = Handles.PositionHandle(p.position, Quaternion.identity);
                            pointControlIDs[i] = -1;
                        }
                        else
                        {
                            pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(pointControlIDs[i], p.position, Quaternion.identity, RailwayEditorSettings.Instance.ControlSize, Vector3.zero,
                                Handles.SphereHandleCap);
                        }

                        if (p.position != handlePos)
                        {
                            p.UpdatePosition(handlePos);
                            selectedPointRot = p.anchorPoint.rotation.eulerAngles;
                            updateInspector = true;
                        }
                        break;
                    }
                }
            }
            
            if (updateNewCurve)
            {
                Vector3 originForward = oldAnchor.rotation * Vector3.forward;
                Vector3 dir = (newAnchor.position - oldAnchor.position).normalized;
                float dot = Vector3.Dot(originForward, dir);
                bool sameDirection = dot > 0;
                newControlStart.flipped = !sameDirection;
                newControlEnd.flipped = sameDirection;
                newControlStart.UpdatePosition();
                newControlEnd.UpdatePosition();
            }

            float normalDelta = 1f / ((float)RailwayEditorSettings.Instance.RailNormalCount);
            for (int i = 0; i < curves.Count; i++)
            {
                BezierCurve c = curves[i];
                
                if (RailwayEditorSettings.Instance.ControlLineShow)
                {
                    Handles.color = RailwayEditorSettings.Instance.ControlLineColor;
                    Handles.DrawLine(c.start.position, c.controlStart.position, RailwayEditorSettings.Instance.ControlLineThickness);
                    Handles.DrawLine(c.end.position, c.controlEnd.position, RailwayEditorSettings.Instance.ControlLineThickness);
                }

                if (RailwayEditorSettings.Instance.InterControlLineShow)
                {
                    Handles.color = RailwayEditorSettings.Instance.InterControlLineColor;
                    Handles.DrawLine(c.controlStart.position, c.controlEnd.position, RailwayEditorSettings.Instance.InterControlLineThickness);
                }

                Handles.DrawBezier(
                    c.start.position,
                    c.end.position,
                    c.controlStart.position,
                    c.controlEnd.position,
                    RailwayEditorSettings.Instance.RailLineColor, null, RailwayEditorSettings.Instance.RailLineThickness);

                if (!RailwayEditorSettings.Instance.RailNormalShow) 
                    continue;
                
                Handles.color = RailwayEditorSettings.Instance.RailNormalColor;
                for (float j = 0f; j <= 1.00001f; j += normalDelta)
                {
                    Vector3 normal = c.GetNormal(j);
                    Vector3 pointPos = c.GetPosition(j);

                    Handles.DrawLine(pointPos, pointPos + RailwayEditorSettings.Instance.RailNormalLength * normal,
                        RailwayEditorSettings.Instance.RailNormalThickness);
                }
            }
            
            if (movingPoint != -1 && closestPoint != -1 && GUIUtility.hotControl == 0)
            {
                CombinePoints((AnchorPoint)points[closestPoint], (AnchorPoint)points[movingPoint]);
            }
        }
        private void MoveAddTool_HandleInput()
        {
            if (prevHotControl != GUIUtility.hotControl && Event.current.button == 0)
            {
                if (GUIUtility.hotControl != 0)
                {
                    if (Event.current.shift)
                    {
                        
                        selectedPoint = pointControlIDs.IndexOf(GUIUtility.hotControl);
                        if (selectedPoint != -1 && points[selectedPoint].GetType() == typeof(AnchorPoint)) 
                            selectedPointRot = ((AnchorPoint) points[selectedPoint]).rotation.eulerAngles;
                        //prevHotControl = GUIUtility.hotControl;

                        updateInspector = true;
                    }

                    if (Event.current.control && prevHotControl == 0)
                    {
                        
                        int point = pointControlIDs.IndexOf(GUIUtility.hotControl);
                        if (point != -1 && points[point].GetType() == typeof(AnchorPoint)) 
                            AddCurveConnectedTo(point);
                        //prevHotControl = GUIUtility.hotControl;
                        
                        updateInspector = true;

                        
                    }
                }
                else
                {
                    if (updateNewCurve)
                    {
                        oldAnchor = null;
                        newControlStart = null;
                        newControlEnd = null;
                        newAnchor = null;
                        updateNewCurve = false;
                    }
                }
            }

            if (GUIUtility.hotControl != 0)
            {
                movingPoint = pointControlIDs.IndexOf(GUIUtility.hotControl);
                if (movingPoint != -1 && points[movingPoint] is ControlPoint)
                    movingPoint = -1;
            }
            else
                movingPoint = -1;
        }

        private void SplitTool_Draw()
        {
            if (Event.current.type == EventType.Repaint){
                Handles.color = RailwayEditorSettings.Instance.AnchorColor;
                for (int i = 0; i < points.Count; i++)
                {
                    switch (points[i])
                    {
                        case ControlPoint:
                            break;
                        case AnchorPoint p:
                        {
                            Handles.FreeMoveHandle(p.position, Quaternion.identity,
                                RailwayEditorSettings.Instance.AnchorSize, Vector3.zero, Handles.SphereHandleCap);
                            break;
                        }
                    }
                }
    
                float normalDelta = 1f / ((float)RailwayEditorSettings.Instance.RailNormalCount);
                for (int i = 0; i < curves.Count; i++)
                {
                    BezierCurve c = curves[i];
                    
                    Handles.DrawBezier(
                        c.start.position, 
                        c.end.position, 
                        c.controlStart.position, 
                        c.controlEnd.position, 
                        RailwayEditorSettings.Instance.RailLineColor, 
                        null, 
                        RailwayEditorSettings.Instance.RailLineThickness);
    
                    if (!RailwayEditorSettings.Instance.RailNormalShow)
                        continue;
    
                    Handles.color = RailwayEditorSettings.Instance.RailNormalColor;
                    for (float j = 0f; j <= 1.00001f; j += normalDelta)
                    {
                        Vector3 normal = c.GetNormal(j);
                        Vector3 pointPos = c.GetPosition(j);
    
                        Handles.DrawLine(pointPos, pointPos + RailwayEditorSettings.Instance.RailNormalLength * normal,
                            RailwayEditorSettings.Instance.RailNormalThickness);
                    }
                }
                 
                closestCurve = GetClosestCurve(Event.current.mousePosition);
                if (closestCurve.curve != null)
                {
                    Vector3 pos = closestCurve.curve.GetPosition(closestCurve.t);
                    Handles.color = RailwayEditorSettings.Instance.SplitLineColor;
                    Handles.DrawLine(closestCurve.rayPoint, pos);
                    //Handles.FreeMoveHandle(pos, Quaternion.identity, 0.125f, Vector3.zero, Handles.SphereHandleCap);
                    //Gizmos.DrawSphere(pos, 0.125f);
                }
                
                SceneView.RepaintAll();
            }
        }
        private void SplitTool_HandleInput()
        {
            if (prevHotControl != GUIUtility.hotControl && Event.current.button == 0)
            {
                if (Event.current.shift && Event.current.rawType == EventType.MouseDown)
                {
                    if (closestCurve.curve != null)
                    {
                        SplitCurve(closestCurve.curve, closestCurve.t);
                    }
                }
                
            }
        }

        private void RemoveTool_Draw()
        {
            Handles.color = RailwayEditorSettings.Instance.AnchorColor;
            for (int i = 0; i < points.Count; i++)
            {
                switch (points[i])
                {
                    case ControlPoint:
                        pointControlIDs[i] = -1;
                        break;
                    case AnchorPoint p:
                    {
                        pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                        Handles.FreeMoveHandle(pointControlIDs[i], p.position, p.rotation,
                            RailwayEditorSettings.Instance.AnchorSize, Vector3.zero,
                            Handles.SphereHandleCap);
                        //Handles.FreeMoveHandle(p.position, Quaternion.identity,
                        //    RailwayEditorSettings.Instance.AnchorSize, Vector3.zero, Handles.SphereHandleCap);
                        break;
                    }
                }
            }

            float normalDelta = 1f / ((float) RailwayEditorSettings.Instance.RailNormalCount);
            for (int i = 0; i < curves.Count; i++)
            {
                BezierCurve c = curves[i];

                Handles.DrawBezier(
                    c.start.position,
                    c.end.position,
                    c.controlStart.position,
                    c.controlEnd.position,
                    RailwayEditorSettings.Instance.RailLineColor,
                    null,
                    RailwayEditorSettings.Instance.RailLineThickness);

                if (!RailwayEditorSettings.Instance.RailNormalShow)
                    continue;

                Handles.color = RailwayEditorSettings.Instance.RailNormalColor;
                for (float j = 0f; j <= 1.00001f; j += normalDelta)
                {
                    Vector3 normal = c.GetNormal(j);
                    Vector3 pointPos = c.GetPosition(j);

                    Handles.DrawLine(pointPos, pointPos + RailwayEditorSettings.Instance.RailNormalLength * normal,
                        RailwayEditorSettings.Instance.RailNormalThickness);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                SceneView.RepaintAll();
            }
        }
        private void RemoveTool_HandleInput()
        {
            if (prevHotControl != GUIUtility.hotControl && Event.current.button == 0)
            {
                if (GUIUtility.hotControl != 0)
                {
                    if (Event.current.shift)
                    {
                        selectedPoint = pointControlIDs.IndexOf(GUIUtility.hotControl);
                        if (selectedPoint != -1 && points[selectedPoint].GetType() == typeof(AnchorPoint))
                        {
                            RemovePoint(points[selectedPoint]);
                        }
                        updateInspector = true;
                    }
                }
            }
        }
        
        
        
        
        
        private int GetClosestAnchor(Vector3 pos, float minDistance = Mathf.Infinity, int ignoreIndex = -1)
        {
            float closestDist = Mathf.Infinity;
            int closestIndex = -1;
            for (int i = 0; i < points.Count; i++)
            {
                if (i == ignoreIndex)
                    continue;
                switch (points[i])
                {
                    case ControlPoint:
                        break;
                    case AnchorPoint ap:
                    {
                        float dist = Vector3.Distance(ap.position, pos);
                        if (dist > minDistance)
                            break;
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestIndex = i;
                        }
                        break;
                    }
                }
            }

            return closestIndex;
        }

        private void AddCurveConnectedTo(int pointIndex)
        {
            
            //oldAnchor = (AnchorPoint)points[pointIndex];
            //newAnchor = new AnchorPoint(oldAnchor.position, oldAnchor.rotation);
            //newControlStart = new ControlPoint(oldAnchor, 1, true);
            //newControlEnd = new ControlPoint(newAnchor, 1, false);
            //newPointIndex = points.Count;
            ////selectedPoint = newPointIndex;
            //points.Add(newAnchor);
            //points.Add(newControlStart);
            //points.Add(newControlEnd);
            //curves.Add(new BezierCurve(oldAnchor, newControlStart, newControlEnd, newAnchor));
            //pointControlIDs.Add(-1);
            //pointControlIDs.Add(-1);
            //pointControlIDs.Add(-1);
            //updateNewCurve = true;
            ////GUIUtility.hotControl = -1;
            

            oldAnchor = (AnchorPoint) points[pointIndex];
            newAnchor = new AnchorPoint(oldAnchor.position, oldAnchor.rotation);
            newControlStart = new ControlPoint(oldAnchor, 1, true);
            newControlEnd = new ControlPoint(newAnchor, 1, false);
            
            BezierCurve curve = new BezierCurve(oldAnchor, newControlStart, newControlEnd, newAnchor);
            AddCurve_Internal(curve);
            newPointIndex = points.IndexOf(newAnchor);
            updateNewCurve = true;
        }

        private void SavePathsAndCurves()
        {
            if (railPathObj == null)
                return;
            
            railPathObj.Update();

            //SerializedProperty railPathPointsProp = railPathProp.FindPropertyRelative("pathPoints");
            
            //SerializedProperty railPathPointsProp = railPathObj.GetIterator();
            //do
            //{
            //    if (railPathPointsProp.name == "pathPoints")
            //    {
            //        Debug.Log(railPathPointsProp.name);
            //        break;
            //    }
            //} while (railPathPointsProp.Next(true));
            
            
            SerializedProperty railPathPointsProp = railPathObj.FindProperty("pathPoints");
            SerializedProperty railPathCurvesProp = railPathObj.FindProperty("pathCurves");

            if (railPathPointsProp == null)
            {
                Debug.Log("railPathPointsProp == null");
                railPathPointsProp.managedReferenceValue = new List<PathPoint>();
            }
                
            
            railPathPointsProp.ClearArray();

            for (int i = 0; i < points.Count; i++)
            {
                railPathPointsProp.InsertArrayElementAtIndex(i);
                SerializedProperty pointProp = railPathPointsProp.GetArrayElementAtIndex(i);
                SerializedProperty pointPointTypeProp = pointProp.FindPropertyRelative("pointType");
                SerializedProperty pointPositionProp = pointProp.FindPropertyRelative("position");
                SerializedProperty pointRotationProp = pointProp.FindPropertyRelative("rotation");
                SerializedProperty pointFlippedProp = pointProp.FindPropertyRelative("flipped");
                SerializedProperty pointConnectedPointsProp = pointProp.FindPropertyRelative("connectedPoints");

                switch (points[i])
                {
                    case AnchorPoint p:
                    {
                        pointPointTypeProp.enumValueIndex = 0;
                            pointPositionProp.vector3Value = p.position;
                            pointRotationProp.quaternionValue = p.rotation;
                            pointConnectedPointsProp.ClearArray();
                            for (int j = 0; j < p.controlPoints.Count; j++)
                            {
                                pointConnectedPointsProp.InsertArrayElementAtIndex(j);
                                SerializedProperty pointConnectedPointsIndexProp = pointConnectedPointsProp.GetArrayElementAtIndex(0);
                                pointConnectedPointsIndexProp.intValue = points.IndexOf(p.controlPoints[j]);
                            }

                            break;
                    }
                    case ControlPoint p:
                    {
                        pointPointTypeProp.enumValueIndex = 1;
                            pointPositionProp.vector3Value = p.position;
                            pointFlippedProp.boolValue = p.flipped;
                            pointConnectedPointsProp.ClearArray();
                            pointConnectedPointsProp.InsertArrayElementAtIndex(0);
                            SerializedProperty pointConnectedPointsIndexProp = pointConnectedPointsProp.GetArrayElementAtIndex(0);
                            pointConnectedPointsIndexProp.intValue = points.IndexOf(p.anchorPoint);
                            break;
                    }
                }
            }

            railPathCurvesProp.ClearArray();
            
            for (int i = 0; i < curves.Count; i++)
            {
                railPathCurvesProp.InsertArrayElementAtIndex(i);
                SerializedProperty curveProp = railPathCurvesProp.GetArrayElementAtIndex(i);
                SerializedProperty curveStartProp = curveProp.FindPropertyRelative("start");
                SerializedProperty curveControlStartProp = curveProp.FindPropertyRelative("controlStart");
                SerializedProperty curveControlEndProp = curveProp.FindPropertyRelative("controlEnd");
                SerializedProperty curveEndProp = curveProp.FindPropertyRelative("end");

                curveStartProp.intValue = points.IndexOf(curves[i].start);
                curveControlStartProp.intValue = points.IndexOf(curves[i].controlStart);
                curveControlEndProp.intValue = points.IndexOf(curves[i].controlEnd);
                curveEndProp.intValue = points.IndexOf(curves[i].end);
            }

            railPathObj.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            SavePathsAndCurves();

            Tools.current = _prevTool;
        }

        private void CustomSphereCapFunction(int controlID, Vector3 pos, Quaternion rot, float size, EventType eventType)
        {
            pointControlIDs[0] = controlID;
            Handles.SphereHandleCap(controlID, pos, rot, size, eventType);
        }

        private void AddNewCurve()
        {
            
            //int index = points.Count;
            //points.Add(new AnchorPoint());
            //points.Add(new ControlPoint());
            //points.Add(new ControlPoint());
            //points.Add(new AnchorPoint());
            //
            //((ControlPoint) points[index+2]).flipped = true;
//
            //((AnchorPoint) points[index+0]).AddControlPoint((ControlPoint) points[index+1]);
            //((AnchorPoint) points[index+3]).AddControlPoint((ControlPoint) points[index+2]);
//
            //((AnchorPoint) points[index+0]).UpdatePosition(new Vector3(0, 0,0));
            //((AnchorPoint) points[index+3]).UpdatePosition(new Vector3(4, 0,0));
//
            //((ControlPoint) points[index+1]).UpdatePosition(new Vector3(1,0, 1));
            //((ControlPoint) points[index+2]).UpdatePosition(new Vector3(3,0, -1));
            //
            //curves.Add(new BezierCurve(
            //    (AnchorPoint) points[index+0],
            //    (ControlPoint) points[index+1],
            //    (ControlPoint) points[index+2],
            //    (AnchorPoint) points[index+3]));
            //
            //pointControlIDs.AddRange(new List<int>{-1,-1,-1,-1});
            //
            //SceneView.RepaintAll();
            

            AnchorPoint start = new AnchorPoint();
            ControlPoint cStart = new ControlPoint();
            ControlPoint cEnd = new ControlPoint();
            AnchorPoint end = new AnchorPoint();

            cEnd.flipped = true;
            
            start.AddControlPoint(cStart);
            end.AddControlPoint(cEnd);
            
            start.UpdatePosition(new Vector3(0, 0, 0));
            end.UpdatePosition(new Vector3(4, 0, 0));
            
            cStart.UpdatePosition(new Vector3(1, 0, 1));
            cEnd.UpdatePosition(new Vector3(3, 0, -1));

            BezierCurve curve = new BezierCurve(start, cStart, cEnd, end);
            
            AddCurve_Internal(curve);
            
            SceneView.RepaintAll();
        }

        private void RemovePoint(Point p)
        {
            
            //if (p == null)
            //    return;
            //List<CurvePointData> pCurves = GetCurvesWithPoint(p);
            //for (int i = pCurves.Count - 1; i >= 0; i--)
            //{
            //    if (points.Contains(pCurves[i].curve.controlStart))
            //    {
            //        pCurves[i].curve.controlStart.anchorPoint.RemoveControlPoint(pCurves[i].curve.controlStart);
            //        points.Remove(pCurves[i].curve.controlStart);
            //        pointControlIDs.RemoveAt(0);
            //    }
            //    if (points.Contains(pCurves[i].curve.controlEnd))
            //    {
            //        pCurves[i].curve.controlEnd.anchorPoint.RemoveControlPoint(pCurves[i].curve.controlEnd);
            //        points.Remove(pCurves[i].curve.controlEnd);
            //        pointControlIDs.RemoveAt(0);
            //    }
//
            //    if (GetCurvesCountWithPoint(pCurves[i].curve.start) < 2)
            //    {
            //        points.Remove(pCurves[i].curve.start);
            //        pointControlIDs.RemoveAt(0);
            //    }
            //    if (GetCurvesCountWithPoint(pCurves[i].curve.end) < 2)
            //    {
            //        points.Remove(pCurves[i].curve.end);
            //        pointControlIDs.RemoveAt(0);
            //    }
//
            //    curves.Remove(pCurves[i].curve);
            //}
            
            
            RemovePoint_Internal(p);
        }

        private void SplitCurve(BezierCurve c, float t)
        {
            Vector3 E = Vector3.Lerp(c.start.position, c.controlStart.position, t);
            Vector3 F = Vector3.Lerp(c.controlStart.position, c.controlEnd.position, t);
            Vector3 G = Vector3.Lerp(c.controlEnd.position, c.end.position, t);
            
            Vector3 H = Vector3.Lerp(E, F, t);
            Vector3 J = Vector3.Lerp(F, G, t);
            
            Vector3 K = Vector3.Lerp(H, J, t);

            AnchorPoint mid = new AnchorPoint(K, Quaternion.identity);
            ControlPoint low = new ControlPoint(mid, H);
            ControlPoint high = new ControlPoint(mid, J);
            
            // rotate mid
            //Vector3 tangent = c.GetTangent(t);
            //mid.SetRotation(Quaternion.AngleAxis(Mathf.LerpAngle(c.start.rotation.eulerAngles.z, c.end.rotation.eulerAngles.z, t), tangent), false);
            mid.SetRotation(Quaternion.LookRotation(c.GetTangent(t), c.GetNormal(t)), false);
            low.Flip();

            //points.Add(mid);
            //points.Add(low);
            //points.Add(high);
            //pointControlIDs.Add(-1);
            //pointControlIDs.Add(-1);
            //pointControlIDs.Add(-1);
            
            c.controlStart.UpdatePosition(E);
            c.controlEnd.UpdatePosition(G);

            BezierCurve a = new BezierCurve(c.start, c.controlStart, low, mid);
            BezierCurve b = new BezierCurve(mid, high, c.controlEnd, c.end);
            AddCurve_Internal(a);
            AddCurve_Internal(b);
            RemoveCurve_Internal(c);
            //curves.Remove(c);
            //curves.Add(new BezierCurve(c.start, c.controlStart, low, mid));
            //curves.Add(new BezierCurve(mid, high, c.controlEnd, c.end));


        }

        // p1 = overlapPoint , p2 = selectedPoint
        private void CombinePoints(AnchorPoint p1, AnchorPoint p2)
        {
            Vector3 p1Forward = p1.rotation * Vector3.forward;
            Vector3 p2Forward = p2.rotation * Vector3.forward;
            
            float dot = Vector3.Dot(p1Forward, p2Forward);
            bool sameDirection = dot > 0;
            
            List<CurveUtility.CurvePointData> p2Data = CurveUtility.GetCurvesWithPoint(curves, p2);
            
            bool checkP1 = false;
            for (int i = 0; i < p2Data.Count; i++)
            {
                
                BezierCurve.Sides side = p2Data[i].side; // the side the selected point is on the curve
                BezierCurve curve = p2Data[i].curve; // the curve the selected point is part of
                curve.SetAnchor(p1, side);
                if (curve.IsInvalid())
                {
                    // remove curve
                    curve.controlStart.anchorPoint.RemoveControlPoint(curve.controlStart);
                    points.Remove(curve.controlStart);
                    pointControlIDs.RemoveAt(0);
                    curve.controlEnd.anchorPoint.RemoveControlPoint(curve.controlEnd);
                    points.Remove(curve.controlEnd);
                    pointControlIDs.RemoveAt(0);
                    if (CurveUtility.GetCurvesCountWithPoint(curves, p1) <= 1)
                    {
                        checkP1 = true;
                    }
                    curves.Remove(curve);
                    continue;
                }
                ControlPoint control = curve.GetControl(side);
                p1.AddControlPoint(control);
                if (!sameDirection) control.Flip();
            }

            points.Remove(p2);
            pointControlIDs.RemoveAt(0);

            p1.UpdateControls();
            
            if (checkP1)
            {
                List<CurveUtility.CurvePointData> data = CurveUtility.GetCurvesWithPoint(curves, p1);
                if (data.Count <= 1)
                {
                    if (data.Count == 1 && data[0].curve.IsInvalid())
                    {
                        curves.Remove(data[0].curve);
                        points.Remove(p1);
                        pointControlIDs.RemoveAt(0);
                    }
                }
            }
        }

        private struct CurveDistanceData
        {
            public BezierCurve curve;
            public float distance;
            public float t;
            public Vector3 rayPoint;

            public CurveDistanceData(BezierCurve c, float d, float t, Vector3 rp)
            {
                curve = c;
                distance = d;
                this.t = t;
                rayPoint = rp;
            }
        }

        private CurveDistanceData GetClosestCurve(Vector2 screenPos)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(screenPos);
            float originalDelta = 1f / (float)RailwayEditorSettings.Instance.SplitDistantSearchCount;
            BezierCurve bestCurve = null;
            float bestT = 0;
            float bestDist = Mathf.Infinity;
            Vector3 bestRayPoint = Vector3.zero;
            foreach (var c in curves)
            {
                float delta = originalDelta;
                float cBestT = 0;
                float cBestDist = Mathf.Infinity;
                Vector3 cBestRayPoint = Vector3.zero;
                for (float t = 0f; t <= 1f; t+=delta)
                {
                    Vector3 cPos = c.GetPosition(t);
                    Vector3 rPos = GetClosestPointOnRay(ray, cPos);
                    float dist = (cPos - rPos).magnitude;
                    if (dist < cBestDist)
                    {
                        cBestDist = dist;
                        cBestT = t;
                        cBestRayPoint = rPos;
                    }
                }

                cBestT -= delta * 0.5f;
                float oldBest = delta;
                delta *= delta;
                cBestT = Mathf.Clamp01(cBestT);
                
                cBestDist = Mathf.Infinity;
                for (float t = cBestT; t <= cBestT+oldBest; t += delta)
                {
                    if (t > 1)
                        break;
                    Vector3 cPos = c.GetPosition(t);
                    Vector3 rPos = GetClosestPointOnRay(ray, cPos);
                    float dist = (cPos - rPos).magnitude;
                    if (dist < cBestDist)
                    {
                        cBestDist = dist;
                        cBestT = t;
                        cBestRayPoint = rPos;
                    }
                }
                
                cBestT = Mathf.Clamp01(cBestT);

                if (cBestDist < bestDist)
                {
                    bestCurve = c;
                    bestDist = cBestDist;
                    bestT = cBestT;
                    bestRayPoint = cBestRayPoint;
                }
            }

            return new CurveDistanceData(bestCurve, bestDist, bestT, bestRayPoint);
        }

        private Vector3 GetClosestPointOnRay(Ray r, Vector3 p)
        {
            return r.origin + Vector3.Project(p - r.origin, r.direction);
        }
        */



        /*


        private RailwayManager manager;

        //private SerializedProperty railwayNetworkProp;

        //private SerializedProperty pointsProp;

        public void OnEnable()
        {*/
        /*
        serializedObject.Update();

        railwayNetworkProp = serializedObject.FindProperty("railwayNetwork");

        if (railwayNetworkProp.managedReferenceValue == null)
            railwayNetworkProp.managedReferenceValue = new RailwayNetwork();

        Debug.Log(railwayNetworkProp);
        pointsProp = railwayNetworkProp.FindPropertyRelative("points");
        Debug.Log(pointsProp);

        serializedObject.ApplyModifiedProperties();
        */
        /*
        manager = (RailwayManager) target;

        if (manager.railwayNetwork == null)
            manager.railwayNetwork = new RailwayNetwork();

        EditorUtility.SetDirty(manager);
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        if (GUILayout.Button("Reset"))
        {
            manager.railwayNetwork = new RailwayNetwork();

            EditorUtility.SetDirty(manager);
        }
    }

    private void OnSceneGUI()
    {
        //if (railwayNetworkProp.managedReferenceValue == null)
        //    return;

        //serializedObject.Update();

        bool changed = false;
        */
        /*
        int pointsCount = pointsProp.arraySize;

        //for (int i = 0; i < pointsCount; i++)
        //{
            //SerializedProperty sp = pointsProp.GetArrayElementAtIndex(i);
            //Point p = (Point) sp.managedReferenceValue;

            //bool updatePoint = false;


            switch (p)
            {
                case AnchorPoint anchor:
                {
                    Handles.color = Color.blue;
                    handlePos = Handles.FreeMoveHandle(anchor.position, anchor.rotation, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                    if (anchor.position != handlePos)
                    {
                        anchor.UpdatePosition(handlePos);
                        updatePoint = true;
                    }
                    break;
                }

                case ControlPoint control:
                {
                    Handles.color = Color.red;
                    handlePos = Handles.FreeMoveHandle(control.position, Quaternion.identity, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                    if (control.position != handlePos)
                    {
                        control.UpdatePosition(handlePos);
                        updatePoint = true;
                    }
                    break;
                }
            }


            sp.managedReferenceValue = p;
        }


        serializedObject.ApplyModifiedProperties();
        */

        /*
        foreach (Point p in manager.railwayNetwork.points)
        {
            Vector3 handlePos;
            switch (p)
            {
                case AnchorPoint anchor:
                {
                    Handles.color = Color.blue;
                    //handlePos = Handles.FreeMoveHandle(anchor.position, anchor.rotation, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                    handlePos = Handles.PositionHandle(anchor.position, anchor.rotation);
                    if (anchor.position != handlePos)
                    {
                        anchor.UpdatePosition(handlePos);
                        changed = true;
                    }
                    break;
                }
                case ControlPoint control:
                {
                    Handles.color = Color.red;
                    //handlePos = Handles.FreeMoveHandle(control.position, Quaternion.identity, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                    handlePos = Handles.PositionHandle(control.position, Quaternion.identity);
                    if (control.position != handlePos)
                    {
                        control.UpdatePosition(handlePos);
                        changed = true;
                    }
                    break;
                }
            }


        } 

        if (changed)
        {

            EditorUtility.SetDirty(manager);
        }

        Handles.DrawBezier(
            manager.railwayNetwork.points[0].position, 
            manager.railwayNetwork.points[3].position, 
            manager.railwayNetwork.points[1].position, 
            manager.railwayNetwork.points[2].position, 
            Color.magenta, null, 2);

        float dist = HandleUtility.DistancePointBezier(new Vector3(2, 0, 0), 
            manager.railwayNetwork.points[0].position, 
            manager.railwayNetwork.points[3].position, 
            manager.railwayNetwork.points[1].position, 
            manager.railwayNetwork.points[2].position);

        //Handles.SphereHandleCap(-1, new Vector3(2, 0, 0), Quaternion.identity, dist*2f, EventType.Repaint);


    }

    public void OnDisable()
    {
        //Debug.Log("Yam");


    }

*/
    }
}