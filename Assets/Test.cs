using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteInEditMode]
public class Test : MonoBehaviour
{
    // Start is called before the first frame update

    public Boolean DrawNormal = false;
    public Color ColorNormal = Color.blue;    
    public Boolean DrawTangent = false;
    public Color ColorTangent = Color.yellow;
    public Boolean DrawBiTangent = false;
    public Color ColorBiTangent = Color.white;

    public Mesh mesh;

    private void OnDrawGizmos()
    {
        
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(Vector3.zero, .1f);

        if (DrawNormal)
        {
            Gizmos.color = ColorNormal;
            for (int i = 0; i < mesh.vertices.Length; i++) 
            {
                Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i]/2);   
            }
        }
        if (DrawTangent)
        {
            Gizmos.color = ColorTangent;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + (Vector3) mesh.tangents[i]/2);
            }
        }
        if (DrawBiTangent)
        {
            Gizmos.color = ColorBiTangent;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] 
                                + Vector3.Cross((Vector3)mesh.tangents[i], mesh.normals[i])/2);
            }
        }
    }
    void Awake ()
    {
        Generate();
    }
    void Start()
    {
    }

    void Generate()
    {
        
        mesh = new Mesh
        {
            name = "Procedural Mesh"
        };

        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,4),new Vector3(1,0,4),new Vector3(2,1,4),new Vector3(3,0,4),new Vector3(4,0,4),
            new Vector3(0,0,3),new Vector3(1,0,3),new Vector3(2,1,3),new Vector3(3,0,3),new Vector3(4,0,3),
            new Vector3(0,0,2),new Vector3(1,0,2),new Vector3(2,1,2),new Vector3(3,0,2),new Vector3(4,0,2),
            new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(2,1,1),new Vector3(3,0,1),new Vector3(4,0,1),
            new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(2,1,0),new Vector3(3,0,0),new Vector3(4,0,0),
        };
        mesh.triangles = new int[] 
        {
             0, 5, 1, 1, 5, 6,  1, 6, 2, 2, 6, 7,  2, 7, 3, 3, 7, 8,  3, 8, 4, 4, 8, 9,

             5,10, 6, 6,10,11,  6,11, 7, 7,11,12,  7,12, 8, 8,12,13,  8,13, 9, 9,13,14,

            10,15,11,11,15,16, 11,16,12,12,16,17, 12,17,13,13,17,18, 13,18,14,14,18,19,

            15,20,16,16,20,21, 16,21,17,17,21,22, 17,22,18,18,22,23, 18,23,19,19,23,24,

        };
        
        //mesh.vertices = vertices;
        //mesh.triangles = indices;
        mesh.RecalculateNormals();
        //

        Vector3[] uvw0 =
        {
            new Vector3(0,0,0),new Vector3(0,0,0),new Vector3(0,0,2),new Vector3(0,0,0),new Vector3(0,0,0),
            new Vector3(0,0,0),new Vector3(0,0,0),new Vector3(0,0,2),new Vector3(0,0,0),new Vector3(0,0,0),
            new Vector3(0,0,1),new Vector3(0,0,1),new Vector3(0,0,1),new Vector3(0,0,1),new Vector3(0,0,1),
            new Vector3(0,0,0),new Vector3(0,0,0),new Vector3(0,0,1),new Vector3(0,0,0),new Vector3(0,0,0),
            new Vector3(0,0,0),new Vector3(0,0,0),new Vector3(0,0,1),new Vector3(0,0,0),new Vector3(0,0,0)
        };
        mesh.SetUVs(0, uvw0);
        mesh.RecalculateTangents();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    Vector2 CalcUV_InTangentPlane_UV(Vector3 u, Vector3 v, Vector3 x)
    {
        return new Vector2(
            Vector3.Dot(x, u) / Vector3.Dot(u, u),
            Vector3.Dot(x, v) / Vector3.Dot(v, v)
        );
    }

    // Update is called once per frame
    void Update()
    {
        Generate();
    }
}
