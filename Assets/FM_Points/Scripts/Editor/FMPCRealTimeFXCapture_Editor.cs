using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FMPCRealTimeFXCapture))]
[CanEditMultipleObjects]
public class FMPCRealTimeFXCapture_Editor : Editor
{
    public FMPCRealTimeFXCapture fXCapture;

    SerializedProperty captureWidthProp;
    SerializedProperty captureHeightProp;
    SerializedProperty PCRenderersProp;


    private bool PCRenderersFold = false;

    void OnEnable()
    {
        captureWidthProp = serializedObject.FindProperty("captureWidth");
        captureHeightProp = serializedObject.FindProperty("captureHeight");

        PCRenderersProp = serializedObject.FindProperty("PCRenderers");
    }

    private Texture2D logo;
    public override void OnInspectorGUI()
    {
        if (fXCapture == null) fXCapture = (FMPCRealTimeFXCapture)target;

        if (logo == null) logo = Resources.Load<Texture2D>("Logo/" + "Logo_FMPoints");
        if (logo != null)
        {
            const float maxLogoWidth = 430.0f;
            EditorGUILayout.Separator();
            float w = EditorGUIUtility.currentViewWidth;
            Rect r = new Rect();
            r.width = Math.Min(w - 40.0f, maxLogoWidth);
            r.height = r.width / 4.886f;
            Rect r2 = GUILayoutUtility.GetRect(r.width, r.height);
            r.x = r2.x;
            r.y = r2.y;
            GUI.DrawTexture(r, logo, ScaleMode.ScaleToFit);
            if (GUI.Button(r, "", new GUIStyle()))
            {
                Application.OpenURL("http://frozenmist.com");
            }
            EditorGUILayout.Separator();
        }

        serializedObject.Update();

        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginVertical("box");
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.yellow;
                GUILayout.BeginHorizontal();
                GUILayout.Label(" Capture 3D Scene into Points(Experiment Feature)", style);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();


                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(captureWidthProp, new GUIContent(" Capture Width"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(captureHeightProp, new GUIContent(" Capture Height"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                //GUILayout.BeginHorizontal();
                //GUILayout.Label(" Create new Renderer in Scene");
                //GUILayout.EndHorizontal();

                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Renderer"))
                {
                    fXCapture.Action_AddRenderer();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                {
                    int PCRenderersObjectsNum = PCRenderersProp.FindPropertyRelative("Array.size").intValue;

                    if (PCRenderersFold)
                    {
                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        if (GUILayout.Button("- PCRenderers: " + PCRenderersObjectsNum)) PCRenderersFold = false;
                        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                        DrawPropertyArray(PCRenderersProp);
                    }
                    else
                    {
                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        if (GUILayout.Button("+ PCRenderers: " + PCRenderersObjectsNum)) PCRenderersFold = true;
                        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    }
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }


        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPropertyArray(SerializedProperty property)
    {
        SerializedProperty arraySizeProp = property.FindPropertyRelative("Array.size");
        EditorGUILayout.PropertyField(arraySizeProp);

        EditorGUI.indentLevel++;

        for (int i = 0; i < arraySizeProp.intValue; i++)
        {
            EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i));
        }

        EditorGUI.indentLevel--;
    }
}
