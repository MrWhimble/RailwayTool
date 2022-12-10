using System;
using System.Collections.Generic;
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

        //private int pointControlIDIndex;
        private List<int> pointControlIDs;
        private int prevHotControl;


        private void OnEnable()
        {
            Debug.Log("OnEnable");

            railPathProp = serializedObject.FindProperty("pathData");
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
            
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (GUILayout.Button("Add New Curve"))
            {
                AddNewCurve();
            }

            int prevSelectedPoint = selectedPoint;
            selectedPoint = EditorGUILayout.IntField("Current Point Index", selectedPoint);
            if (prevSelectedPoint != selectedPoint)
            {
                if (selectedPoint >= 0 && selectedPoint < points.Count)
                {
                    
                    if (points[selectedPoint].GetType() == typeof(AnchorPoint))
                        selectedPointRot = ((AnchorPoint) points[selectedPoint]).rotation.eulerAngles;
                }
                // update current curve
            }

            Vector3 prevV3;
            Vector3 newV3;
            float prevFloat;
            float newFloat;
            bool prevBool;
            bool newBool;

            bool updateScene = false;

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
                        //p.position = EditorGUILayout.Vector3Field("Position", p.position);
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

                if (updateScene)
                {
                    SceneView.RepaintAll();
                }
                
            }
            else
            {
                EditorGUILayout.LabelField("Invalid Selected Point");
            }
            EditorGUILayout.EndVertical();
            
            
        }

        private void OnSceneGUI()
        {
            Vector3 handlePos;
            bool updateInspector = false;
            //pointControlIDIndex = 0;
            for (int i = 0; i < points.Count; i++)
            {
                switch (points[i])
                {
                    case AnchorPoint p:
                    {
                        Handles.color = Color.blue;

                        if (selectedPoint == i)
                        {
                            if (Tools.pivotRotation == PivotRotation.Local)
                                handlePos = Handles.PositionHandle(p.position, p.rotation);
                            else
                                handlePos = Handles.PositionHandle(p.position, Quaternion.identity);
                            pointControlIDs[i] = -1;
                        }
                        else
                        {
                            pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(pointControlIDs[i], p.position, p.rotation, 0.5f, Vector3.zero,
                                Handles.SphereHandleCap);
                        }

                        if (p.position != handlePos)
                        {
                            p.UpdatePosition(handlePos);
                            selectedPointRot = p.rotation.eulerAngles;
                            updateInspector = true;
                            //updatePoint = true;
                        }
                        break;
                    }

                    case ControlPoint p:
                    {
                        Handles.color = Color.red;
                        if (selectedPoint == i)
                        {
                            handlePos = Handles.PositionHandle(p.position, Quaternion.identity);
                            pointControlIDs[i] = -1;
                        }
                        else
                        {
                            pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(pointControlIDs[i], p.position, Quaternion.identity, 0.5f, Vector3.zero,
                                Handles.SphereHandleCap);
                        }

                        if (p.position != handlePos)
                        {
                            p.UpdatePosition(handlePos);
                            selectedPointRot = p.anchorPoint.rotation.eulerAngles;
                            updateInspector = true;
                            //updatePoint = true;
                        }
                        break;
                    }
                }
            }

            

            for (int i = 0; i < curves.Count; i++)
            {
                Handles.DrawBezier(
                    curves[i].start.position,
                    curves[i].end.position,
                    curves[i].controlStart.position,
                    curves[i].controlEnd.position,
                    Color.magenta, null, 2);

                for (float j = 0f; j <= 1.01f; j += 0.05f)
                {
                    //Vector3 lerpUp = Quaternion.Lerp(curves[i].start.rotation,curves[i].end.rotation, j) * Vector3.up;
                    Vector3 forward = curves[i].GetTangent(j);
                    Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
                    Vector3 up = rot * Vector3.up;
                    
                    
                    
                    float angle = Mathf.LerpAngle(curves[i].start.rotation.eulerAngles.z,
                        curves[i].end.rotation.eulerAngles.z, j);

                    up = Quaternion.AngleAxis(angle, forward) * up;
                    
                    //Vector3 normal = curves[i].GetTangent(j);
                    Debug.DrawRay(curves[i].GetPosition(j), up);
                    //Debug.DrawRay(curves[i].GetPosition(j), lerpUp, Color.red);
                }
            }

            
            if (prevHotControl != GUIUtility.hotControl && Event.current.shift && Event.current.button == 0)
            {
                if (GUIUtility.hotControl != 0)
                {
                    selectedPoint = pointControlIDs.IndexOf(GUIUtility.hotControl);
                    if (selectedPoint != -1 && points[selectedPoint].GetType() == typeof(AnchorPoint))
                        selectedPointRot = ((AnchorPoint) points[selectedPoint]).rotation.eulerAngles;
                    prevHotControl = GUIUtility.hotControl;

                    updateInspector = true;
                }
            }
            
            
            if (updateInspector)
            {
                Repaint();
            }
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable");

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

        private void CustomSphereCapFunction(int controlID, Vector3 pos, Quaternion rot, float size, EventType eventType)
        {
            pointControlIDs[0] = controlID;
            Handles.SphereHandleCap(controlID, pos, rot, size, eventType);
        }

        private void AddNewCurve()
        {
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
        }

        private void RemovePoint(Point p)
        {
            List<CurvePointData> pCurves = GetCurvesWithPoint(p);
            for (int i = pCurves.Count - 1; i >= 0; i--)
            {
                if (points.Contains(pCurves[i].curve.controlStart))
                {
                    points.Remove(pCurves[i].curve.controlStart);
                }
                if (points.Contains(pCurves[i].curve.controlEnd))
                {
                    points.Remove(pCurves[i].curve.controlEnd);
                }

                if (GetCurvesCountWithPoint(pCurves[i].curve.start) < 2)
                {
                    points.Remove(pCurves[i].curve.start);
                }
                if (GetCurvesCountWithPoint(pCurves[i].curve.end) < 2)
                {
                    points.Remove(pCurves[i].curve.end);
                }

                curves.Remove(pCurves[i].curve);
            }
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
            Vector3 tangent = c.GetTangent(t);
            mid.SetRotation(Quaternion.AngleAxis(Mathf.LerpAngle(c.start.rotation.eulerAngles.z, c.end.rotation.eulerAngles.z, t), tangent), false);
            low.Flip();

            points.Add(mid);
            points.Add(low);
            points.Add(high);
            
            c.controlStart.UpdatePosition(E);
            c.controlEnd.UpdatePosition(G);

            curves.Remove(c);
            curves.Add(new BezierCurve(c.start, c.controlStart, low, mid));
            curves.Add(new BezierCurve(mid, high, c.controlEnd, c.end));
        }

        // p1 = overlapPoint , p2 = selectedPoint
        private void CombinePoints(AnchorPoint p1, AnchorPoint p2)
        {
            Vector3 p1Forward = p1.rotation * Vector3.forward;
            Vector3 p2Forward = p2.rotation * Vector3.forward;
            
            float dot = Vector3.Dot(p1Forward, p2Forward);
            bool sameDirection = dot > 0;
            
            List<CurvePointData> p1Data = GetCurvesWithPoint(p1);
            List<CurvePointData> p2Data = GetCurvesWithPoint(p2);
            
            ControlPoint p1ControlStart = null;
            ControlPoint p1ControlEnd = null;
            for (int i = 0; i < p1Data.Count; i++)
            {
                if (p1Data[i].side == BezierCurve.Sides.Start && p1ControlStart == null)
                    p1ControlStart = p1Data[i].GetControl();
                else if (p1Data[i].side == BezierCurve.Sides.End && p1ControlEnd == null)
                    p1ControlEnd = p1Data[i].GetControl();
            }
            for (int i = 0; i < p2Data.Count; i++)
            {
                p2Data[i].curve.SetAnchor(p1, p2Data[i].side);
                ControlPoint control = p2Data[i].curve.GetControl(p2Data[i].side);
                if (!sameDirection) control.flipped = !control.flipped;
                p2.RemoveControlPoint(control);
                control.anchorPoint = p1;
                p1.AddControlPoint(control);
            }

            points.Remove(p2);
            for (int i = curves.Count-1; i >= 0; i--)
            {
                if (curves[i].IsInvalid())
                    curves.RemoveAt(i);
            }
        }

        private struct CurvePointData
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
        private List<CurvePointData> GetCurvesWithPoint(Point p)
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
        private int GetCurvesCountWithPoint(Point p)
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