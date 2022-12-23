using System;
using System.Collections.Generic;
using MrWhimble.ConstantConsole;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailwayManager))]
    public class RailwayManagerEditor : Editor
    {
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
            /*
            oldAnchor = (AnchorPoint)points[pointIndex];
            newAnchor = new AnchorPoint(oldAnchor.position, oldAnchor.rotation);
            newControlStart = new ControlPoint(oldAnchor, 1, true);
            newControlEnd = new ControlPoint(newAnchor, 1, false);
            newPointIndex = points.Count;
            //selectedPoint = newPointIndex;
            points.Add(newAnchor);
            points.Add(newControlStart);
            points.Add(newControlEnd);
            curves.Add(new BezierCurve(oldAnchor, newControlStart, newControlEnd, newAnchor));
            pointControlIDs.Add(-1);
            pointControlIDs.Add(-1);
            pointControlIDs.Add(-1);
            updateNewCurve = true;
            //GUIUtility.hotControl = -1;
            */

            oldAnchor = (AnchorPoint) points[pointIndex];
            newAnchor = new AnchorPoint(oldAnchor.position, oldAnchor.rotation);
            newControlStart = new ControlPoint(oldAnchor, 1, true);
            newControlEnd = new ControlPoint(newAnchor, 1, false);
            
            BezierCurve curve = new BezierCurve(oldAnchor, newControlStart, newControlEnd, newAnchor);
            AddCurve_Internal(curve);
            newPointIndex = points.IndexOf(newAnchor);
        }

        private void SavePathsAndCurves()
        {
            if (railPathObj == null)
                return;
            
            railPathObj.Update();

            //SerializedProperty railPathPointsProp = railPathProp.FindPropertyRelative("pathPoints");
            /*
            SerializedProperty railPathPointsProp = railPathObj.GetIterator();
            do
            {
                if (railPathPointsProp.name == "pathPoints")
                {
                    Debug.Log(railPathPointsProp.name);
                    break;
                }
            } while (railPathPointsProp.Next(true));
            */
            
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
            /*
            int index = points.Count;
            points.Add(new AnchorPoint());
            points.Add(new ControlPoint());
            points.Add(new ControlPoint());
            points.Add(new AnchorPoint());
            
            ((ControlPoint) points[index+2]).flipped = true;

            ((AnchorPoint) points[index+0]).AddControlPoint((ControlPoint) points[index+1]);
            ((AnchorPoint) points[index+3]).AddControlPoint((ControlPoint) points[index+2]);

            ((AnchorPoint) points[index+0]).UpdatePosition(new Vector3(0, 0,0));
            ((AnchorPoint) points[index+3]).UpdatePosition(new Vector3(4, 0,0));

            ((ControlPoint) points[index+1]).UpdatePosition(new Vector3(1,0, 1));
            ((ControlPoint) points[index+2]).UpdatePosition(new Vector3(3,0, -1));
            
            curves.Add(new BezierCurve(
                (AnchorPoint) points[index+0],
                (ControlPoint) points[index+1],
                (ControlPoint) points[index+2],
                (AnchorPoint) points[index+3]));
            
            pointControlIDs.AddRange(new List<int>{-1,-1,-1,-1});
            
            SceneView.RepaintAll();
            */

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
            /*
            if (p == null)
                return;
            List<CurvePointData> pCurves = GetCurvesWithPoint(p);
            for (int i = pCurves.Count - 1; i >= 0; i--)
            {
                if (points.Contains(pCurves[i].curve.controlStart))
                {
                    pCurves[i].curve.controlStart.anchorPoint.RemoveControlPoint(pCurves[i].curve.controlStart);
                    points.Remove(pCurves[i].curve.controlStart);
                    pointControlIDs.RemoveAt(0);
                }
                if (points.Contains(pCurves[i].curve.controlEnd))
                {
                    pCurves[i].curve.controlEnd.anchorPoint.RemoveControlPoint(pCurves[i].curve.controlEnd);
                    points.Remove(pCurves[i].curve.controlEnd);
                    pointControlIDs.RemoveAt(0);
                }

                if (GetCurvesCountWithPoint(pCurves[i].curve.start) < 2)
                {
                    points.Remove(pCurves[i].curve.start);
                    pointControlIDs.RemoveAt(0);
                }
                if (GetCurvesCountWithPoint(pCurves[i].curve.end) < 2)
                {
                    points.Remove(pCurves[i].curve.end);
                    pointControlIDs.RemoveAt(0);
                }

                curves.Remove(pCurves[i].curve);
            }
            */
            
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