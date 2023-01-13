using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

            Tools.current = Tool.View;
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
                for (float t = 0f; t <= 1.0001f; t+=delta)
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
                float lastT = cBestT+delta;
                delta *= delta;
                cBestT = Mathf.Clamp01(cBestT);
                
                cBestDist = Mathf.Infinity;
                for (float t = cBestT; t <= lastT; t += delta)
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
    }
}