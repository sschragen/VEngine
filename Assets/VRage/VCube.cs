using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Describes one Voxel used in MC
/// float3 Vertex
/// int Material
/// float Weight
/// </summary>
public struct VCube
{
    public float3 Vertex;
    public int Material;
    public float Weight;
}