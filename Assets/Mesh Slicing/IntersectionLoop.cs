using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionLoop
{
    public List<Vector3> verts;
    public Vector3 center;

    public IntersectionLoop(List<int> index, List<Edge> edges)
    {
        verts = new List<Vector3>();
        for (int k = 0; k < index.Count; k++)
        {
            //Debug.DrawRay(edges[index[k]].start, -Vector3.forward, Color.red, 100f);
            verts.Add(edges[index[k]].start);
            //Debug.DrawRay(edges[index[k]].end, -Vector3.forward * 2, Color.green, 100f);

            Debug.DrawRay(edges[index[k]].start, new Vector3(edges[index[k]].end.x, edges[index[k]].end.y, edges[index[k]].end.z+.1f)- edges[index[k]].start, Color.yellow, 100f);
            Debug.DrawRay(edges[index[k]].end, new Vector3(edges[index[k]].end.x, edges[index[k]].end.y, edges[index[k]].end.z + .1f) - edges[index[k]].end, Color.yellow, 100f);
            verts.Add(edges[index[k]].end);
            center += edges[index[k]].start;
            center += edges[index[k]].end;
        }
        center /= index.Count * 2;
    }

    public IntersectionLoop(List<Vector3> verts)
    {
        this.verts = verts;
        Color[] colors = { Color.green, Color.red, Color.yellow, Color.magenta, Color.blue, Color.cyan, Color.black, Color.gray, Color.white };
        for (int k = 0; k < verts.Count; k++)
        {
            center += verts[k];
        }
        center /= verts.Count;
    }
}