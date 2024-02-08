using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// the representation of a single Point 
/// with the Components 
/// Vector3 
/// Material 
/// </summary>
public struct Point
{
    public Vector3 vertex;
    public int material;
}
public class TriangleMesh
{ 
    public List<Vector3> vertices;
    public List<int> indices;
    public List<Vector3> uvw;

    /// <summary>
    /// constructs a new clear mesh
    /// </summary>
    public TriangleMesh() 
    {
        vertices = new List<Vector3>();
        indices = new List<int>();
        uvw = new List<Vector3>();
    }

    /// <summary>
    /// adds one single Point to the mesh
    /// </summary>
    /// <param name="point">Point</param>
    /// <returns>
    /// the new Index of the Point <paramdef name="point"
    /// </returns>
    public int AddPoint (Point point) 
    {
        int index = vertices.IndexOf(point.vertex);
        if (index <0)
        {
            vertices.Add(point.vertex);
            uvw.Add(new Vector3(0,0, Convert.ToInt32(point.material)));
            index = vertices.Count-1;
        }
        indices.Add(index);
        return index;
    }

    /// <summary>
    /// Add a Triangle to the mesh
    /// </summary>
    /// <param name="p1">
    /// Point No 1 </param>
    /// <param name="p2">
    /// Point No 2 </param>
    /// <param name="p3">
    /// Point No 3 </param>
    /// /// <returns>
    /// void
    /// </returns>
    public void AddTriangle (Point p1, Point p2, Point p3)
    {
        AddPoint(p1);
        AddPoint(p2);
        AddPoint(p3);
    }
    /// <summary>
    /// Clears the mesh
    /// </summary>
    public void Clear()
    { 
        vertices.Clear();
        indices.Clear();
        uvw.Clear();
    }
}
