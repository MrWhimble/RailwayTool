using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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

        //private SerializedObject _settingsObject;
        //
        //private SerializedProperty _anchorColorProp;
        //private SerializedProperty _anchorSizeProp;
        //
        //private SerializedProperty _controlColorProp;
        //private SerializedProperty _controlSizeProp;
        //
        //private SerializedProperty _controlLineShowProp;
        //private SerializedProperty _controlLineThicknessProp;
        //
        //private SerializedProperty _interControlLineShowProp;
        //private SerializedProperty _interControlLineColorProp;
        //private SerializedProperty _interControlLineThicknessProp;
        //
        //private SerializedProperty _railLineColorProp;
        //private SerializedProperty _railLineThicknessProp;
        //
        //private SerializedProperty _railNormalShowProp;
        //private SerializedProperty _railNormalColorProp;
        //private SerializedProperty _railNormalThicknessProp;
        //private SerializedProperty _railNormalLengthProp;


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
                    //AssetDatabase.CreateAsset(newSettings, "Assets/new RailwayEditorSettings.asset");
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
            RailwayEditorSettings.Instance.SplitDistantSearchCount = Mathf.Max(2, EditorGUILayout.IntField("Split Distance Search Count", RailwayEditorSettings.Instance.SplitDistantSearchCount));
            EditorGUI.indentLevel--;

            /*
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.LabelField("Anchor Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.AnchorColor = EditorGUILayout.ColorField("Anchor Color", RailwayEditorSettings.AnchorColor);
            RailwayEditorSettings.AnchorSize = EditorGUILayout.FloatField("Anchor Size", RailwayEditorSettings.AnchorSize);
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Control Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.ControlColor = EditorGUILayout.ColorField("Control Color", RailwayEditorSettings.ControlColor);
            RailwayEditorSettings.Instance.ControlSize = EditorGUILayout.FloatField("Control Size", RailwayEditorSettings.ControlSize);
            
            RailwayEditorSettings.Instance.ControlLineShow = EditorGUILayout.BeginToggleGroup("Show Control Lines", RailwayEditorSettings.ControlLineShow);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.ControlLineColor = EditorGUILayout.ColorField("Control Line Color", RailwayEditorSettings.ControlLineColor);
            RailwayEditorSettings.Instance.ControlLineThickness = EditorGUILayout.FloatField("Control Line Thickness", RailwayEditorSettings.ControlLineThickness);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            
            RailwayEditorSettings.Instance.InterControlLineShow = EditorGUILayout.BeginToggleGroup("Show Lines Between Controls", RailwayEditorSettings.InterControlLineShow);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.InterControlLineColor = EditorGUILayout.ColorField("Inter-Control Line Color", RailwayEditorSettings.InterControlLineColor);
            RailwayEditorSettings.Instance.InterControlLineThickness = EditorGUILayout.FloatField("Inter-Control Line Thickness", RailwayEditorSettings.InterControlLineThickness);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Rail Line Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.RailLineColor = EditorGUILayout.ColorField("Rail Line Color", RailwayEditorSettings.RailLineColor);
            RailwayEditorSettings.Instance.RailLineThickness = EditorGUILayout.FloatField("Rail Line Thickness", RailwayEditorSettings.RailLineThickness);
            
            RailwayEditorSettings.Instance.RailNormalShow = EditorGUILayout.BeginToggleGroup("Show Rail Normals", RailwayEditorSettings.RailNormalShow);
            EditorGUI.indentLevel++;
            RailwayEditorSettings.Instance.RailNormalColor = EditorGUILayout.ColorField("Rail Normal Color", RailwayEditorSettings.RailNormalColor);
            RailwayEditorSettings.Instance.RailNormalThickness = EditorGUILayout.FloatField("Rail Normal Thickness", RailwayEditorSettings.RailNormalThickness);
            RailwayEditorSettings.Instance.RailNormalLength = EditorGUILayout.FloatField("Rail Normal Length", RailwayEditorSettings.RailNormalLength);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            
            EditorGUI.indentLevel--;
            */
            if (EditorGUI.EndChangeCheck())
            {
                
                Repaint();
            }
            
        }
    }
}