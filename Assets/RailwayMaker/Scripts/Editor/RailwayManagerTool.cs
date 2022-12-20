using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [EditorTool("Railway Manager Tool", typeof(RailwayManager))]
    public class RailwayManagerTool : EditorTool
    {
        

        public override void OnActivated()
        {
            base.OnActivated();
            Tools.hidden = true;
        }

        public override void OnWillBeDeactivated()
        {
            //base.OnWillBeDeactivated();
            Tools.hidden = false;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }
}