
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;

public class VMeshBuilder 
{
    [Tooltip("Value from which the vertices are inside the figure")][Range(0, 255)]
    public int isoLevel = 0;
    [Tooltip("Allow to get a middle point between the voxel vertices in function of the weight of the vertices")]
    public bool interpolate = true;

    private Vector3 pos;

    /// <summary>
    /// Method that calculate cubes, vertex and mesh in that order of a chunk.
    /// </summary>
    /// <param name="chunkData"> data of the chunk</param>
    public Mesh BuildChunk(VVoxel[] chunkData,Vector3 Pos)
    {
        pos = Pos;
        Debug.Log("creating the Job");
        VBuildChunkJob buildChunkJob = new VBuildChunkJob()
        {
            VoxelsData  = new NativeArray<VVoxel>(chunkData, Allocator.TempJob),
            //VoxelsData = chunkData.data,
            isoLevel    = this.isoLevel,
            interpolate = this.interpolate,
            chunkPos    = this.pos,

            vertex  = new NativeList<Vector3>(500, Allocator.TempJob),
            indices = new NativeList<int>(500, Allocator.TempJob),            
            uvw     = new NativeList<Vector3>(500, Allocator.TempJob),
            
        };
        Debug.Log("starting Job");
        JobHandle jobHandle = buildChunkJob.Schedule();
        jobHandle.Complete();
        Debug.Log("Job Complete");
        Debug.Log("Mesh from Job result :");
        Debug.Log("Vertices : " + buildChunkJob.vertex.Length);
        Debug.Log("indices  : " + buildChunkJob.indices.Length);
        Debug.Log("uvw      : " + buildChunkJob.uvw.Length);

        //Get all the data from the jobs and use to generate a Mesh
        Mesh meshGenerated = new();
        Vector3[] meshVert = new Vector3[buildChunkJob.vertex.Length];
        

        Vector3[] meshVertices  = buildChunkJob.vertex.ToArray();
        //List<Vector3>meshUV     = buildChunkJob.uvw;
        int[]     meshTriangles = buildChunkJob.indices.ToArray();


        meshGenerated.SetVertices(meshVertices);
        //meshGenerated.
        //meshGenerated.SetUVs(0,meshUV);
        meshGenerated.SetTriangles(meshTriangles,0);
        meshGenerated.RecalculateNormals();
        meshGenerated.RecalculateTangents();

        Debug.Log("MeshGenerated result :");
        Debug.Log("Vertices : " + meshGenerated.vertices.Length);
        Debug.Log("indices  : " + meshGenerated.triangles.Length);
        Debug.Log("uvw      : " + meshGenerated.uv.Length);

        //Dispose (Clear the jobs NativeLists)
        buildChunkJob.vertex.Dispose();
        buildChunkJob.indices.Dispose();
        buildChunkJob.uvw.Dispose();
        buildChunkJob.VoxelsData.Dispose();

        return meshGenerated;
    }
}
