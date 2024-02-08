using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using static UnityEngine.InputManagerEntry;
using System.Drawing;
using System.Diagnostics.Eventing.Reader;
using NUnit;
using Unity.Mathematics.Geometry;

//[BurstCompile]//For test without burst, just remove this flag.

public struct EdgeDef
{
    public float4 Edge0;
    public float4 Edge1;
    public float4 Edge2;
    public int Material0;
    public int Material1;
    public int Material2;
}

//[BurstCompile]
public struct MarchingCubes_PreCalcCubeIndices : IJobParallelFor
{
    //IN
    [ReadOnly] public NativeArray<VVoxel> VoxelsData;
    [ReadOnly] public Vector3 chunkPos;

    //OUT
    [WriteOnly] public NativeArray<int> cubeIndex;

    void IJobParallelFor.Execute(int index)
    {
        int3 actCube = Constants.IndexToXyz(index);
        if (actCube.x < Constants.CHUNK_SIZE-1 && actCube.y < Constants.CHUNK_SIZE-1 && actCube.z < Constants.CHUNK_SIZE-1)
        {
            NativeArray<VCube> cube = new(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            /*
                cube[0] = GetVoxelDataInChunk(actCube.x, actCube.y, actCube.z);
            cube[1] = GetVoxelDataInChunk(actCube.x + 1, actCube.y, actCube.z);
            cube[2] = GetVoxelDataInChunk(actCube.x + 1, actCube.y, actCube.z + 1);
            cube[3] = GetVoxelDataInChunk(actCube.x, actCube.y, actCube.z + 1);
            cube[4] = GetVoxelDataInChunk(actCube.x, actCube.y + 1, actCube.z);
            cube[5] = GetVoxelDataInChunk(actCube.x + 1, actCube.y + 1, actCube.z);
            cube[6] = GetVoxelDataInChunk(actCube.x + 1, actCube.y + 1, actCube.z + 1);
            cube[7] = GetVoxelDataInChunk(actCube.x, actCube.y + 1, actCube.z + 1);
             */

            cube[0] = GetVoxelDataInChunk(actCube.x,     actCube.y,     actCube.z);
            cube[1] = GetVoxelDataInChunk(actCube.x + 1, actCube.y,     actCube.z);
            cube[2] = GetVoxelDataInChunk(actCube.x + 1, actCube.y,     actCube.z + 1);
            cube[3] = GetVoxelDataInChunk(actCube.x,     actCube.y,     actCube.z + 1);

            cube[4] = GetVoxelDataInChunk(actCube.x,     actCube.y + 1, actCube.z);
            cube[5] = GetVoxelDataInChunk(actCube.x + 1, actCube.y + 1, actCube.z);
            cube[6] = GetVoxelDataInChunk(actCube.x + 1, actCube.y + 1, actCube.z + 1);
            cube[7] = GetVoxelDataInChunk(actCube.x,     actCube.y + 1, actCube.z + 1);

            int cubeI = math.select(0, 1, cube[0].Weight > 0);
            cubeI |= math.select(0, 2, cube[1].Weight > 0);
            cubeI |= math.select(0, 4, cube[2].Weight > 0);
            cubeI |= math.select(0, 8, cube[3].Weight > 0);
            cubeI |= math.select(0, 16, cube[4].Weight > 0);
            cubeI |= math.select(0, 32, cube[5].Weight > 0);
            cubeI |= math.select(0, 64, cube[6].Weight > 0);
            cubeI |= math.select(0, 128, cube[7].Weight > 0);

            cubeIndex[index] = cubeI;
            
            if (actCube.x == 0 && actCube.y == 0 && actCube.z == 0)
            {
                Debug.Log("PreCalc cubeIndex : Index :" + index + " cubeIndex : " + cubeI);
            }
            if (actCube.x == 0 && actCube.y == 0 && actCube.z == 1)
            {
                Debug.Log("PreCalc cubeIndex : Index :" + index + " cubeIndex : " + cubeI);
            }

            cube.Dispose();
        }
        // else nothing to do
    }
    #region helpMethods
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ix"></param>
    /// <param name="iy"></param>
    /// <param name="iz"></param>
    /// <returns></returns>
    public VCube GetVoxelDataInChunk(int ix, int iy, int iz)
    {
        int index = Constants.XyzToIndex(ix, iy, iz);
        return new VCube
        {
            Vertex = new Vector3
                (
                    ix - Constants.CHUNK_HALFSIZE + chunkPos.x,
                    iy - Constants.CHUNK_HALFSIZE + chunkPos.y,
                    iz - Constants.CHUNK_HALFSIZE + chunkPos.z
                ),
            Material = VoxelsData[index].material,
            Weight = VoxelsData[index].weight
        };
    }
    #endregion
}

//[BurstCompile]
public struct MarchingCubes_PreCalcEdges : IJobParallelFor
{
    //IN
    [ReadOnly] public NativeArray<VVoxel> VoxelsData;
    [ReadOnly] public Vector3 chunkPos;

    //OUT
    [WriteOnly] public NativeArray<EdgeDef> edges;

    void IJobParallelFor.Execute(int index)
    {
        int3 actCube = Constants.IndexToXyz(index);       
        
        EdgeDef epv = new EdgeDef();
        //Debug.Log("PreCalcEdges:" + actCube);
        /*
        NativeArray<VCube> vCubes = new NativeArray<VCube>(4,Allocator.Temp);
        vCubes[0] = GetVoxelDataInChunk(actCube.x, actCube.y, actCube.z);
        vCubes[1] = GetVoxelDataInChunk(actCube.x + 1, actCube.y, actCube.z);
        vCubes[2] = GetVoxelDataInChunk(actCube.x, actCube.y + 1, actCube.z);
        vCubes[3] = GetVoxelDataInChunk(actCube.x, actCube.y, actCube.z + 1);
        */
        VCube v0 = GetVoxelDataInChunk(actCube.x, actCube.y, actCube.z);
        VCube v1 = GetVoxelDataInChunk(actCube.x + 1, actCube.y, actCube.z);
        VCube v2 = GetVoxelDataInChunk(actCube.x, actCube.y + 1, actCube.z);
        VCube v3 = GetVoxelDataInChunk(actCube.x, actCube.y, actCube.z + 1);
        
        int a0 = math.select(0, 1, v0.Weight <= 0) | math.select(0, 1, v1.Weight <= 0);
        int a1 = math.select(0, 1, v0.Weight <= 0) | math.select(0, 1, v2.Weight <= 0);
        int a2 = math.select(0, 1, v0.Weight <= 0) | math.select(0, 1, v3.Weight <= 0);

        if (a0 == 0 || a0 == 3) a0 = -2;
        else a0 = -1;
        if (a1 == 0 || a1 == 3) a1 = -2;
        else a1 = -1;
        if (a2 == 0 || a2 == 3) a2 = -2;
        else a2 = -1;

        epv.Edge0 = new float4( InterporlateVertex(v0, v1),a0);
        epv.Edge1 = new float4( InterporlateVertex(v0, v2),a1);
        epv.Edge2 = new float4( InterporlateVertex(v0, v3),a2);

        epv.Material0 = v0.Weight > 0 ? v0.Material : v1.Material;
        epv.Material1 = v0.Weight > 0 ? v0.Material : v2.Material;
        epv.Material2 = v0.Weight > 0 ? v0.Material : v3.Material;

        edges[index] = epv;
        if ( (actCube.x == 0 && actCube.y == 0 && actCube.z == 0) || (actCube.x == 0 && actCube.y == 0 && actCube.z == 1))
        {
            Debug.Log("PreCalc Edges 3perVox : Index :" + index + " Edge0 : " + epv.Edge0);
            Debug.Log("PreCalc Edges 3perVox : Index :" + index + " Edge1 : " + epv.Edge1);
            Debug.Log("PreCalc Edges 3perVox : Index :" + index + " Edge2 : " + epv.Edge2);
        
            
        }
    }
    #region helpMethods
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ix"></param>
    /// <param name="iy"></param>
    /// <param name="iz"></param>
    /// <returns></returns>
    public VCube GetVoxelDataInChunk(int ix, int iy, int iz)
    {
        int index = Constants.XyzToIndex(ix, iy, iz);
        VCube v = new VCube()
        {
            Vertex = new Vector3
                (
                    ix - Constants.CHUNK_HALFSIZE + chunkPos.x,
                    iy - Constants.CHUNK_HALFSIZE + chunkPos.y,
                    iz - Constants.CHUNK_HALFSIZE + chunkPos.z
                    //ix,
                    //iy,
                    //iz
                ),
        };

        if (ix >= Constants.CHUNK_SIZE || iy >= Constants.CHUNK_SIZE || iz >= Constants.CHUNK_SIZE)
        {
            v.Material = -1;
            v.Weight = -1;
        }
        else
        {
            v.Material = VoxelsData[index].material;
            v.Weight = VoxelsData[index].weight;
        }
        return v;
    }

    public float3 interporlateVertex2(float4 p1, float4 p2, out float interpolation)
    {
        interpolation = (0 - p1.w) / (p2.w - p1.w);
        return math.lerp(new float3(p1.x, p1.y, p1.z), new float3(p2.x, p2.y, p2.z), interpolation);
    }

    /// <summary>
    /// Calculate a point between two vertex using the weight of each vertex , used in interpolation voxel building.
    /// </summary>
    public float3 InterporlateVertex(VCube p1, VCube p2)
    {
        float3 ret;
        float interpolation = 10f;
        if (p2.Weight - p1.Weight != 0)
        {
            interpolation = (-p1.Weight) / (p2.Weight - p1.Weight);
            ret = math.lerp(p1.Vertex, p2.Vertex, interpolation);
        }
        else ret = new float3(3.33f,4.44f,5.55f);
        return ret;
    }


    #endregion
}
//[BurstCompile]
public struct MarchinCubes_SerializePreCalcEdges : IJob
{
    //IN
    [ReadOnly] public NativeArray<EdgeDef> edgesIn;
    [ReadOnly] public int ArraySize;

    //OUT
    [WriteOnly] public NativeArray<int3> edgesOut;
    [WriteOnly] public NativeList<float3> vertices;
    [WriteOnly] public NativeList<float3> uvw;

    //private int indexCount;
    void IJob.Execute()
    {
        int indexCount = -1;

        for (int i = 0; i < edgesIn.Length-1; i++) 
        {
            int3 eOut;

            eOut.x = TryAdd(edgesIn[i].Edge0, edgesIn[i].Material0, ref indexCount);
            eOut.y = TryAdd(edgesIn[i].Edge1, edgesIn[i].Material1, ref indexCount);
            eOut.z = TryAdd(edgesIn[i].Edge2, edgesIn[i].Material2, ref indexCount);

            edgesOut[i] = eOut;  
        }        
    }

    private int TryAdd (float4 OneEdge, int OneMaterial, ref int indexCount)
    {
        if (OneMaterial > 0)
        {
            vertices.Add(new float3(OneEdge.x, OneEdge.y, OneEdge.z));
            uvw.Add(new float3(0, 0, (float)OneMaterial));
            indexCount++;
            return indexCount;
        }
        else return -1;
    }
}

//[BurstCompile]
public struct MarchinCubes_Finalize : IJob
{
    //IN
    [ReadOnly] public NativeArray<VVoxel> VoxelsData;
    [ReadOnly] public Vector3 chunkPos;

    [ReadOnly] public NativeArray<int3> edges;
    [ReadOnly] public NativeArray<int> cubeIndex;
    //OUT

    [WriteOnly] public NativeList<int3> indexList;

    void IJob.Execute()
    {
        for (int index =0;index< VoxelsData.Length;index++) 
        {
            int3 actCube = Constants.IndexToXyz(index);
            if (actCube.x < Constants.CHUNK_SIZE-1 && actCube.y < Constants.CHUNK_SIZE-1 && actCube.z < Constants.CHUNK_SIZE-1 )
            {
                //Debug.Log(actCube);
                int triJobTableOffset = cubeIndex[index] * 16;

                //triJobTableOffset = jobEdgeTable[cubeIndex[index]];

                NativeArray<int> edgeTable = new NativeArray<int>(12, Allocator.Temp);

                edgeTable[0] = edges[index].x;

                edgeTable[9] = edges[Constants.XyzToIndex(actCube.x + 1, actCube.y    , actCube.z    )].y;
                edgeTable[4] = edges[Constants.XyzToIndex(actCube.x    , actCube.y + 1, actCube.z    )].x;
                edgeTable[8] = edges[index].y;
                edgeTable[2] = edges[Constants.XyzToIndex(actCube.x    , actCube.y    , actCube.z + 1)].x;
                edgeTable[10] = edges[Constants.XyzToIndex(actCube.x + 1, actCube.y    , actCube.z + 1)].y;
                edgeTable[6] = edges[Constants.XyzToIndex(actCube.x, actCube.y+1, actCube.z + 1)].x;
                edgeTable[11] = edges[Constants.XyzToIndex(actCube.x, actCube.y, actCube.z + 1)].y;
                edgeTable[3] = edges[index].z;
                edgeTable[1] = edges[Constants.XyzToIndex(actCube.x+1, actCube.y, actCube.z)].z;
                edgeTable[5] = edges[Constants.XyzToIndex(actCube.x+1, actCube.y+1, actCube.z)].z;
                edgeTable[7] = edges[Constants.XyzToIndex(actCube.x, actCube.y+1, actCube.z)].z;
                if ((actCube.x == 0 && actCube.y == 0 && actCube.z == 0) )
                {
                    for (int j = 0; j <= 11; j++)
                    {

                        Debug.Log("EdgeTable : ["+j+"] : "+ edgeTable[j] +" : ");
                    }
                    
                   
                }

                int anz = jobTriTableCount[cubeIndex[index]] / 3;
                if (actCube.x == 0 && actCube.y == 0 && actCube.z == 0)  Debug.Log("Writing " + anz + " Triangles");
                for (int i = 0; i < jobTriTableCount[cubeIndex[index]]-1; i += 3)
                //for (int i = cubeIndex[index] * 16; jobTriTable[i] != -1; i+=3)
                //    for (int i = triJobTableOffset; jobTriTable[i] != -1; i += 3)
                    {
                    int3 TriIndex;

                    TriIndex.x = edgeTable[jobTriTable[triJobTableOffset + i + 0]];
                    TriIndex.y = edgeTable[jobTriTable[triJobTableOffset + i + 1]];
                    TriIndex.z = edgeTable[jobTriTable[triJobTableOffset + i + 2]];

                    indexList.Add(TriIndex);
                    if (actCube.x==0 && actCube.y==0 && actCube.z==0) Debug.Log("actCube : "+actCube+" Index : " + cubeIndex[index] +" TriIndex : "+TriIndex);
                    
                }
                edgeTable.Dispose();
            }
        }
    }
    #region mesh building tables
    //Mesh build tables
    public static readonly int[] jobEdgeTable = new int[]
    {
        0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
        0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
        0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
        0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
        0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
        0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
        0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
        0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
        0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
        0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
        0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
        0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
        0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
        0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
        0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
        0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
        0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0
    };

    public static readonly int[] jobTriTableCount = new int[]
    {
        0, 3, 3, 6, 3, 6, 6, 9,
        3, 6, 6, 9, 6, 9, 9, 6,
        3, 6, 6, 9, 6, 9, 9, 12,
        6, 9, 9, 12, 9, 12, 12, 9,
        3, 6, 6, 9, 6, 9, 9, 12,
        6, 9, 9, 12, 9, 12, 12, 9,
        6, 9, 9, 6, 9, 12, 12, 9,
        9, 12, 12, 9, 12, 15, 15, 6,
        3, 6, 6, 9, 6, 9, 9, 12,
        6, 9, 9, 12, 9, 12, 12, 9,
        6, 9, 9, 12, 9, 12, 12, 15,
        9, 12, 12, 15, 12, 15, 15, 12,
        6, 9, 9, 12, 9, 12, 6, 9,
        9, 12, 12, 15, 12, 15, 9, 6,
        9, 12, 12, 9, 12, 15, 9, 6,
        12, 15, 15, 12, 15, 6, 12, 3,
        3, 6, 6, 9, 6, 9, 9, 12,
        6, 9, 9, 12, 9, 12, 12, 9,
        6, 9, 9, 12, 9, 12, 12, 15,
        9, 6, 12, 9, 12, 9, 15, 6,
        6, 9, 9, 12, 9, 12, 12, 15,
        9, 12, 12, 15, 12, 15, 15, 12,
        9, 12, 12, 9, 12, 15, 15, 12,
        12, 9, 15, 6, 15, 12, 6, 3,
        6, 9, 9, 12, 9, 12, 12, 15,
        9, 12, 12, 15, 6, 9, 9, 6,
        9, 12, 12, 15, 12, 15, 15, 6,
        12, 9, 15, 12, 9, 6, 12, 3,
        9, 12, 12, 15, 12, 15, 9, 12,
        12, 15, 15, 6, 9, 12, 6, 3,
        6, 9, 9, 6, 9, 12, 6, 3,
        9, 6, 12, 3, 6, 3, 3, 0
    };


    public static readonly int[] jobTriTable = new int[]
    {
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1,

        3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1,
        3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1,
        3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1,
        9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,

        4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1,
        9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1,
        2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1,

        8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1,
        4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1,
        3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1,
        1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1,
        4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1,
        4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1,

        9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1,
        5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1,
        2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1,

        9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1,
        0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1,
        2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1,
        10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1,
        5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1,
        5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1,

        9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1,
        0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1,
        1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1,
        10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1,
        8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1,
        2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1,

        7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1,
        2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1,
        11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1,
        5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1,
        11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1,
        11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,

        10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1,
        9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1,
        5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1,

        2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1,
        5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1,
        6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1,
        3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1,
        6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1,

        5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1,
        10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1,
        6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1,
        8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1,
        7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1,

        3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1,
        5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1,
        0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1,
        9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1,
        8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1,
        5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1,
        0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1,
        6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1,

        10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1,
        10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1,
        8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1,
        1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1,
        0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1,

        10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1,
        3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1,
        6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1,
        9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1,
        8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1,
        3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1,
        6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,

        7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1,
        0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1,
        10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1,
        10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1,
        2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1,
        7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1,
        7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,

        2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1,
        2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1,
        1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1,
        11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1,
        8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1,
        0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1,
        7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,

        7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1,
        10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1,
        2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1,
        6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1,

        7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1,
        2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1,
        10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1,
        10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1,
        0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1,
        7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1,
        6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1,
        8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1,
        9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1,
        6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1,
        4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1,
        10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1,
        8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1,
        0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1,
        1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1,
        8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1,
        10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1,
        4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1,
        10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1,
        5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1,
        11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1,
        9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1,
        6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1,
        7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1,
        3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1,
        7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1,
        3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1,
        6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1,
        9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1,
        1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1,
        4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1,
        7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1,
        6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1,
        3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1,
        0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1,
        6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1,
        0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1,
        11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1,
        6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1,
        5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1,
        9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1,
        1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1,
        1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1,
        10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1,
        0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1,
        5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1,
        10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1,
        11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1,
        9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1,
        7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1,
        2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1,
        8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1,
        9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1,
        9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1,
        1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1,
        9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1,
        5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1,
        0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1,
        10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1,
        2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1,
        0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1,
        0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1,
        9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1,
        5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1,
        3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1,
        5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1,
        0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1,
        9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1,
        1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1,
        3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1,
        4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1,
        9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1,
        11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1,
        11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1,
        2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1,
        9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1,
        3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1,
        1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1,
        4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1,
        3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1,
        0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1,
        9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1,
        1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
    };


    public static readonly int[] jobCornerIndexAFromEdge = new int[]
    {
        0,        1,        2,        3,        4,        5,        6,        7,        0,        1,        2,        3
    };

    public static readonly int[] jobCornerIndexBFromEdge = new int[]
    {
        1,        2,        3,        0,        5,        6,        7,        4,        4,        5,        6,        7
    };
    #endregion
}

public struct MarchinCubes_flatten : IJob
{
    //IN
    [ReadOnly] public NativeList<int3> indexIn;
    //OUT
    [WriteOnly] public NativeList<int> indexOut;
    public void Execute()
    {
        for (int i = 0; i < indexIn.Length; i++)
        {
            indexOut.Add(indexIn[i].x);
            indexOut.Add(indexIn[i].y);
            indexOut.Add(indexIn[i].z);
        }
    }
}



