using UnityEngine;
using UnityEditor;

public class MeshController : MonoBehaviour
{
    [SerializeField]
    private GameObject[] meshGameObjects;

    [SerializeField] public bool createSaveMeshCollider;

    public void Start()
    {
        if (!createSaveMeshCollider) return;
        foreach (var meshGameObject in meshGameObjects)
        {
            ModifyMeshGameObject(meshGameObject);
        }
    }

    private void ModifyMeshGameObject(GameObject complexMeshGameObject)
    {
        Mesh originalMesh = complexMeshGameObject.GetComponent<MeshFilter>().sharedMesh;

        Mesh simplifiedMesh = SimplifyMesh(originalMesh, 0.3f);

        complexMeshGameObject.AddComponent<MeshCollider>().sharedMesh = simplifiedMesh;
        
        var savePath = "Assets/Meshes/" + "colliderMesh" + ".asset";
        Debug.Log("Saved Mesh to:" + savePath);
        AssetDatabase.CreateAsset(simplifiedMesh, savePath);
        
    }
    
    private Mesh SimplifyMesh(Mesh sourceMesh, float quality)
    {
        var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
        meshSimplifier.Initialize(sourceMesh);
        meshSimplifier.SimplifyMesh(quality);
        return meshSimplifier.ToMesh();
    }
    private void ReverseNormals(Mesh sourceMesh)
    {
        if (sourceMesh != null)
        {
            Vector3[] normals = sourceMesh.normals;
            for (int i=0;i<normals.Length;i++)
                normals[i] = -normals[i];
            sourceMesh.normals = normals;
 
            for (int m=0;m<sourceMesh.subMeshCount;m++)
            {
                int[] triangles = sourceMesh.GetTriangles(m);
                for (int i=0;i<triangles.Length;i+=3)
                {
                    int temp = triangles[i + 0];
                    triangles[i + 0] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                sourceMesh.SetTriangles(triangles, m);
            }
        }	
    }
}
