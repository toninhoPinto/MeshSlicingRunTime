using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;

public class OrderedHashSet<T> : KeyedCollection<T, T>
{
    protected override T GetKeyForItem(T item)
    {
        return item;
    }
}

public class CutMeshV5 : MonoBehaviour
{

    public GameObject target;
    public GameObject prefabPart;
    Vector3 planeNormal;
    Vector3 planePoint;
    Mesh myMesh;

    List<List<Vector3>> upVerts;
    List<OrderedHashSet< Vector3>> uphashVerts;
    List<List<int>> upTris;

    List<List<Vector3>> downVerts;
    List<OrderedHashSet< Vector3>> downhashVerts;
    List<List<int>> downTris;

    List<Edge> centerEdges;

    void Start()
    {
        myMesh = GetComponent<MeshFilter>().mesh;
    }

    public void Cut()
    {
        if (target == null)
            return;
        CutMesh();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Cut();
        }
    }

    void CutMesh()
    {
        //SETUP QUAD VARIABLES==================================================
        planeNormal = transform.TransformVector(myMesh.normals[0]);
        planeNormal = planeNormal.normalized;
        planePoint = transform.TransformPoint(myMesh.vertices[0]);
        //==================================================

        Mesh targetMesh = target.GetComponent<MeshFilter>().mesh;

        int[] tris = targetMesh.triangles;
        Vector2[] uvs = targetMesh.uv;
        Vector3[] verts = targetMesh.vertices;
        Vector3[] normals = targetMesh.normals;

        upVerts = new List<List<Vector3>>();
        uphashVerts = new List<OrderedHashSet< Vector3>>();
        upTris = new List<List<int>>();

        downVerts = new List<List<Vector3>>();
        downhashVerts = new List<OrderedHashSet<Vector3>>();
        downTris = new List<List<int>>();

        centerEdges = new List<Edge>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 worldp1 = target.transform.TransformPoint(verts[tris[i]]);
            Vector3 worldp2 = target.transform.TransformPoint(verts[tris[i + 1]]);
            Vector3 worldp3 = target.transform.TransformPoint(verts[tris[i + 2]]);

            Vector2 uv1 = uvs[tris[i]];
            Vector2 uv2 = uvs[tris[i + 1]];
            Vector2 uv3 = uvs[tris[i + 2]];

            Vector3 normal1 = target.transform.TransformVector(normals[tris[i]]).normalized;
            Vector3 normal2 = target.transform.TransformVector(normals[tris[i + 1]]).normalized;
            Vector3 normal3 = target.transform.TransformVector(normals[tris[i + 2]]).normalized;
            bool[] intersected = DoesTriIntersectPlane(worldp1, worldp2, worldp3);

            if (intersected[0] || intersected[1] || intersected[2])
            {
                Vector2[] triUvs = { uv1, uv2, uv3 };
                Vector3[] triVerts = { worldp1, worldp2, worldp3 };
                Vector3[] triNormals = { normal1, normal2, normal3 };
                HandleTriIntersectionPoints(intersected, triVerts, triUvs, triNormals);
            }
            else
            {
                if (Mathf.Sign(Vector3.Dot(planeNormal, (worldp1 - planePoint))) > 0)
                {//above
                    FindSeparateMeshes(worldp1, worldp2, worldp3, upVerts, uphashVerts);
                }
                else
                {
                    FindSeparateMeshes(worldp1, worldp2, worldp3, downVerts, downhashVerts);
                }
            }
            
        }

        if (centerEdges.Count == 0)
        {
            return;
        }
        //List<List<int>> groupedVerts = CenterVertsIntoParts();
        //HandleIntersectedZone(upVerts, upTris, upUVs, upNormals, groupedVerts, true);
        //HandleIntersectedZone(downVerts, downTris, downUVs, downNormals, groupedVerts, false);
        CreateParts(upVerts, upTris);
        CreateParts(downVerts, downTris);
        Destroy(target);
    }

    void FindSeparateMeshes(Vector3 wp1, Vector3 wp2, Vector3 wp3, List<List<Vector3>> vertParts, List<OrderedHashSet< Vector3>> justForgiggles)
    {

        List<int> indexFound = new List<int>();
        for (int w = 0; w < vertParts.Count; w++)
        {
            if (justForgiggles[w].Contains(wp1) || justForgiggles[w].Contains(wp2) || justForgiggles[w].Contains(wp3))
            {
                indexFound.Add(w);
            }
        }

        if (indexFound.Count == 0)
        {
            vertParts.Add(new List<Vector3>() { wp1, wp2, wp3 });
            justForgiggles.Add(new OrderedHashSet<Vector3>() { wp1,  wp2, wp3 });
        }
        else
        {
            vertParts[indexFound[0]].Add(wp1);
            vertParts[indexFound[0]].Add(wp2);
            vertParts[indexFound[0]].Add(wp3);

            if(!justForgiggles[indexFound[0]].Contains(wp1))
                justForgiggles[indexFound[0]].Add(wp1);

            if (!justForgiggles[indexFound[0]].Contains(wp2))
                justForgiggles[indexFound[0]].Add( wp2);

            if (!justForgiggles[indexFound[0]].Contains(wp3))
                justForgiggles[indexFound[0]].Add( wp3);

            for (int k = indexFound.Count-1; k > 0; k--)
            {
                vertParts[indexFound[0]].AddRange(vertParts[indexFound[k]]);
                justForgiggles[indexFound[0]].Union(justForgiggles[indexFound[k]]);

                List<Vector3> tmp = vertParts[vertParts.Count-1];
                vertParts[vertParts.Count-1] = vertParts[indexFound[k]];
                vertParts[indexFound[k]] = tmp;
                vertParts.RemoveAt(vertParts.Count-1);
                justForgiggles.RemoveAt(indexFound[k]);
            }
        }
        indexFound.Clear();
    }

    bool MyContains(List<Vector3> list, Vector3 check)
    {
        for(int i = 0; i <list.Count; i++)
        {
            if (list[i] == check)
                return true;
        }
        return false;
    }

    List<List<int>> CenterVertsIntoParts()
    {
        bool[] visited = new bool[centerEdges.Count];

        int nextEdge = 0;
        int EdgeA = nextEdge;
        Vector3 start = centerEdges[nextEdge].start;
        int EdgeB = nextEdge;
        Vector3 end = centerEdges[nextEdge].end;
        visited[nextEdge] = true;

        List<List<int>> groupedEdgesConnected = new List<List<int>>();
        List<int> tmpEdgesConnected = new List<int>();
        tmpEdgesConnected.Add(nextEdge);
        bool finished = false;
        while (!finished)
        {
            for (int i = 0; i < centerEdges.Count; i++)
            {

                if (EdgeA.Equals(EdgeB) && tmpEdgesConnected.Count>1)// did a loop
                { 
                    groupedEdgesConnected.Add(tmpEdgesConnected);
                    finished = true;
                    for (int j = 0; j < visited.Length; j++)
                    {
                        finished &= visited[j];
                    }

                    if (finished)
                    {
                        return groupedEdgesConnected;
                    }

                    for (int j = 0; j < visited.Length; j++)
                    {
                        if (!visited[j])
                        {
                            nextEdge = j;
                            break;
                        }
                    }

                    EdgeA = nextEdge;
                    start = centerEdges[nextEdge].start;
                    EdgeB = nextEdge;
                    end = centerEdges[nextEdge].end;
                    tmpEdgesConnected = new List<int>();
                    visited[nextEdge] = true;
                    tmpEdgesConnected.Add(nextEdge);
                }

                if (visited[i])
                    continue;

                if (start == centerEdges[i].start || start == centerEdges[i].end)
                {
                    tmpEdgesConnected.Add(i);
                    EdgeA = i;
                    visited[EdgeA] = true;
                    if (start == centerEdges[i].start)
                        start = centerEdges[i].end;
                    else
                        start = centerEdges[i].start;
                }

                if (end == centerEdges[i].start || end == centerEdges[i].end)
                {
                    if (!visited[i])
                        tmpEdgesConnected.Add(i);
                    EdgeB = i;
                    visited[EdgeB] = true;
                    if (end == centerEdges[i].start)
                        end = centerEdges[i].end;
                    else
                        end = centerEdges[i].start;
                }


            }

        }


        return groupedEdgesConnected;
    }

    void HandleIntersectedZone(List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals, List<List<int>> centerGroups, bool top)
    {
        //Debug.Log(centerGroups.Count);
        for (int k = 0; k < centerGroups.Count; k++)
        {
            //Debug.Log(k);
            //Debug.Log(centerGroups[k].Count);
            List<int> centerTris = new List<int>();

            List<int> thisGroupEdges = centerGroups[k];
            List<Vector3> centerVerts = new List<Vector3>();
            Vector3 center = Vector3.zero;

            for (int i = 0; i < thisGroupEdges.Count; i++)
            {
                centerVerts.Add(centerEdges[thisGroupEdges[i]].start);
                centerVerts.Add(centerEdges[thisGroupEdges[i]].end);
                center += centerEdges[thisGroupEdges[i]].start;
                center += centerEdges[thisGroupEdges[i]].end;
            }
            center /= thisGroupEdges.Count *2;

            if (planeNormal.y != 0)
            {
                float normalDir = Mathf.Sign(planeNormal.y);
                centerVerts = centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).y, (x - center).x)).ToList();
            }
            else
            {
                float normalDir = Mathf.Sign(planeNormal.z);
                centerVerts = centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).z, (x - center).x)).ToList();
            }

            int sizeVertsBeforeCenter = partVerts.Count;
            partVerts.AddRange(centerVerts);
            partVerts.Add(center);

            if (top)
            {
                for (int i = sizeVertsBeforeCenter; i < partVerts.Count - 1; i++)
                {
                    centerTris.Add(i);
                    centerTris.Add(i + 1);
                    centerTris.Add(partVerts.Count - 1);
                }

                centerTris.Add(partVerts.Count - 2);
                centerTris.Add(sizeVertsBeforeCenter);
                centerTris.Add(partVerts.Count - 1);
            }
            else
            {
                for (int i = sizeVertsBeforeCenter; i < partVerts.Count - 1; i++)
                {
                    centerTris.Add(i);
                    centerTris.Add(partVerts.Count - 1);
                    centerTris.Add(i + 1);
                }

                centerTris.Add(partVerts.Count - 2);
                centerTris.Add(partVerts.Count - 1);
                centerTris.Add(sizeVertsBeforeCenter);
            }

            partTris.AddRange(centerTris);

            /*
            Vector3 normal;
            if (top)
                normal = topPart.transform.InverseTransformVector(-planeNormal);
            else
                normal = bottomPart.transform.InverseTransformVector(planeNormal);
            for (int i = sizeVertsBeforeCenter; i < partVerts.Count; i++)
            {
                partUvs.Add(new Vector2(0, 0));
                partNormals.Add(normal.normalized * 3);
            }
            */
        }
    }

    void CreateParts(List<List<Vector3>> partVerts, List<List<int>> partTris)
    {

        for (int i = 0; i < partVerts.Count; i++)
        {
            GameObject newPart = Instantiate(prefabPart);
            partTris.Add(new List<int>());
            for (int k = 0; k < partVerts[i].Count; k++)
            {
                partVerts[i][k] = newPart.transform.InverseTransformPoint(partVerts[i][k]);
                partTris[i].Add(k);
            }

            Mesh newPartMesh = newPart.GetComponent<MeshFilter>().mesh;
            newPartMesh.Clear();
            newPartMesh.vertices = partVerts[i].ToArray();
            newPartMesh.triangles = partTris[i].ToArray();
            newPartMesh.RecalculateBounds();
        }
    }


    bool[] DoesTriIntersectPlane(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, p1 - planePoint));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, p2 - planePoint));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, p3 - planePoint));

        bool[] intersections = { upOrDown != upOrDown2, upOrDown2 != upOrDown3, upOrDown != upOrDown3 };

        return intersections;
    }

    void HandleTriIntersectionPoints(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals)
    {
        List<Vector3> tmpUpVerts = new List<Vector3>();
        List<Vector3> tmpDownVerts = new List<Vector3>();
        bool secondLine = false;

        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, verts[0] - planePoint));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, verts[1] - planePoint));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, verts[2] - planePoint));

        Vector3[] newVectors = new Vector3[2];
        int newVectorIndex = 0;

        if (intersections[0])
        {
            newVectors[newVectorIndex] = AddToCorrectSideList(upOrDown, 0, 1, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
            newVectorIndex++;
        }
        if (intersections[1])
        {
            newVectors[newVectorIndex] = AddToCorrectSideList(upOrDown2, 1, 2, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
            newVectorIndex++;
        }
        if (intersections[2])
        {
            newVectors[newVectorIndex] = AddToCorrectSideList(upOrDown3, 2, 0, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }

        //only 2 new vectors in all cases
        centerEdges.Add(new Edge(newVectors[0], newVectors[1]));

        HandleTriOrder(tmpUpVerts, tmpDownVerts);
    }

    void HandleTriOrder(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts)
    {
        int upLastInsert = upVerts.Count;
        int downLastInsert = downVerts.Count;

        //FindSeparateMeshes(tmpDownVerts[0], tmpDownVerts[1], tmpDownVerts[2], downVerts, downhashVerts);
        //FindSeparateMeshes(tmpUpVerts[0], tmpUpVerts[1], tmpUpVerts[2], upVerts, uphashVerts);
    }

    void HandleBaryCentric(Vector3 newPoint, ref Vector2 newUV, ref Vector3 newNormal, Vector3[] points, Vector2[] uvs, Vector3[] normals)
    {
        Vector3 f1 = points[0] - newPoint;
        Vector3 f2 = points[1] - newPoint;
        Vector3 f3 = points[2] - newPoint;
        // calculate the areas and factors (order of parameters doesn't matter):
        float areaMainTri = Vector3.Cross(points[0] - points[1], points[0] - points[2]).magnitude; // main triangle area a
        float a1 = Vector3.Cross(f2, f3).magnitude / areaMainTri; // p1's triangle area / a
        float a2 = Vector3.Cross(f3, f1).magnitude / areaMainTri; // p2's triangle area / a 
        float a3 = Vector3.Cross(f1, f2).magnitude / areaMainTri; // p3's triangle area / a
        // find the uv corresponding to point f (uv1/uv2/uv3 are associated to p1/p2/p3):
        newNormal = normals[0] * a1 + normals[1] * a2 + normals[2] * a3;
        newUV = uvs[0] * a1 + uvs[1] * a2 + uvs[2] * a3;
    }

    Vector3 AddToCorrectSideList(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, List<Vector3> top, List<Vector3> bottom)
    {
        Vector3 p1 = verts[pIndex1];
        Vector3 p2 = verts[pIndex2];
        Vector2 uv1 = uvs[pIndex1];
        Vector2 uv2 = uvs[pIndex2];
        Vector3 n1 = normals[pIndex1];
        Vector3 n2 = normals[pIndex2];

        Vector3 rayDir = (p2 - p1).normalized;
        float t = Vector3.Dot(planePoint - p1, planeNormal) / Vector3.Dot(rayDir, planeNormal);
        Vector3 newVert = p1 + rayDir * t;
        Vector2 newUv = new Vector2(0, 0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        HandleBaryCentric(newVert, ref newUv, ref newNormal, verts, uvs, normals);

        if (upOrDown > 0)
        {

            if (AddUniquelyToList(p1, top))
            {
                //upUVs.Add(uv1);
                //upNormals.Add(n1);
            }
            if (AddUniquelyToList(newVert, top))
            {
                //upUVs.Add(newUv);
                //upNormals.Add(newNormal);
            }

            if (AddUniquelyToList(newVert, bottom))
            {
                //downUVs.Add(newUv);
                //downNormals.Add(newNormal);
            }
            if (AddUniquelyToList(p2, bottom))
            {
                //downUVs.Add(uv2);
                //downNormals.Add(n2);
            }

            return newVert;
        }
        else
        {
            if (AddUniquelyToList(newVert, top))
            {
                //upUVs.Add(newUv);
                //upNormals.Add(newNormal);
            }
            if (AddUniquelyToList(p2, top))
            {
                //upUVs.Add(uv2);
                //upNormals.Add(n2);
            }

            if (AddUniquelyToList(p1, bottom))
            {
                //downUVs.Add(uv1);
                //downNormals.Add(n1);
            }
            if (AddUniquelyToList(newVert, bottom))
            {
                //downUVs.Add(newUv);
                //downNormals.Add(newNormal);
            }

            return newVert;
        }
    }

    bool AddUniquelyToList(Vector3 vertex, List<Vector3> list) //order is important so cant use HashSet
    {
        if (!list.Contains(vertex))
        {
            list.Add(vertex);
            return true;
        }
        return false;
    }



}
