using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FMPCManager))]
[CanEditMultipleObjects]
public class FMPCManager_Editor : Editor
{
    public FMPCManager Manager;
    public int LastCaptureCount = 0;

    SerializedProperty DebugModeProp;

    SerializedProperty CaptureModeProp;

    SerializedProperty TargetProp;
    SerializedProperty SegmentXProp;
    SerializedProperty SegmentYProp;


    SerializedProperty SubSampleProp;
    SerializedProperty MiniDistanceProp;

    SerializedProperty PLYNormalProp;

    void OnEnable()
    {
        DebugModeProp = serializedObject.FindProperty("DebugMode");

        CaptureModeProp = serializedObject.FindProperty("CaptureMode");

        TargetProp = serializedObject.FindProperty("Target");
        SegmentXProp = serializedObject.FindProperty("SegmentX");
        SegmentYProp = serializedObject.FindProperty("SegmentY");

        SubSampleProp = serializedObject.FindProperty("SubSample");
        MiniDistanceProp = serializedObject.FindProperty("MiniDistance");

        PLYNormalProp = serializedObject.FindProperty("PLYNormal");
    }

    private Texture2D logo;

    Color color_default;
    public override void OnInspectorGUI()
    {
        color_default = GUI.color;
        if (Manager == null) Manager = (FMPCManager)target;

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

        
        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
        GUI.skin.button.alignment = TextAnchor.MiddleCenter;

        if (Manager.Cam == null) Manager.Cam = Manager.gameObject.GetComponent<Camera>();

        //if (Manager.ModeCapture)
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginVertical("box");
                if (!Manager.Cam.orthographic)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" Captured Points: " + Manager.FMM.vertices.Count) ;
                    EditorGUILayout.PropertyField(DebugModeProp, new GUIContent("DebugMode"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" Projection: Perspective");
                    float HFOV = 2 * Mathf.Atan(Mathf.Tan(Manager.Cam.fieldOfView * Mathf.Deg2Rad / 2) * Manager.Cam.aspect) * Mathf.Rad2Deg;
                    GUILayout.Label(" Field of View (H): " + HFOV);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" Captured Points: " + Manager.FMM.vertices.Count);
                    EditorGUILayout.PropertyField(DebugModeProp, new GUIContent("DebugMode"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" Projection: Orthographic");
                    GUILayout.Label(" Orthographic Size: " + Manager.Cam.orthographicSize);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();

            {
                GUILayout.BeginVertical("box");

                if (!Application.isPlaying)
                {
                    GUILayout.BeginVertical("box");
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.yellow;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" Available on Runtime Only", style);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }


                GUILayout.BeginHorizontal();
                GUILayout.Label(" Step 1:  Capture Points");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(CaptureModeProp, new GUIContent(" Capture Mode"));
                GUILayout.EndHorizontal();

                if (Manager.CaptureMode == PCCaptureMode.Object)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Capture"))
                    {
                        Manager.Action_CaptureObject();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(TargetProp, new GUIContent(" Target Object"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(SegmentXProp, new GUIContent(" Segment X"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(SegmentYProp, new GUIContent(" Segment Y"));
                    GUILayout.EndHorizontal();
                }
                if (Manager.CaptureMode == PCCaptureMode.Scene)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Capture"))
                    {
                        Manager.Action_CaptureWorld();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(SegmentXProp, new GUIContent("Segment X"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(SegmentYProp, new GUIContent("Segment Y"));
                    GUILayout.EndHorizontal();
                }

                if (Manager.CaptureMode == PCCaptureMode.Manual)
                {
                    GUILayout.BeginHorizontal();
                    if (!Manager.IsCapturing && !Manager.IsOptimising)
                    {
                        if (Manager.FMM.vertices.Count == 0)
                        {
                            GUI.color = Color.yellow;
                            if (GUILayout.Button("Capture"))
                            {
                                Manager.Action_Capture();
                                LastCaptureCount = Manager.FMM.vertices.Count;
                            }
                            GUI.color = color_default;
                        }
                        else
                        {
                            GUI.color = color_default;
                            if (GUILayout.Button("Capture: Add Points"))
                            {
                                Manager.Action_Capture();
                                LastCaptureCount = Manager.FMM.vertices.Count;
                            }
                            GUI.color = color_default;
                        }

                    }
                    else
                    {
                        if (Manager.IsOptimising)
                        {
                            GUI.color = Color.grey;
                            if (GUILayout.Button("Capture (Not Ready: Optimising Points)")) { }
                            GUI.color = color_default;
                        }
                        else
                        {
                            GUI.color = Color.green;

                            int step = ((int)Mathf.Pow(2, Manager.SubSample));
                            int TargetVertices = (Manager.Cam.pixelWidth / step) * (Manager.Cam.pixelHeight / step);
                            int CaptureProgress = (int)(((float)(Manager.FMM.vertices.Count - LastCaptureCount) / (float)TargetVertices) * 100f);
                            if (GUILayout.Button("Stop: Capture ( processing: " + CaptureProgress.ToString() + "% )"))
                            {
                                Manager.IsCapturing = false;
                                LastCaptureCount = Manager.FMM.vertices.Count;
                            }
                            GUI.color = color_default;
                        }
                    }
                    GUILayout.EndHorizontal();
                }



                {
                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();
                    int step = ((int)Mathf.Pow(2, Manager.SubSample));
                    GUILayout.Label(" Sample: " + Manager.Cam.pixelWidth / step + " x " + Manager.Cam.pixelHeight / step);

                    GUILayout.Label("Screen: " + Manager.Cam.pixelWidth + " x " + Manager.Cam.pixelHeight);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    //SubSample = EditorGUILayout.IntSlider(" Subsample", Manager.SubSample, 0, 8);
                    EditorGUILayout.PropertyField(SubSampleProp, new GUIContent(" SubSample"));
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
            }

            {
                GUILayout.BeginVertical("box");

                GUILayout.BeginHorizontal();
                GUILayout.Label(" Step 2:  Reduce Points(Optional)");
                GUILayout.EndHorizontal();

                if (!Manager.IsOptimising)
                {

                    if (!Manager.IsCapturing)
                    {
                        GUILayout.BeginHorizontal();
                        if (Manager.FMM.vertices.Count > 0 && Manager.MiniDistance > 0f)
                        {
                            GUI.color = Color.yellow;
                            if (GUILayout.Button("Optimise")) Manager.Action_OptimisePoints();
                            GUI.color = color_default;
                        }
                        else
                        {
                            if (Manager.MiniDistance == 0f)
                            {
                                GUI.color = Color.grey;
                                if (GUILayout.Button("Optimise (Not Ready: Minimum Distance cannot be zero)")) { }
                                GUI.color = color_default;
                            }
                            else
                            {
                                GUI.color = Color.grey;
                                if (GUILayout.Button("Optimise (Not Ready: Empty Point)")) { }
                                GUI.color = color_default;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUI.color = Color.grey;
                        if (GUILayout.Button("Optimise: Reduce Points (Not Ready: Capturing)")) { }
                        GUI.color = color_default;
                        GUILayout.EndHorizontal();
                    }

                    {
                        GUILayout.BeginVertical("box");
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(" Remove Points by Minimum Distance");
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(MiniDistanceProp, new GUIContent(" MiniDistance"));
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = Color.green;
                    if (GUILayout.Button("Stop: Optimise ( Processing: " + (int)(Manager.progress * 100f) + "% )"))
                    {
                        Manager.IsOptimising = false;
                    }
                    GUI.color = color_default;
                    GUILayout.EndHorizontal();


                    {
                        GUILayout.BeginVertical("box");
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(" Processor: " + Manager.TaskThreadCount);

                        int hours = Mathf.FloorToInt(Manager.TimeLeft / 3600);
                        int minutes = Mathf.FloorToInt((Manager.TimeLeft % 3600) / 60);
                        int seconds = Mathf.FloorToInt(Manager.TimeLeft % 60);
                        String TimeLeftStr = hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
                        GUILayout.Label(" Estimated: " + TimeLeftStr);
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        Manager.MiniDistance = EditorGUILayout.Slider("Minimum Distance", Manager.MiniDistance, 0f, 10f);
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndVertical();

            }

            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label(" Step 3:  Save Mesh");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (Manager.FMM.vertices.Count > 0)
                {
                    if (!Manager.IsOptimising && !Manager.IsCapturing)
                    {
                        if (GUILayout.Button("Save")) Manager.Action_SavePointCloud();
                    }
                    else
                    {
                        if (Manager.IsCapturing)
                        {
                            GUI.color = Color.grey;
                            if (GUILayout.Button("Save (Not Ready: Capturing)")) { }
                            GUI.color = color_default;
                        }
                        else if (Manager.IsOptimising)
                        {
                            GUI.color = Color.grey;
                            if (GUILayout.Button("Save (Not Ready: Optimising)")) { }
                            GUI.color = color_default;
                        }
                    }
                }
                else
                {
                    GUI.color = Color.grey;
                    if (GUILayout.Button("Save (Not Ready: Empty Point)")) { }
                    GUI.color = color_default;

                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label(" Step 4:  Export PLY file (Optional)");
                GUILayout.EndHorizontal();

                {
                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(" Export with Normals");
                    EditorGUILayout.PropertyField(PLYNormalProp, new GUIContent(""));
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

                GUILayout.BeginHorizontal();
                if (Manager.FMM.vertices.Count > 0)
                {
                    if (!Manager.IsOptimising && !Manager.IsCapturing)
                    {
                        if (GUILayout.Button("Export")) Manager.Action_ExportPLY();
                    }
                    else
                    {
                        if (Manager.IsCapturing)
                        {
                            GUI.color = Color.grey;
                            if (GUILayout.Button("Export (Not Ready: Capturing)")) { }
                            GUI.color = color_default;
                        }
                        else if (Manager.IsOptimising)
                        {
                            GUI.color = Color.grey;
                            if (GUILayout.Button("Export (Not Ready: Optimising)")) { }
                            GUI.color = color_default;
                        }
                    }
                }
                else
                {
                    GUI.color = Color.grey;
                    if (GUILayout.Button("Export (Not Ready: Empty Point)")) { }
                    GUI.color = color_default;

                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("New Capture"))
                {
                    Manager.ModeCapture = false;
                    Manager.Action_StopCapture();
                    Manager.Action_InitCapture();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
        GUI.color = color_default;


        serializedObject.ApplyModifiedProperties();
    }
}
