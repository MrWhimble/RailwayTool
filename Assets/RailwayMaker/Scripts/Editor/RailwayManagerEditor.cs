using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailwayManager))]
    public class RailwayManagerEditor : Editor
    {
        public RailwayManager manager;

        public void OnEnable()
        {
            manager = (RailwayManager) target;
        }

        public override void OnInspectorGUI()
        {
            if (manager.railwayNetwork == null)
                manager.railwayNetwork = new RailwayNetwork();

            
        }

        private void OnSceneGUI()
        {
            if (manager.railwayNetwork == null)
                return;
            
            foreach (Point p in manager.railwayNetwork.points)
            {
                Vector3 handlePos;
                switch (p)
                {
                    case AnchorPoint anchor:
                    {
                        Handles.color = Color.blue;
                        handlePos = Handles.FreeMoveHandle(anchor.position, anchor.rotation, 0.5f, Vector3.zero,
                            Handles.SphereHandleCap);
                        if (anchor.position != handlePos)
                        {
                            anchor.UpdatePosition(handlePos);
                        }
                        break;
                    }
                    case ControlPoint control:
                    {
                        Handles.color = Color.red;
                        handlePos = Handles.FreeMoveHandle(control.position, Quaternion.identity, 0.5f, Vector3.zero,
                            Handles.SphereHandleCap);
                        if (control.position != handlePos)
                        {
                            control.UpdatePosition(handlePos);
                        }
                        break;
                    }
                }
                
                Handles.DrawBezier(manager.railwayNetwork.points[0].position, manager.railwayNetwork.points[3].position, manager.railwayNetwork.points[1].position, manager.railwayNetwork.points[2].position, Color.magenta, null, 2);
            } 
        }
    }
}