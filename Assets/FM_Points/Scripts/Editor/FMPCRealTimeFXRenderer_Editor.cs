using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FMPCRealTimeFXRenderer))]
[CanEditMultipleObjects]
public class FMPCRealTimeFXRenderer_Editor : Editor
{
    public FMPCRealTimeFXRenderer helper;

    SerializedProperty mainColorProp;
    SerializedProperty pointSizeProp;

    SerializedProperty applyDistanceProp;

    void OnEnable()
    {
        mainColorProp = serializedObject.FindProperty("mainColor");
        pointSizeProp = serializedObject.FindProperty("pointSize");
        applyDistanceProp = serializedObject.FindProperty("applyDistance");

    }

    private Texture2D logo;

    Color color_default;
    public override void OnInspectorGUI()
    {
        if (helper == null) helper = (FMPCRealTimeFXRenderer)target;
        serializedObject.Update();

        {
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(mainColorProp, new GUIContent(" Main Color"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(pointSizeProp, new GUIContent(" Point Size"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(applyDistanceProp, new GUIContent(" Apply Distance"));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        serializedObject.ApplyModifiedProperties();
    }
}
