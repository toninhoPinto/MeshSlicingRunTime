using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;

public class CutMeshV6 : MonoBehaviour
{

    public GameObject target;
    public GameObject prefabPart;
    Vector3 planeNormal;
    Vector3 planePoint;
    Mesh myMesh;

    List<List<Vector3>> upVerts;
    List<OrderedHashSet< Vector3>> uphashVerts;
    List<List<int>> upTris;
    List<List<Vector2>> upUVs;
    List<List<Vector3>> upNormals;
    List<List<Vector4>> upTangents;

    List<List<Vector3>> downVerts;
    List<OrderedHashSet< Vector3>> downhashVerts;
    List<List<int>> downTris;
    List<List<Vector2>> downUVs;
    List<List<Vector3>> downNormals;
    List<List<Vector4>> downTangents;

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
        Vector4[] tangents = targetMesh.tangents;

        upVerts = new List<List<Vector3>>();
        uphashVerts = new List<OrderedHashSet< Vector3>>();
        upTris = new List<List<int>>();
        upUVs = new List<List<Vector2>>();
        upNormals = new List<List<Vector3>>();
        upTangents = new List<List<Vector4>>();

        downVerts = new List<List<Vector3>>();
        downhashVerts = new List<OrderedHashSet<Vector3>>();
        downTris = new List<List<int>>();
        downUVs = new List<List<Vector2>>();
        downNormals = new List<List<Vector3>>();
        downTangents = new List<List<Vector4>>();

        centerEdges = new List<Edge>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 worldp1 = target.transform.TransformPoint(verts[tris[i]]);
            Vector3 worldp2 = target.transform.TransformPoint(verts[tris[i + 1]]);
            Vector3 worldp3 = target.transform.TransformPoint(verts[tris[i + 2]]);

            Vector2 uv1 = uvs[tris[i]];
            Vector2 uv2 = uvs[tris[i + 1]];
            Vector2 uv3 = uvs[tris[i + 2]];

            Vector3 normal1 = target.transform.TransformVector(normals[tris[i]]);
            Vector3 normal2 = target.transform.TransformVector(normals[tris[i + 1]]);
            Vector3 normal3 = target.transform.TransformVector(normals[tris[i + 2]]);
            bool[] intersected = DoesTriIntersectPlane(worldp1, worldp2, worldp3);

            Vector2 tangents1 = tangents[tris[i]];
            Vector2 tangents2 = tangents[tris[i + 1]];
            Vector2 tangents3 = tangents[tris[i + 2]];

            Vector2[] triUvs = { uv1, uv2, uv3 };
            Vector3[] triVerts = { worldp1, worldp2, worldp3 };
            Vector3[] triNormals = { normal1, normal2, normal3 };
            Vector4[] triTangents = { tangents1, tangents2, tangents3};
            if (intersected[0] || intersected[1] || intersected[2])
            {
                HandleTriIntersectionPoints(intersected, triVerts, triUvs, triNormals, triTangents);
            }
            else
            {
                if (Mathf.Sign(Vector3.Dot(planeNormal, (worldp1 - planePoint))) > 0)
                {//above
                    FindSeparateMeshes(triVerts, triNormals, triTangents, triUvs, upVerts, uphashVerts, upNormals, upUVs, upTangents);
                }
                else
                {
                    FindSeparateMeshes(triVerts, triNormals, triTangents, triUvs, downVerts, downhashVerts, downNormals, downUVs, downTangents);
                }
            }
            
        }

        if (centerEdges.Count == 0)
        {
            return;
        }
        List<List<int>> groupedVerts = CenterVertsIntoParts();

        List<IntersectionLoop> faceLoops = new List<IntersectionLoop>();
        for(int i = 0; i < groupedVerts.Count; i++)
        {
            faceLoops.Add(new IntersectionLoop(groupedVerts[i], centerEdges));
        }

        HandleIntersectedZone(upVerts, uphashVerts, upTris, upUVs ,upNormals, upTangents, faceLoops, true);
        HandleIntersectedZone(downVerts, downhashVerts, downTris, downUVs, downNormals, downTangents, faceLoops, false);
        CreateParts(upVerts, upTris, upNormals, upTangents, upUVs);
        CreateParts(downVerts, downTris, downNormals, downTangents, downUVs);
        Destroy(target);
    }

    void FindSeparateMeshes(Vector3[] wPos, Vector3[] wNormals, Vector4[] wTangents, Vector2[] UVs, List<List<Vector3>> vertParts, List<OrderedHashSet<Vector3>> vertPartsHashed,
        List<List<Vector3>> normalParts, List<List<Vector2>> UVParts, List<List<Vector4>> tangentParts)
    {

        List<int> indexFound = new List<int>();
        for (int w = 0; w < vertPartsHashed.Count; w++)
        {
            if (vertPartsHashed[w].Contains(wPos[0]) || vertPartsHashed[w].Contains(wPos[1]) || vertPartsHashed[w].Contains(wPos[2]))
            {
                indexFound.Add(w);
            }
        }

        if (indexFound.Count == 0)
        {
            
            vertParts.Add(new List<Vector3>() { wPos[0], wPos[1], wPos[2] });
            vertPartsHashed.Add(new OrderedHashSet<Vector3>() { wPos[0], wPos[1], wPos[2] });
            normalParts.Add(new List<Vector3>() { wNormals[0], wNormals[1], wNormals[2] });
            UVParts.Add(new List<Vector2>() { UVs[0], UVs[1], UVs[2] });
            tangentParts.Add(new List<Vector4>() { wTangents[0], wTangents[1], wTangents[2] });
        }
        else
        {
            vertParts[indexFound[0]].Add(wPos[0]);
            vertParts[indexFound[0]].Add(wPos[1]);
            vertParts[indexFound[0]].Add(wPos[2]);

            normalParts[indexFound[0]].Add(wNormals[0]);
            normalParts[indexFound[0]].Add(wNormals[1]);
            normalParts[indexFound[0]].Add(wNormals[2]);

            UVParts[indexFound[0]].Add(UVs[0]);
            UVParts[indexFound[0]].Add(UVs[1]);
            UVParts[indexFound[0]].Add(UVs[2]);

            tangentParts[indexFound[0]].Add(wTangents[0]);
            tangentParts[indexFound[0]].Add(wTangents[1]);
            tangentParts[indexFound[0]].Add(wTangents[2]);

            if (!vertPartsHashed[indexFound[0]].Contains(wPos[0]))
                vertPartsHashed[indexFound[0]].Add(wPos[0]);

            if (!vertPartsHashed[indexFound[0]].Contains(wPos[1]))
                vertPartsHashed[indexFound[0]].Add(wPos[1]);

            if (!vertPartsHashed[indexFound[0]].Contains(wPos[2]))
                vertPartsHashed[indexFound[0]].Add(wPos[2]);

            for (int k = indexFound.Count-1; k > 0; k--)
            {
                vertParts[indexFound[0]].AddRange(vertParts[indexFound[k]]);
                normalParts[indexFound[0]].AddRange(normalParts[indexFound[k]]);
                UVParts[indexFound[0]].AddRange(UVParts[indexFound[k]]);
                tangentParts[indexFound[0]].AddRange(tangentParts[indexFound[k]]);
                vertPartsHashed[indexFound[0]].ConcatIt(vertPartsHashed[indexFound[k]]);

                /* fancy method, after debug put back in
                List<Vector3> tmp = vertParts[vertParts.Count-1];
                vertParts[vertParts.Count-1] = vertParts[indexFound[k]];
                vertParts[indexFound[k]] = tmp;
                vertParts.RemoveAt(vertParts.Count-1);
                */

                vertParts.RemoveAt(indexFound[k]);
                normalParts.RemoveAt(indexFound[k]);
                UVParts.RemoveAt(indexFound[k]);
                tangentParts.RemoveAt(indexFound[k]);
                vertPartsHashed.RemoveAt(indexFound[k]);
            }
        }
        indexFound.Clear();
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

    void HandleIntersectedZone(List<List<Vector3>> partVerts, List<OrderedHashSet<Vector3>> vertPartsHashed, 
        List<List<int>> partTris, List<List<Vector2>> partUvs, List<List<Vector3>> partNormals, List<List<Vector4>> partTangents, List<IntersectionLoop> centerGroups, bool top)
    {

        for (int i = 0; i < vertPartsHashed.Count; i++)
        {
            partTris.Add(new List<int>());

            for (int k = 0; k < partVerts[i].Count; k++)
            {
                partTris[i].Add(k);
            }

            for (int j=0; j< centerGroups.Count; j++)
            {
                List<Vector3> centerVerts = centerGroups[j].verts;
                if (vertPartsHashed[i].Contains(centerVerts[0]))
                {
                    List<int> centerTris = new List<int>();

                    Vector3 center = centerGroups[j].center;

                    int sizeVertsBeforeCenter = partVerts[i].Count;
                    if (planeNormal.y != 0)
                    {
                        float normalDir = Mathf.Sign(planeNormal.y);
                        partVerts[i].AddRange(centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).z, (x - center).x)));
                    }
                    else
                    {
                        float normalDir = Mathf.Sign(planeNormal.z);
                        partVerts[i].AddRange(centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).y, (x - center).x)));
                    }

                    partVerts[i].Add(center);

                    if (top)
                    {
                        for (int k = sizeVertsBeforeCenter; k < partVerts[i].Count - 1; k++)
                        {
                            centerTris.Add(k);
                            centerTris.Add(k + 1);
                            centerTris.Add(partVerts[i].Count - 1);
                        }

                        centerTris.Add(partVerts[i].Count - 2);
                        centerTris.Add(sizeVertsBeforeCenter);
                        centerTris.Add(partVerts[i].Count - 1);
                    }
                    else
                    {
                        for (int k = sizeVertsBeforeCenter; k < partVerts[i].Count - 1; k++)
                        {
                            centerTris.Add(k);
                            centerTris.Add(partVerts[i].Count - 1);
                            centerTris.Add(k + 1);
                        }

                        centerTris.Add(partVerts[i].Count - 2);
                        centerTris.Add(partVerts[i].Count - 1);
                        centerTris.Add(sizeVertsBeforeCenter);
                    }
                    partTris[i].AddRange(centerTris);


                    Vector3 normal;
                    if (top)
                        normal = -planeNormal;
                    else
                        normal = planeNormal;
                    for (int k = sizeVertsBeforeCenter; k < partVerts[i].Count; k++)
                    {
                        partUvs[i].Add(new Vector2(0, 0));
                        partNormals[i].Add(normal);
                        partTangents[i].Add(new Vector4(0, 0, 0, 0));
                    }

                }

            }
           
        }
    }

    void CreateParts(List<List<Vector3>> partVerts, List<List<int>> partTris, List<List<Vector3>> partNormals, List<List<Vector4>> partTangents, List<List<Vector2>> partUvs)
    {

        for (int i = 0; i < partVerts.Count; i++)
        {
            GameObject newPart = Instantiate(prefabPart);

            Debug.Log(partVerts[i].Count);
            Debug.Log(partTris[i].Count);
            Debug.Log(partNormals[i].Count);
            Debug.Log(partUvs[i].Count);
            Debug.Log(partTangents[i].Count);


            for (int k = 0; k < partVerts[i].Count; k++)
            {
                partVerts[i][k] = newPart.transform.InverseTransformPoint(partVerts[i][k]);
                partNormals[i][k] = newPart.transform.InverseTransformVector(partNormals[i][k]).normalized * 3;
            }

            Mesh newPartMesh = newPart.GetComponent<MeshFilter>().mesh;
            newPartMesh.Clear();
            newPartMesh.vertices = partVerts[i].ToArray();
            newPartMesh.triangles = partTris[i].ToArray();
            newPartMesh.normals = partNormals[i].ToArray();
            newPartMesh.tangents = partTangents[i].ToArray();
            newPartMesh.uv = partUvs[i].ToArray();
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

    void HandleTriIntersectionPoints(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals, Vector4[] tangents)
    {
        List<Vector3> tmpUpVerts = new List<Vector3>();
        List<Vector3> tmpDownVerts = new List<Vector3>();
        List<Vector3> tmpUpNormals = new List<Vector3>();
        List<Vector3> tmpDownNormals = new List<Vector3>();
        List<Vector2> tmpUpUvs = new List<Vector2>();
        List<Vector2> tmpDownUvs = new List<Vector2>();
        List<Vector4> tmpUpTangents = new List<Vector4>();
        List<Vector4> tmpDownTangents = new List<Vector4>();

        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, verts[0] - planePoint));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, verts[1] - planePoint));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, verts[2] - planePoint));

        Vector3[] newVectors = new Vector3[2];
        int newVectorIndex = 0;

        if (intersections[0])
        {
            newVectors[newVectorIndex] = AddToCorrectSideList(upOrDown, 0, 1, verts, uvs, normals, tangents, tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs,
                tmpUpTangents, tmpDownTangents);
            newVectorIndex++;
        }
        if (intersections[1])
        {
            newVectors[newVectorIndex] = AddToCorrectSideList(upOrDown2, 1, 2, verts, uvs, normals, tangents, tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs,
                 tmpUpTangents, tmpDownTangents);
            newVectorIndex++;
        }
        if (intersections[2])
        {
            newVectors[newVectorIndex] = AddToCorrectSideList(upOrDown3, 2, 0, verts, uvs, normals, tangents, tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs,
                 tmpUpTangents, tmpDownTangents);
        }

        //only 2 new vectors in all cases
        centerEdges.Add(new Edge(newVectors[0], newVectors[1]));

        HandleTriOrder(tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs, tmpUpTangents, tmpDownTangents);
    }

    void HandleTriOrder(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts, List<Vector3> tmpUpNormals, List<Vector3> tmpDownNormals, List<Vector2> tmpUpUvs, List<Vector2> tmpDownUvs,
        List<Vector4> tmpUpTangs, List<Vector4> tmpDownTangs)
    {
        FindSeparateMeshes(tmpDownVerts.ToArray(), tmpDownNormals.ToArray(), tmpDownTangs.ToArray(), tmpDownUvs.ToArray(), downVerts, downhashVerts, downNormals, downUVs, downTangents);

        if(tmpDownVerts.Count > 3) //for when a triangle is cut into 3 triangles (2 on 1 side and 1 on the other)
            FindSeparateMeshes(new Vector3[] { tmpDownVerts[0], tmpDownVerts[2], tmpDownVerts[3] }, 
                new Vector3[] { tmpDownNormals[0], tmpDownNormals[2], tmpDownNormals[3] },
                new Vector4[] { tmpDownTangs[0], tmpDownTangs[2], tmpDownTangs[3] },
                new Vector2[] { tmpDownUvs[0], tmpDownUvs[2], tmpDownUvs[3] }, downVerts, downhashVerts, downNormals, downUVs, downTangents);

        FindSeparateMeshes(tmpUpVerts.ToArray(), tmpUpNormals.ToArray(), tmpUpTangs.ToArray(), tmpUpUvs.ToArray(), upVerts, uphashVerts, upNormals, upUVs, upTangents);

        if (tmpUpVerts.Count > 3) //for when a triangle is cut into 3 triangles (2 on 1 side and 1 on the other)
            FindSeparateMeshes(new Vector3[] { tmpUpVerts[0], tmpUpVerts[2], tmpUpVerts[3] },
                new Vector3[] { tmpUpNormals[0], tmpUpNormals[2], tmpUpNormals[3] },
                new Vector4[] { tmpUpTangs[0], tmpUpTangs[2], tmpUpTangs[3] },
                new Vector2[] { tmpUpUvs[0], tmpUpUvs[2], tmpUpUvs[3] }, upVerts, uphashVerts, upNormals, upUVs, upTangents);
    }

    void HandleBaryCentric(Vector3 newPoint, ref Vector2 newUV, ref Vector3 newNormal, ref Vector4 newTangent, Vector3[] points, Vector2[] uvs, Vector3[] normals, Vector4[] tangents)
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
        newTangent = tangents[0] * a1 + tangents[1] * a2 + tangents[2] * a3;
    }

    Vector3 AddToCorrectSideList(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, Vector4[] tangents, 
        List<Vector3> top, List<Vector3> bottom, List<Vector3> tmpUpNormals, List<Vector3> tmpDownNormals, List<Vector2> tmpUpUvs, List<Vector2> tmpDownUvs, List<Vector4> tmpUpTangs, List<Vector4> tmpDownTangs)
    {
        Vector3 p1 = verts[pIndex1];
        Vector3 p2 = verts[pIndex2];
        Vector2 uv1 = uvs[pIndex1];
        Vector2 uv2 = uvs[pIndex2];
        Vector3 n1 = normals[pIndex1];
        Vector3 n2 = normals[pIndex2];
        Vector3 t1 = tangents[pIndex1];
        Vector3 t2 = tangents[pIndex2];

        Vector3 rayDir = (p2 - p1).normalized;
        float t = Vector3.Dot(planePoint - p1, planeNormal) / Vector3.Dot(rayDir, planeNormal);
        Vector3 newVert = p1 + rayDir * t;
        Vector2 newUv = new Vector2(0, 0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        Vector4 newTangent = new Vector4(0, 0, 0, 0);
        HandleBaryCentric(newVert, ref newUv, ref newNormal, ref newTangent, verts, uvs, normals, tangents);

        //---------------------------------
        if (upOrDown > 0)
        {

            if (!top.Contains(p1))
            {
                top.Add(p1);
                tmpUpUvs.Add(uv1);
                tmpUpNormals.Add(n1);
                tmpUpTangs.Add(t1);
            }

            top.Add(newVert);
            tmpUpUvs.Add(newUv);
            tmpUpNormals.Add(newNormal);
            tmpUpTangs.Add(newTangent);

            bottom.Add(newVert);
            tmpDownUvs.Add(newUv);
            tmpDownNormals.Add(newNormal);
            tmpDownTangs.Add(newTangent);

            if (!bottom.Contains(p2))
            {
                bottom.Add(p2);
                tmpDownUvs.Add(uv2);
                tmpDownNormals.Add(n2);
                tmpDownTangs.Add(t2);
            }

            return newVert;
        }
        else
        {

            top.Add(newVert);
            tmpUpUvs.Add(newUv);
            tmpUpNormals.Add(newNormal);
            tmpUpTangs.Add(newTangent);

            if (!top.Contains(p2))
            {
                top.Add(p2);
                tmpUpUvs.Add(uv2);
                tmpUpNormals.Add(n2);
                tmpUpTangs.Add(t2);
            }
            if (!bottom.Contains(p1))
            {
                bottom.Add(p1);
                tmpDownUvs.Add(uv1);
                tmpDownNormals.Add(n1);
                tmpDownTangs.Add(t1);
            }

            bottom.Add(newVert);
            tmpDownUvs.Add(newUv);
            tmpDownNormals.Add(newNormal);
            tmpDownTangs.Add(newTangent);

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
