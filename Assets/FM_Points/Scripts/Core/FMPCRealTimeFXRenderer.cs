using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System;


[ExecuteInEditMode]
[Serializable]
public class FMPCRealTimeFXRenderer : MonoBehaviour
{
    [SerializeField] private Color mainColor = new Color(1, 1, 1, 1);
    [Range(0.000001f, 100f)]
    [SerializeField] private float pointSize = 0.04f;
    [SerializeField] private bool applyDistance = true;

    public Color MainColor { get { return mainColor; } set { mainColor = value; } }
    public float PointSize { get { return pointSize; } set { pointSize = value; } }
    public bool ApplyDistance { get { return applyDistance; } set { applyDistance = value; } }

    private Mesh PMesh;
    private int PCWidth = 0;
    private int PCHeight = 0;
    [HideInInspector]public int PCCount = 0;
    [HideInInspector] public Material MatFMPCRTFXRender;

    //init when added component, or reset component
    void Reset()
    {
        MatFMPCRTFXRender = new Material(Shader.Find("Hidden/FMPCRTFXRender"));

        this.gameObject.AddComponent<MeshRenderer>().hideFlags = HideFlags.HideInInspector;
        this.gameObject.AddComponent<MeshFilter>().hideFlags = HideFlags.HideInInspector;
        GetComponent<MeshRenderer>().sharedMaterial = MatFMPCRTFXRender;
        GetComponent<MeshRenderer>().allowOcclusionWhenDynamic = false;
    }

    private void Update()
    {
        MatFMPCRTFXRender.color = mainColor;
        MatFMPCRTFXRender.SetFloat("_PointSize", pointSize);
        MatFMPCRTFXRender.SetFloat("_ApplyDistance", applyDistance ? 1f : 0f);
    }

    public void Action_ProcessImage(RenderTexture inputTexture, float farClipPlane, float nearClipPlane, float fieldOfView, float aspect, bool orthographic, float orthographicSize)
    {
        if (inputTexture.width / 2 != PCWidth || inputTexture.height != PCHeight)
        {
            PCWidth = inputTexture.width / 2;
            PCHeight = inputTexture.height;
            PCCount = PCWidth * PCHeight;

            if (PMesh != null) DestroyImmediate(PMesh);
            PMesh = new Mesh();
            PMesh.name = "PMesh_" + PCWidth + "x" + PCHeight;

            PMesh.indexFormat = PCCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            Vector3[] vertices = new Vector3[PCCount];
            for (int j = 0; j < PCHeight; j++)
            {
                for (int i = 0; i < PCWidth; i++)
                {
                    int index = (j * PCWidth) + i;
                    vertices[index].x = ((float)i / (float)PCWidth);
                    vertices[index].y = ((float)j / (float)PCHeight);
                    vertices[index].z = 0f;
                }
            }

            PMesh.vertices = vertices;

            PMesh.SetIndices(Enumerable.Range(0, PMesh.vertices.Length).ToArray(), MeshTopology.Points, 0);
            PMesh.UploadMeshData(false);

            PMesh.bounds = new Bounds(Vector3.zero, new Vector3(2, 2, 2) * farClipPlane);
            GetComponent<MeshFilter>().sharedMesh = PMesh;
            GetComponent<MeshRenderer>().sharedMaterial = MatFMPCRTFXRender;
            GetComponent<MeshRenderer>().allowOcclusionWhenDynamic = false;
        }

        if (MatFMPCRTFXRender != null) MatFMPCRTFXRender.mainTexture = inputTexture;

        MatFMPCRTFXRender.SetFloat("_NearClipPlane", nearClipPlane);
        MatFMPCRTFXRender.SetFloat("_FarClipPlane", farClipPlane);

        MatFMPCRTFXRender.SetFloat("_VerticalFOV", fieldOfView);
        MatFMPCRTFXRender.SetFloat("_Aspect", aspect);

        MatFMPCRTFXRender.SetFloat("_OrthographicProjection", (orthographic ? 1.0f : 0.0f));
        MatFMPCRTFXRender.SetFloat("_OrthographicSize", orthographicSize);

        MatFMPCRTFXRender.SetFloat("_PointSize", PointSize);
    }
}
