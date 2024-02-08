using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Constants
{
    #region configurable variables
    public const int CHUNK_SIZE = 4; //Number voxel per side

    #endregion

    # region auto-configurable variables
    public const int CHUNK_AREA = CHUNK_SIZE * CHUNK_SIZE;
    public const int CHUNK_DATA_SIZE = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
    public const int CHUNK_HALFSIZE = CHUNK_SIZE / 2;

    public const int MC_EDGES = (CHUNK_SIZE-1) * (CHUNK_SIZE-1) * (CHUNK_SIZE - 1);
    #endregion

    public static int XyzToIndex(int x, int y, int z)
    {
        return z * CHUNK_AREA + y * CHUNK_SIZE + x;
    }
    public static int3 IndexToXyz(int index)
    {
        int3 position = new int3(
            index % CHUNK_SIZE,
            index / CHUNK_SIZE % CHUNK_SIZE,
            index / (CHUNK_AREA)
        );
        return position;
    }
}




