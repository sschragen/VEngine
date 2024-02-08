using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public struct VChunkData
{
    public Vector3Int chunkPos;
    public VVoxel[] data;

    public VChunkData(Vector3Int newPos)
    {
        data = new VVoxel[Constants.CHUNK_DATA_SIZE];
            //new NativeArray<VVoxel>(Constants.CHUNK_DATA_SIZE, Allocator.Persistent);
        chunkPos = newPos;
        Debug.Log("VCHunkData created.");
    }

    public int GetIndexInChunk(int ix, int iy, int iz)
    {
        return (ix + iz * Constants.CHUNK_SIZE + iy * Constants.CHUNK_AREA);
    }
    public VCube GetVoxelDataInChunk(int ix, int iy, int iz)
    {
        int index = GetIndexInChunk(ix, iy, iz);
        return new VCube
        {
            Vertex = new Vector3
                (
                    ix - Constants.CHUNK_HALFSIZE + chunkPos.x,
                    iy - Constants.CHUNK_HALFSIZE + chunkPos.y,
                    iz - Constants.CHUNK_HALFSIZE + chunkPos.z
                ),
            Material = data[index].material,
            Weight = data[index].weight
        };
    }
}
