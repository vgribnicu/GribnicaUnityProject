using System;
using System.Collections;

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FMPCMorphGenerator))]
[CanEditMultipleObjects]
public class FMPCMorphGenerator_Editor : Editor
{
    public FMPCMorphGenerator Generator;

    SerializedProperty Mesh1;
    SerializedProperty Mesh2;

    void OnEnable()
    {
        Mesh1 = serializedObject.FindProperty("Mesh1");
        Mesh2 = serializedObject.FindProperty("Mesh2");
    }

    Color color_default;
    private Texture2D logo;
    public override void OnInspectorGUI()
    {
        color_default = GUI.color;
        if (Generator == null) Generator = (FMPCMorphGenerator)target;

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
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Morph"))
                {
                    if (Application.isPlaying)
                    {
                        Generator.Action_MeshGeneration();
                    }
                    else
                    {
                        Debug.Log("Runtime Only");
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(" Progress: " + (Generator.progress*100f).ToString("00.00") + "%");

                int hours = Mathf.FloorToInt(Generator.TimeLeft / 3600);
                int minutes = Mathf.FloorToInt((Generator.TimeLeft % 3600) / 60);
                int seconds = Mathf.FloorToInt(Generator.TimeLeft % 60);
                String TimeLeftStr = hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
                GUILayout.Label(" Estimated: " + TimeLeftStr);
                //GUILayout.Label(" Estimated: " + Generator.VertChecked + ":" + Generator.MaximumPoints);
                //GUILayout.Label(" Estimated: " + Generator.TaskThreadFinished + ":" + Generator.TaskThreadCount);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(Mesh1, new GUIContent(" Mesh1"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(Mesh2, new GUIContent(" Mesh2"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
        GUI.color = color_default;

        serializedObject.ApplyModifiedProperties();
    }
}
