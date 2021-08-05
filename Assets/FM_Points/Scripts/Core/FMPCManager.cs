using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using System;
using System.IO;

using UnityEngine.Rendering;


#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Text;

[Serializable]
public class FMMesh
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<Color> colors = new List<Color>();
    public List<Vector3> normals = new List<Vector3>();
    public List<bool> ListRemove = new List<bool>();

    public void RemoveAt(int id)
    {
        vertices.RemoveAt(id);
        colors.RemoveAt(id);
        normals.RemoveAt(id);
        ListRemove.RemoveAt(id);
    }

    public void AddRemoveList(int id){ ListRemove[id] = true; }
    public bool CheckRemoveList(int id){ return ListRemove[id]; }
}

public enum PCCaptureMode { Object, Scene, Manual }
public enum PCDebugMode { Default, Normal, Depth }

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FMPCManager : MonoBehaviour {

    //public bool DebugMode = false;
    public PCDebugMode DebugMode = PCDebugMode.Default;
    public int DebugChannel = 1;
    public bool NeedCapture = false;
    public Camera Cam;

    public PCCaptureMode CaptureMode = PCCaptureMode.Object;

    [HideInInspector]
    public Material PCMat3D;
    [HideInInspector]
    public Material PCMatColor;
    [HideInInspector]
    public Texture2D PCMap3D;
    [HideInInspector]
    public Texture2D PCMapColor;

    private void Awake()
    {
        Application.runInBackground = true;

        IsFinished = true;

        IsCapturing = false;
        IsOptimising = false;
    }
    void Start()
    {
        if (Cam == null) Cam = this.GetComponent<Camera>();
        Cam.ResetProjectionMatrix();
        Cam.depthTextureMode = DepthTextureMode.DepthNormals;
    }

    private void Update()
    {
        if (Cam == null)
        {
            Cam = this.GetComponent<Camera>();
            Cam.depthTextureMode = DepthTextureMode.DepthNormals;
        }

        if (PCMat3D == null) PCMat3D = new Material(Shader.Find("Hidden/FMPCDepth"));
        if (PCMat3D.shader.name != "Hidden/FMPCDepth") PCMat3D = new Material(Shader.Find("Hidden/FMPCDepth"));


        if (PCMatColor == null) PCMatColor = new Material(Shader.Find("Hidden/FMPCColor"));
        if (PCMatColor.shader.name != "Hidden/FMPCColor") PCMatColor = new Material(Shader.Find("Hidden/FMPCColor"));
    }

    public GameObject Target;
    public int RotationCount = 0;
    public float DistInit = 0f;

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    public void Action_CaptureObject()
    {
        if (IsCapturingObject) return;

        if (!Application.isPlaying)
        {
            Debug.Log("Execute in Runtime Only");
            return;
        }
        if(Target == null)
        {
            Debug.Log("Missing Debug Object");
            return;
        }
        Action_StopCapture();
        Action_InitCapture();
        StartCoroutine(CaptureObject());
    }

    public void Action_CaptureWorld()
    {
        if (IsCapturingWorld) return;

        if (!Application.isPlaying)
        {
            Debug.Log("Execute in Runtime Only");
            return;
        }
        Action_StopCapture();
        Action_InitCapture();
        StartCoroutine(CaptureWorld());
    }

    [Range(1,32)]
    public int SegmentX = 4;
    [Range(1,32)]
    public int SegmentY = 4;

    bool IsCapturingObject = false;
    bool IsCapturingWorld = false;
    IEnumerator CaptureObject()
    {
        IsCapturingObject = true;
        Vector3 init_pos = transform.position;
        Vector3 pivot = Target.transform.position;
        float init_dist = Vector3.Distance(init_pos, pivot);

        float AX = 180f / (float)(SegmentX + 1);
        float AY = 360f / (float)(SegmentY);

        Vector2 CC = Vector2.one;
        int check=0;
        int max = (int)(SegmentX * SegmentY);
        while(check < max)
        {
            if (!IsCapturing && !IsOptimising)
            {
                CC.x++;
                Vector3 ang = Vector3.zero;
                ang.x = CC.x * AX - 90f;
                ang.y = CC.y * AY;

                transform.position = Target.transform.position + Target.transform.rotation * Quaternion.Euler(ang) * Vector3.forward * init_dist;
                transform.LookAt(Target.transform);

                Debug.DrawRay(Target.transform.position, Target.transform.rotation * Quaternion.Euler(ang) * Vector3.forward * init_dist, Color.blue, 1f);
                yield return new WaitForSeconds(0.1f);
                Action_Capture();

                if (CC.x >= SegmentX)
                {
                    CC.x = 0;
                    CC.y++;
                }

                check++;
                //Debug.Log(check);
            }
            yield return null;
        }
        IsCapturingObject = false;
    }

    IEnumerator CaptureWorld()
    {
        IsCapturingWorld = true;
        Quaternion init_rot = transform.rotation;

        float AX = 180f / (float)(SegmentX + 1);
        float AY = 360f / (float)(SegmentY);

        Vector2 CC = Vector2.one;
        int check = 0;
        int max = (int)(SegmentX * SegmentY);
        while (check < max)
        {
            if (!IsCapturing && !IsOptimising)
            {
                CC.x++;
                if (CC.x >= SegmentX)
                {
                    CC.x = 0;
                    CC.y++;
                }
                Vector3 ang = Vector3.zero;
                ang.x = CC.x * AX - 90f;
                ang.y = CC.y * AY;

                transform.rotation = Quaternion.Euler(ang) * init_rot;
                Debug.DrawLine(transform.position, transform.position+transform.forward*Cam.farClipPlane, Color.green);

                yield return null;
                yield return null;
                Action_Capture();

                check++;
                Debug.Log(check);
            }
            yield return null;
        }
        IsCapturingWorld = false;
    }

    Mesh PMesh;
    public FMMesh FMM = new FMMesh();

    [Range(0,8)]
    public int SubSample = 4;
    [Range(0, 10)]
    public float MiniDistance = 0.02f;

    public int VertCount = 0;

    [HideInInspector]
    public int TaskThreadCount = 0;
    [HideInInspector]
    public int TaskThreadFinished = 0;
    public bool IsFinished = false;

    [Header("Progress: Waiting(sec)")]
    public float TimeLeft = 0;
    [Range(0f, 1f)]
    public float progress = 0f;

    public bool ModeCapture = false;


    public bool IsCapturing = false;
    public bool IsOptimising = false;

    public void Action_InitCapture()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Execute in Runtime Only");
            return;
        }
        if (FMM.vertices != null) FMM.vertices.Clear();
        if (FMM.colors != null) FMM.colors.Clear();
        if (FMM.normals != null) FMM.normals.Clear();
        if (FMM.ListRemove != null) FMM.ListRemove.Clear();

        FMM.vertices = new List<Vector3>();
        FMM.colors = new List<Color>();
        FMM.normals = new List<Vector3>();
        FMM.ListRemove = new List<bool>();

        ModeCapture = true;
    }

    public void Action_Capture()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Execute in Runtime Only");
            return;
        }

        if (ModeCapture == false)
        {
            ModeCapture = true;
            Action_InitCapture();
        }

        Cam.ResetProjectionMatrix();

#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif

        if (!IsCapturing) NeedCapture = true;
    }

    public void Action_OptimisePoints()
    {
        StartCoroutine(OptimisePoints());
    }
    public void Action_StopCapture()
    {
        ModeCapture = false;
        NeedCapture = false;
        IsCapturing = false;
        IsOptimising = false;

        IsCapturingObject = false;
        IsCapturingWorld = false;

        StopCoroutine(OptimisePoints());

        StopAllCoroutines();
    }

    IEnumerator AddPoints()
    {
        IsCapturing = true;
        yield return null;

        int CamW = Cam.pixelWidth;
        int CamH = Cam.pixelHeight;

        Vector3 StartPosition = Cam.transform.position;
        Vector3 DirForward = Cam.transform.forward;
        Vector3 DirRight = Cam.transform.right;
        Vector3 DirUp = Cam.transform.up;
        float FarClipPlane = Cam.farClipPlane;

        int step = (int)Mathf.Pow(2, SubSample);
        if (!Cam.orthographic)
        {
            for (int i = 0; i < CamW; i += step)
            {
                for (int j = 0; j < CamH; j += step)
                {
                    Color pixel_color = PCMapColor.GetPixel(i, j);
                    Color pixel_3d = PCMap3D.GetPixel(i, j);

                    float depth = pixel_3d.a;

                    if (depth < 0.9999f && depth > 0)
                    {
                        Ray ray = Cam.ScreenPointToRay(new Vector3(i, j, 0));

                        Vector3 _dir = ray.direction;
                        float angle = Vector3.Angle(_dir, DirForward);

                        //depth + curve projection
                        _dir *= depth * FarClipPlane * (1f / (Mathf.Cos(Mathf.Deg2Rad * angle)));
                        Vector3 _pos = StartPosition + _dir;

                        FMM.vertices.Add(_pos);
                        FMM.colors.Add(pixel_color);
                        FMM.normals.Add(new Vector3(pixel_3d.r, pixel_3d.g, pixel_3d.b));
                        FMM.ListRemove.Add(false);

                        if (!IsCapturing) yield break;
                    }
                }
                if (i % 10 == 0) yield return null;
            }
        }
        else
        {
            float OrthographicSize = Cam.orthographicSize;
            float Aspect = Cam.aspect;
            for (int i = 0; i < CamW; i += step)
            {
                for (int j = 0; j < CamH; j += step)
                {
                    Color pixel_color = PCMapColor.GetPixel(i, j);
                    Color pixel_3d = PCMap3D.GetPixel(i, j);

                    float depth = pixel_3d.a;

                    if (depth < 0.9999f && depth > 0)
                    {
                        Vector3 ScreenOffset = Vector3.zero;
                        ScreenOffset += DirUp * OrthographicSize * (((float)j / CamH) - 0.5f) * 2f;
                        ScreenOffset += DirRight * (OrthographicSize * Aspect) * (((float)i / CamW) - 0.5f) * 2f;

                        Vector3 _pos = StartPosition + ScreenOffset + DirForward * depth * (FarClipPlane);
                        FMM.vertices.Add(_pos);
                        FMM.colors.Add(pixel_color);
                        FMM.normals.Add(new Vector3(pixel_3d.r, pixel_3d.g, pixel_3d.b));
                        FMM.ListRemove.Add(false);

                        if (!IsCapturing) yield break;
                    }
                }
                if (i % 10 == 0) yield return null;
            }
        }

        IsCapturing = false;
    }

    IEnumerator OptimisePoints()
    {
        if (IsOptimising) yield break;

        IsOptimising = true;
        if (MiniDistance > 0)
        {
            while (Loom.numThreads > Loom.maxThreads - 1) yield return null;

            TaskThreadFinished = 0;
            TaskThreadCount = Loom.maxThreads - Loom.numThreads;

            int VertChecked = 0;
            VertCount = FMM.vertices.Count;

            for (int k = 0; k < TaskThreadCount; k++)
            {
                while (Loom.numThreads > Loom.maxThreads - 1) yield return null;

                int _start = k * TaskThreadCount;
                Loom.RunAsync(() =>
                {
                    for (int i = _start; i < VertCount && ModeCapture && IsOptimising; i += TaskThreadCount)
                    {
                        System.Threading.Interlocked.Increment(ref VertChecked);
                        if (!FMM.CheckRemoveList(i))
                        {
                            Vector3 curVert = FMM.vertices[i];
                            for (int j = i + 1; j < VertCount && ModeCapture && IsOptimising; j++)
                            {
                                if (!FMM.CheckRemoveList(j))
                                {
                                    if (Vector3.Distance(curVert, FMM.vertices[j]) < MiniDistance) FMM.AddRemoveList(j);
                                }
                            }
                        }
                    }

                    Loom.QueueOnMainThread(() =>
                    {
                        System.Threading.Interlocked.Increment(ref TaskThreadFinished);
                        //Debug.Log(TaskThreadFinished);
                    });
                    System.Threading.Thread.Sleep(1);
                });
            }


            float TimeStart = Time.realtimeSinceStartup;
            while (TaskThreadFinished < TaskThreadCount)
            {
                progress = (float)VertChecked / (float)VertCount;
                if (Time.frameCount % 60 == 0) TimeLeft = ((Time.realtimeSinceStartup - TimeStart) / progress) * (1f - progress);
                yield return null;
            }
            Debug.Log("before: " + FMM.vertices.Count);
            for (int i = FMM.ListRemove.Count - 1; i > 0; i--)
            {
                if (FMM.ListRemove[i]) FMM.RemoveAt(i);
            }
            Debug.Log("after: " + FMM.vertices.Count);
        }
        IsOptimising = false;
    }

    public void Action_SavePointCloud()
    {
        PMesh = new Mesh();
        PMesh.name = "PMesh_" + FMM.vertices.Count;
        PMesh.indexFormat = FMM.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        PMesh.vertices = FMM.vertices.ToArray();
        //Debug.Log(PMesh.vertices.Length);

        PMesh.colors = FMM.colors.ToArray();
        PMesh.normals = FMM.normals.ToArray();
        PMesh.SetIndices(Enumerable.Range(0, FMM.vertices.Count).ToArray(), MeshTopology.Points, 0);
        PMesh.UploadMeshData(false);

        SaveMesh(PMesh, "PMesh", true);
    }
    public void Action_ExportPLY()
    {
        PMesh = new Mesh();
        PMesh.name = "PMesh_" + FMM.vertices.Count;
        PMesh.indexFormat = FMM.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        PMesh.vertices = FMM.vertices.ToArray();
        //Debug.Log(PMesh.vertices.Length);

        PMesh.colors = FMM.colors.ToArray();
        PMesh.normals = FMM.normals.ToArray();
        PMesh.SetIndices(Enumerable.Range(0, FMM.vertices.Count).ToArray(), MeshTopology.Points, 0);
        PMesh.UploadMeshData(false);
        SaveAsPly(PMesh, "PMesh");
    }

    void OnPreRender()
    {
        Shader.SetGlobalMatrix(Shader.PropertyToID("UNITY_MATRIX_IV"), Cam.cameraToWorldMatrix);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        if (NeedCapture)
        {
            NeedCapture = false;
            PCMat3D.SetFloat("_Debug", 0);


            //=============Color===============
            RenderTexture RTextureColor = RenderTexture.GetTemporary(source.width, source.height, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, RTextureColor, PCMatColor);

            DestroyImmediate(PCMapColor);
#if UNITY_COLORSPACE_GAMMA
            PCMapColor = new Texture2D(RTextureColor.width, RTextureColor.height, TextureFormat.RGBAFloat, false, false);
#else
            PCMapColor = new Texture2D(RTextureColor.width, RTextureColor.height, TextureFormat.RGBAFloat, false, true);
#endif
            Rect rect_color = new Rect(0, 0, RTextureColor.width, RTextureColor.height);
            PCMapColor.ReadPixels(rect_color, 0, 0);
            PCMapColor.Apply();
            //=============Color===============


            //=============3D data===============
            RenderTexture RTexture3D = RenderTexture.GetTemporary(source.width, source.height, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, RTexture3D, PCMat3D);

            DestroyImmediate(PCMap3D);
#if UNITY_COLORSPACE_GAMMA
            PCMap3D = new Texture2D(RTexture3D.width, RTexture3D.height, TextureFormat.RGBAFloat, false, false);
#else
            PCMap3D = new Texture2D(RTexture3D.width, RTexture3D.height, TextureFormat.RGBAFloat, false, true);
#endif
            Rect rect_3d = new Rect(0, 0, RTexture3D.width, RTexture3D.height);
            PCMap3D.ReadPixels(rect_3d, 0, 0);
            PCMap3D.Apply();
            //=============3D data===============

            //Graphics.Blit(RTexture3D, destination);

            RenderTexture.ReleaseTemporary(RTexture3D);
            RenderTexture.ReleaseTemporary(RTextureColor);
            StartCoroutine(AddPoints());
        }

        if(DebugMode == PCDebugMode.Default)
        {
            PCMat3D.SetFloat("_Debug", 0);
            Graphics.Blit(source, destination);
        }
        if (DebugMode == PCDebugMode.Normal)
        {

            PCMat3D.SetFloat("_Debug", 1);
            Graphics.Blit(source, destination, PCMat3D);
        }
        if (DebugMode == PCDebugMode.Depth)
        {

            PCMat3D.SetFloat("_Debug", 2);
            Graphics.Blit(source, destination, PCMat3D);
        }

    }

    private void OnDestroy()
    {
        Action_StopCapture();
    }
    private void OnApplicationQuit()
    {
        Action_StopCapture();
    }

    public void SaveMesh(Mesh mesh, string name, bool makeNewInstance)
    {
#if UNITY_EDITOR
        string SaveFolder = Path.Combine(Application.dataPath, "FM_Points_Prefabs");
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);

        //=====================================================================
        string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", FileUtil.GetProjectRelativePath(SaveFolder), name, "");

        name = Path.GetFileName(path);
        SaveFolder = Path.Combine(SaveFolder, name);
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);

        GameObject PCMesh = new GameObject();
        PCMesh.name = name;

        string path_mesh = Path.Combine(SaveFolder, name + "_mesh" + ".asset");
        path_mesh = FileUtil.GetProjectRelativePath(path_mesh);
        if (string.IsNullOrEmpty(path_mesh)) return;

        Mesh meshToSave = (makeNewInstance) ? Instantiate(mesh) as Mesh : mesh;
        AssetDatabase.CreateAsset(meshToSave, path_mesh);
        //=====================================================================
        string path_mat = Path.Combine(SaveFolder, name + "_mat" + ".mat");
        path_mat = FileUtil.GetProjectRelativePath(path_mat);
        if (string.IsNullOrEmpty(path_mat)) return;

        Material material = new Material(Shader.Find("FMPCD/FMPCUnlit"));
        AssetDatabase.CreateAsset(material, path_mat);
        //=====================================================================
        AssetDatabase.SaveAssets();

        //=====================================================================
        PCMesh.AddComponent<MeshFilter>();
        PCMesh.AddComponent<MeshRenderer>();

        Mesh mesh_prefab = AssetDatabase.LoadAssetAtPath<Mesh>(path_mesh);
        Material mat_prefab = AssetDatabase.LoadAssetAtPath<Material>(path_mat);

        PCMesh.GetComponent<MeshFilter>().mesh = mesh_prefab;
        PCMesh.GetComponent<MeshRenderer>().material = mat_prefab;

        PCMesh.AddComponent<FMPCHelper>();

        string path_prefab = Path.Combine(SaveFolder, name + "_prefab" + ".prefab");
        path_prefab = FileUtil.GetProjectRelativePath(path_prefab);

        // Create the new Prefab
#if UNITY_2018_1_OR_NEWER
        PrefabUtility.SaveAsPrefabAsset(PCMesh, path_prefab);
#else
        PrefabUtility.CreatePrefab(path_prefab, PCMesh, ReplacePrefabOptions.ReplaceNameBased);
#endif
#endif
    }

    public bool PLYNormal = true;
    //experiment feature
    void SaveAsPly(Mesh mesh, string name)
    {
#if UNITY_EDITOR
        string SaveFolder = Path.Combine(Application.dataPath, "FM_Points_Prefabs");
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
        string path = EditorUtility.SaveFilePanel("Export as PLY Format", FileUtil.GetProjectRelativePath(SaveFolder), name, "ply");


        string name_with_extension = Path.GetFileName(path);
        string name_without_extension = Path.GetFileNameWithoutExtension(path);
        SaveFolder = Path.Combine(SaveFolder, name_without_extension);
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
        string path_ply = Path.Combine(SaveFolder, name_with_extension);

        //Debug.Log("LittleEndian? : " + BitConverter.IsLittleEndian);
        BinaryWriter BWriter = new BinaryWriter(File.Open(path_ply, FileMode.Create));
        try
        {
            int _vertexCount = mesh.vertexCount;
            Vector3[] _vertices = mesh.vertices;
            Color32[] _colors = mesh.colors32;
            Vector3[] _normals = mesh.normals;

            bool _HasNormals = _normals.Length > 0;

            string _meta = "";
            _meta += "ply\n";
            _meta += "format binary_little_endian 1.0\n";
            _meta += "element vertex " + _vertexCount + "\n";
            _meta += "property float x\n";
            _meta += "property float y\n";
            _meta += "property float z\n";
            _meta += "property uchar red\n";
            _meta += "property uchar green\n";
            _meta += "property uchar blue\n";

            if (PLYNormal && _HasNormals)
            {
                _meta += "property float nx\n";
                _meta += "property float ny\n";
                _meta += "property float nz\n";
            }

            _meta += "end_header\n";
            BWriter.Write(Encoding.ASCII.GetBytes(_meta));

            for (int i = 0; i < _vertexCount; i++)
            {
                BWriter.Write(BitConverter.GetBytes(_vertices[i].x));
                BWriter.Write(BitConverter.GetBytes(_vertices[i].y));
                BWriter.Write(BitConverter.GetBytes(_vertices[i].z));
                BWriter.Write((byte)_colors[i].r);
                BWriter.Write((byte)_colors[i].g);
                BWriter.Write((byte)_colors[i].b);

                if (PLYNormal & _HasNormals)
                {
                    BWriter.Write(BitConverter.GetBytes(_normals[i].x));
                    BWriter.Write(BitConverter.GetBytes(_normals[i].y));
                    BWriter.Write(BitConverter.GetBytes(_normals[i].z));
                }
            }
        }
        catch (Exception exp)
        {
            Console.Write(exp.Message);
        }
        BWriter.Close();
        AssetDatabase.Refresh();
#endif
    }

}
