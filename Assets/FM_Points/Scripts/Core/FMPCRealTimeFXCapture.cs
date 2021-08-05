using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FMPCRealTimeFXCapture : MonoBehaviour
{
    [HideInInspector] public Camera targetCamera;
    private RenderTextureDescriptor renderTextureDescriptor;
    private RenderTexture rt;
    private RenderTexture rt_fx;

    [Range(1, 4096)]
    [SerializeField] int captureWidth = 256;
    [Range(1, 4096)]
    [SerializeField] int captureHeight = 256;

    private Mesh PMesh;

    [HideInInspector] public Material MatFMPCStream;
    public List<FMPCRealTimeFXRenderer> PCRenderers = new List<FMPCRealTimeFXRenderer>();

    //init when added component, or reset component
    void Reset()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.farClipPlane = 100;
        targetCamera.depthTextureMode = DepthTextureMode.DepthNormals;
        targetCamera.depth = -10;

        MatFMPCStream = new Material(Shader.Find("Hidden/FMPCRTFXCapture"));
        CheckResolution();

        PCRenderers = new List<FMPCRealTimeFXRenderer>();
        Action_AddRenderer();
    }

    public void Action_AddRenderer()
    {
        FMPCRealTimeFXRenderer pcRenderer = new GameObject("FMPCRenderer").AddComponent<FMPCRealTimeFXRenderer>();
        pcRenderer.transform.position = transform.position;
        pcRenderer.transform.rotation = transform.rotation;
        PCRenderers.Add(pcRenderer);
    }

    void CheckResolution()
    {
        int _captureWidth = captureWidth;
        int _captureHeight = captureHeight;
        _captureWidth = Mathf.Clamp(_captureWidth, 1, 8192);
        _captureHeight = Mathf.Clamp(_captureHeight, 1, 8192);

        if (rt == null)
        {
            rt = new RenderTexture(_captureWidth, _captureHeight, 32, RenderTextureFormat.ARGB64);
            rt.filterMode = FilterMode.Point;
            targetCamera.targetTexture = rt;
        }
        else
        {
            if (rt.width != _captureWidth || rt.height != _captureHeight)
            {
                targetCamera.targetTexture = null;
                DestroyImmediate(rt);
                rt = new RenderTexture(_captureWidth, _captureHeight, 32, RenderTextureFormat.ARGB64);
                rt.filterMode = FilterMode.Point;
                targetCamera.targetTexture = rt;
            }
        }
        if (targetCamera.depthTextureMode != DepthTextureMode.DepthNormals) targetCamera.depthTextureMode = DepthTextureMode.DepthNormals;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (rt_fx == null)
        {
            renderTextureDescriptor = source.descriptor;
            renderTextureDescriptor.width *= 2;
            rt_fx = RenderTexture.GetTemporary(renderTextureDescriptor);
        }
        else if (rt_fx.width != source.descriptor.width * 2)
        {
            rt_fx.Release();
            renderTextureDescriptor = source.descriptor;
            renderTextureDescriptor.width *= 2;
            rt_fx = RenderTexture.GetTemporary(renderTextureDescriptor);
        }
        Graphics.Blit(source, rt_fx, MatFMPCStream);


        PCRenderers = PCRenderers.Where(item => item != null).ToList();
        foreach (FMPCRealTimeFXRenderer pcRenderer in PCRenderers)
        {
            if (pcRenderer != null)
            {
                pcRenderer.Action_ProcessImage(rt_fx, targetCamera.farClipPlane, targetCamera.nearClipPlane, targetCamera.fieldOfView, targetCamera.aspect, targetCamera.orthographic, targetCamera.orthographicSize);
            }
        }

        Graphics.Blit(source, destination);

    }
    private void LateUpdate() { CheckResolution(); }
}
