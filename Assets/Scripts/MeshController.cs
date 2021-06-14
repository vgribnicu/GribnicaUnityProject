using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshController : MonoBehaviour
{
    public GameObject complexMesh;
    public GameObject simplifiedMesh;
    void Start()
    {
        MeshFilter meshFilter1 = complexMesh.GetComponent<MeshFilter>();
        ReverseNormals(meshFilter1.mesh);

        MeshFilter meshFilter2 = simplifiedMesh.GetComponent<MeshFilter>();
        ReverseNormals(meshFilter2.mesh);
        meshFilter2.mesh = SimplifyMesh(meshFilter2.mesh);
        simplifiedMesh.AddComponent<MeshCollider>();
        simplifiedMesh.GetComponent<MeshRenderer>().enabled = false;
    }
    
    private Mesh SimplifyMesh(Mesh sourceMesh)
    {
        float quality = 0.5f;
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
