using UnityEditor;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class RailwayMakerSettingsProvider : SettingsProvider
    {
        public RailwayMakerSettingsProvider(string path, SettingsScope scope = SettingsScope.User): base(path, scope){}

        [SettingsProvider]
        public static SettingsProvider CreateRailwayMakerSettingsProvider()
        {
            var provider = new RailwayMakerSettingsProvider("Preferences/MrWhimble/Railway Maker", SettingsScope.User);
            return provider;
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 256;

            RailwayEditorSettings settings = (RailwayEditorSettings)EditorGUILayout.ObjectField("Settings", RailwayEditorSettings.Instance,
                typeof(RailwayEditorSettings), false);
            if (settings != RailwayEditorSettings.Instance)
            {
                RailwayEditorSettings.Instance = settings;
            }

            if (RailwayEditorSettings.Instance == null)
            {
                if (GUILayout.Button("Create New Settings"))
                {
                    RailwayEditorSettings newSettings = ScriptableObject.CreateInstance<RailwayEditorSettings>();
                    newSettings.name = "new RailwayEditorSettings";
                    // https://answers.unity.com/questions/480226/select-asset-for-rename.html
                    ProjectWindowUtil.CreateAsset(newSettings, "Assets/new RailwayEditorSettings.asset");
                }

                return;
            }
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.LabelField("Anchor Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.AnchorColor = EditorGUILayout.ColorField("Anchor Color", RailwayEditorSettings.Instance.AnchorColor);
            RailwayEditorSettings.Instance.AnchorHighlightColor = EditorGUILayout.ColorField("Anchor Highlight Color", RailwayEditorSettings.Instance.AnchorHighlightColor);
            RailwayEditorSettings.Instance.AnchorSize = Mathf.Max(0f, EditorGUILayout.FloatField("Anchor Size", RailwayEditorSettings.Instance.AnchorSize));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Control Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.ControlColor = EditorGUILayout.ColorField("Control Color", RailwayEditorSettings.Instance.ControlColor);
            RailwayEditorSettings.Instance.ControlSize = Mathf.Max(0f, EditorGUILayout.FloatField("Control Size", RailwayEditorSettings.Instance.ControlSize));
            RailwayEditorSettings.Instance.ControlLineShow = EditorGUILayout.BeginToggleGroup("Show Control Lines", RailwayEditorSettings.Instance.ControlLineShow);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.ControlLineColor = EditorGUILayout.ColorField("Control Line Color", RailwayEditorSettings.Instance.ControlLineColor);
            RailwayEditorSettings.Instance.ControlLineThickness = Mathf.Max(0f, EditorGUILayout.FloatField("Control Line Thickness", RailwayEditorSettings.Instance.ControlLineThickness));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            
            RailwayEditorSettings.Instance.InterControlLineShow = EditorGUILayout.BeginToggleGroup("Show Lines Between Controls", RailwayEditorSettings.Instance.InterControlLineShow);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.InterControlLineColor = EditorGUILayout.ColorField("Inter-Control Line Color", RailwayEditorSettings.Instance.InterControlLineColor);
            RailwayEditorSettings.Instance.InterControlLineThickness = Mathf.Max(0f, EditorGUILayout.FloatField("Inter-Control Line Thickness", RailwayEditorSettings.Instance.InterControlLineThickness));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Rail Line Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.RailLineColor = EditorGUILayout.ColorField("Rail Line Color", RailwayEditorSettings.Instance.RailLineColor);
            RailwayEditorSettings.Instance.RailLineThickness = Mathf.Max(0f, EditorGUILayout.FloatField("Rail Line Thickness", RailwayEditorSettings.Instance.RailLineThickness));
            
            RailwayEditorSettings.Instance.RailNormalShow = EditorGUILayout.BeginToggleGroup("Show Rail Normals", RailwayEditorSettings.Instance.RailNormalShow);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.RailNormalColor = EditorGUILayout.ColorField("Rail Normal Color", RailwayEditorSettings.Instance.RailNormalColor);
            RailwayEditorSettings.Instance.RailNormalThickness = Mathf.Max(0f, EditorGUILayout.FloatField("Rail Normal Thickness", RailwayEditorSettings.Instance.RailNormalThickness));
            RailwayEditorSettings.Instance.RailNormalLength = EditorGUILayout.FloatField("Rail Normal Length", RailwayEditorSettings.Instance.RailNormalLength);
            RailwayEditorSettings.Instance.RailNormalCount = Mathf.Max(2, EditorGUILayout.IntField("Rail Normal Count", RailwayEditorSettings.Instance.RailNormalCount));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Split Tool Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.SplitLineColor = EditorGUILayout.ColorField("Split Indicator Line Color", RailwayEditorSettings.Instance.SplitLineColor);
            RailwayEditorSettings.Instance.SplitLineThickness = Mathf.Max(0f, EditorGUILayout.FloatField("Split Line Thickness", RailwayEditorSettings.Instance.SplitLineThickness));
            RailwayEditorSettings.Instance.SplitDistantSearchCount = Mathf.Max(2, EditorGUILayout.IntField("Split Distance Search Count", RailwayEditorSettings.Instance.SplitDistantSearchCount));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Waypoint Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.WaypointConnectionLineColor = EditorGUILayout.ColorField("Waypoint Connection Line Color", RailwayEditorSettings.Instance.WaypointConnectionLineColor);
            RailwayEditorSettings.Instance.WaypointConnectionLineThickness = Mathf.Max(0f, EditorGUILayout.FloatField("Waypoint Connection Line Thickness", RailwayEditorSettings.Instance.WaypointConnectionLineThickness));
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(RailwayEditorSettings.Instance);
                Repaint();
            }
            
        }
    }
}