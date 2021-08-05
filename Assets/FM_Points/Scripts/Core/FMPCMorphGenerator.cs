using System.Collections;
using UnityEngine;

using UnityEngine.Rendering;
using System.Linq;

using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class FMPCMorphGenerator : MonoBehaviour
{
    private void Awake()
    {
        Application.runInBackground = true;
    }
    bool stop = false;

    string NameToSave = "PMeshMorph";
    public Mesh Mesh1;
    public Mesh Mesh2;

    public int MaximumPoints;

    [HideInInspector]
    public int VertChecked = 0;
    [HideInInspector]
    public int TaskThreadCount = 0;
    [HideInInspector]
    public int TaskThreadFinished = 0;

    bool[] SelectedGrp;
    public void RegisterSelectedList(int id) { SelectedGrp[id] = true; }
    public bool GetSelectedList(int id) { return SelectedGrp[id]; }

    public bool Different(float num1, float num2, float value)
    {
        return (Mathf.Abs(num1 - num2) > value) ? true : false;
    }

    [Header("Progress: Waiting(sec)")]
    public float TimeLeft = 0;
    [Range(0f, 1f)]
    public float progress = 0f;

    bool IsGenerating = false;
    object _asyncLock = new object();

    public void Action_MeshGeneration()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Runtime only!");
            return;
        }
        if (Mesh1 == null)
        {
            Debug.Log("Mesh1 is null");
            return;
        }
        if (Mesh2 == null)
        {
            Debug.Log("Mesh2 is null");
            return;
        }
        StartCoroutine(MeshGeneration());
    }

    //static Vector3[] SortListV3OnY(List<Vector3> data)
    //{
    //    if (data.Count < 2) return data.ToArray();  //Or throw error, or return null

    //    var min = data.OrderBy(x => x.y).First();
    //    var max = data.OrderByDescending(x => x.y).First();

    //    data.Remove(min);
    //    data.Remove(max);

    //    data.Insert(0, min);
    //    data.Add(max);
    //    return data.ToArray();
    //}


    IEnumerator MeshGeneration()
    {
        MaximumPoints = Mesh1.vertexCount > Mesh2.vertexCount ? Mesh1.vertexCount : Mesh2.vertexCount;

        if (IsGenerating) yield break;
        IsGenerating = true;
        stop = false;

        if(Mesh1.vertexCount < Mesh2.vertexCount)
        {
            Mesh tmpMesh = Mesh1;
            Mesh1 = Mesh2;
            Mesh2 = tmpMesh;
        }


        //=====================
        Vector3[] VertGrp1 = Mesh1.vertices;
        Vector3[] VertGrp2 = Mesh2.vertices;
        Vector3[] VertTarget = new Vector3[MaximumPoints];

        Color[] ColorGrp1 = Mesh1.colors;
        Color[] ColorGrp2 = Mesh2.colors;
        Color[] ColorTarget = new Color[MaximumPoints];

        Vector3[] NormGrp1 = Mesh1.normals.Length > 0 ? Mesh1.normals : new Vector3[MaximumPoints] ;
        Vector3[] NormGrp2 = Mesh2.normals.Length > 0 ? Mesh1.normals : new Vector3[MaximumPoints];

        Vector3[] VertBlend = new Vector3[MaximumPoints];
        Vector3[] NormBlend = new Vector3[MaximumPoints];

        Vector3[] NormTarget = new Vector3[MaximumPoints];
        //color2:rgb + different:w
        Vector4[] TangentTarget = new Vector4[MaximumPoints];

        int LengthGrp1 = VertGrp1.Length;
        int LengthGrp2 = VertGrp2.Length;

        //check dist
        SelectedGrp = new bool[LengthGrp2];
        VertChecked = 0;

        while (Loom.numThreads > Loom.maxThreads - 1) yield return null;
        TaskThreadFinished = 0;
        TaskThreadCount = Loom.maxThreads - Loom.numThreads;

        int step = Mathf.CeilToInt((float)MaximumPoints / (float)TaskThreadCount);

        for (int j = 0; j < TaskThreadCount; j++)
        {
            Loom.RunAsync(() =>
            {
                int _j = j;
                int _start_num = _j * step;
                int _maximum_num = Mathf.Clamp((_j + 1) * step, 0, MaximumPoints);

                for (int n = _start_num; n < _maximum_num && !stop; n++)
                {
                    int order1 = n % LengthGrp1;
                    int order2 = n % LengthGrp2;

                    int _closest_num = -1;
                    float _closest_dist = Vector3.Distance(VertGrp1[order1], VertGrp2[order2]);

                    int mode = n % 3;
                    //for (int i = 0; i < MaximumPoints && !stop; i++)
                    for (int i = 0; i < LengthGrp2 && !stop; i++)
                    {
                        int tmpOrder = i % LengthGrp2;
                        if (!GetSelectedList(tmpOrder))
                        {
                            //if (mode == 0)
                            //{
                            //    if (!Different(VertGrp1[order1].y, VertGrp2[tmpOrder].y, 0.1f))
                            //    {
                            //        float _dist = Vector3.Distance(VertGrp1[order1], VertGrp2[tmpOrder]);
                            //        if (_dist < _closest_dist)
                            //        {
                            //            _closest_dist = _dist;
                            //            _closest_num = tmpOrder;
                            //        }
                            //    }
                            //}
                            //else if (mode == 1)
                            //{
                            //    if (!Different(VertGrp1[order1].x, VertGrp2[tmpOrder].x, 0.1f))
                            //    {
                            //        float _dist = Vector3.Distance(VertGrp1[order1], VertGrp2[tmpOrder]);
                            //        if (_dist < _closest_dist)
                            //        {
                            //            _closest_dist = _dist;
                            //            _closest_num = tmpOrder;
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            //    if (!Different(VertGrp1[order1].z, VertGrp2[tmpOrder].z, 0.1f))
                            //    {
                            //        float _dist = Vector3.Distance(VertGrp1[order1], VertGrp2[tmpOrder]);
                            //        if (_dist < _closest_dist)
                            //        {
                            //            _closest_dist = _dist;
                            //            _closest_num = tmpOrder;
                            //        }
                            //    }
                            //}

                            //if (_closest_num != -1) break;

                            float _dist = Vector3.Distance(VertGrp1[order1], VertGrp2[tmpOrder]);
                            if (_dist < _closest_dist)
                            {
                                _closest_dist = _dist;
                                _closest_num = tmpOrder;
                            }

                        }
                    }

                    bool RandomAssigned = false;
                    if (_closest_num == -1)
                    {
                        for (int i = 0; i < LengthGrp2 && !stop; i++)
                        {
                            int tmpOrder = i % LengthGrp2;
                            if (!GetSelectedList(tmpOrder))
                            {
                                _closest_num = tmpOrder;
                                RandomAssigned = true;
                                break;
                            }
                        }
                        if (_closest_num == -1) _closest_num = order2;
                    }
                    RegisterSelectedList(_closest_num);

                    VertTarget[n] = VertGrp1[order1];
                    ColorTarget[n] = ColorGrp1[order1];
                    NormTarget[n] = NormGrp1[order1];

                    VertBlend[n] = VertGrp2[_closest_num] - VertGrp1[order1];
                    NormBlend[n] = NormGrp2[_closest_num] - NormGrp1[order1];

                    TangentTarget[n] = new Vector4(ColorGrp2[_closest_num].r, ColorGrp2[_closest_num].g, ColorGrp2[_closest_num].b, RandomAssigned ? 1:0);

                    if (VertBlend[n].magnitude > 0.1f) TangentTarget[n].w = 1f;
                    TangentTarget[n].w = VertBlend[n].y;

                    System.Threading.Interlocked.Increment(ref VertChecked);
                }
                Loom.QueueOnMainThread(() => { System.Threading.Interlocked.Increment(ref TaskThreadFinished); });
                System.Threading.Thread.Sleep(1);
            });
            yield return null;
        }

        float TimeStart = Time.realtimeSinceStartup;
        while (TaskThreadFinished < TaskThreadCount)
        {
            progress = (float)VertChecked / (float)MaximumPoints;
            if (Time.frameCount % 60 == 0) TimeLeft = ((Time.realtimeSinceStartup - TimeStart) / progress) * (1f - progress);
            yield return null;
        }

        Mesh BlendMesh = new Mesh();
        BlendMesh.name = NameToSave;
        BlendMesh.indexFormat = MaximumPoints > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        BlendMesh.vertices = VertTarget;
        BlendMesh.colors = ColorTarget;

        BlendMesh.normals = NormTarget;
        BlendMesh.tangents = TangentTarget;

        BlendMesh.SetIndices(Enumerable.Range(0, BlendMesh.vertexCount).ToArray(), MeshTopology.Points, 0);
        Vector3[] tangs2 = new Vector3[MaximumPoints];
        BlendMesh.AddBlendShapeFrame("blend1", 1f, VertBlend, NormBlend, tangs2);

        SaveMesh(BlendMesh, BlendMesh.name, true);
        IsGenerating = false;
    }

    private void OnApplicationQuit()
    {
        stop = true;
        StopAllCoroutines();
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

        Material material = new Material(Shader.Find("FMPCD/FMPCUnlitBlend"));
        AssetDatabase.CreateAsset(material, path_mat);
        //=====================================================================
        AssetDatabase.SaveAssets();


        //=====================================================================
        PCMesh.AddComponent<SkinnedMeshRenderer>();

        Mesh mesh_prefab = AssetDatabase.LoadAssetAtPath<Mesh>(path_mesh);
        Material mat_prefab = AssetDatabase.LoadAssetAtPath<Material>(path_mat);

        PCMesh.GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh_prefab;
        PCMesh.GetComponent<SkinnedMeshRenderer>().material = mat_prefab;

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
}
