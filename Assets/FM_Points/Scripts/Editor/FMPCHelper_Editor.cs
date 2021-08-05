using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FMPCHelper))]
[CanEditMultipleObjects]
public class FMPCHelper_Editor : Editor
{
    public FMPCHelper helper;

    SerializedProperty renderTypeProp;
    SerializedProperty brushTypeProp;

    SerializedProperty mainColorProp;
    SerializedProperty pointSizeProp;
    SerializedProperty applyDistanceProp;

    SerializedProperty blendProp;




    #region DataFX
    SerializedProperty windDirectionProp;
    SerializedProperty windPowerProp;
    SerializedProperty angThresholdProp;
    SerializedProperty durationProp;
    #endregion

    SerializedProperty customisedBrushTextureProp;

    void OnEnable()
    {
        renderTypeProp = serializedObject.FindProperty("renderType");
        brushTypeProp = serializedObject.FindProperty("brushType");
        mainColorProp = serializedObject.FindProperty("mainColor");
        pointSizeProp = serializedObject.FindProperty("pointSize");
        applyDistanceProp = serializedObject.FindProperty("applyDistance");

        blendProp = serializedObject.FindProperty("blend");


        windDirectionProp = serializedObject.FindProperty("windDirection");
        windPowerProp = serializedObject.FindProperty("windPower");
        angThresholdProp = serializedObject.FindProperty("angThreshold");
        durationProp = serializedObject.FindProperty("duration");

        customisedBrushTextureProp = serializedObject.FindProperty("customisedBrushTexture");
    }

    private Texture2D logo;

    Color color_default;
    public override void OnInspectorGUI()
    {
        if (helper == null) helper = (FMPCHelper)target;
        serializedObject.Update();

        //{
        //    GUILayout.BeginVertical("box");

        //    GUILayout.BeginHorizontal();
            
        //    GUILayout.EndHorizontal();

        //    GUILayout.EndVertical();
        //}

        {
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(renderTypeProp, new GUIContent(" Render Type"));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(mainColorProp, new GUIContent(" Main Color"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(pointSizeProp, new GUIContent(" Point Size"));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if(helper.RenderType != FMRenderType.UnlitBrush) EditorGUILayout.PropertyField(applyDistanceProp, new GUIContent(" Apply Distance"));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        if (helper.HasBlendShape) GUIblend();

        switch (helper.RenderType)
        {
            case FMRenderType.Unlit:

                break;
            case FMRenderType.Lambert:

                break;
            case FMRenderType.UnlitFX:
                GUIFX();
                break;
            case FMRenderType.LambertFX:
                GUIFX();
                break;
            case FMRenderType.UnlitBrush:
                GUIBrush();
                break;
        }


        serializedObject.ApplyModifiedProperties();
    }
    void GUIBrush()
    {
        GUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(brushTypeProp, new GUIContent(" Brush Type"));
        GUILayout.EndHorizontal();

        if(helper.BrushType == FMBrushType.brush_Customised)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(customisedBrushTextureProp, new GUIContent(" customisedBrushTexture"));
            GUILayout.EndHorizontal();
        }

        if (!Application.isPlaying)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(" Runtime only(experiment feature) ");
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    void GUIblend()
    {
        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(blendProp, new GUIContent(" blend"));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    void GUIFX()
    {
        GUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(windDirectionProp, new GUIContent(" Wind Direction"));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(windPowerProp, new GUIContent(" Wind Power"));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(angThresholdProp, new GUIContent(" Angle Threshold"));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(durationProp, new GUIContent(" duration"));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}
