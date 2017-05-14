using System.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;
using System.Linq.Expressions;

public class CutMeshV6 : MonoBehaviour
{

    public GameObject target;
    public GameObject prefabPart;
    Vector3 planeNormal;
    Vector3 planePoint;
    Mesh myMesh;

    List<List<Vector3>> upVerts;
    List<OrderedHashSet<Vector3>> uphashVerts;
    List<ProtoMesh> upTris;
    List<List<Vector2>> upUVs;
    List<List<Vector3>> upNormals;
    List<List<Vector4>> upTangents;

    List<List<Vector3>> downVerts;
    List<OrderedHashSet<Vector3>> downhashVerts;
    List<ProtoMesh> downTris;
    List<List<Vector2>> downUVs;
    List<List<Vector3>> downNormals;
    List<List<Vector4>> downTangents;

    List<Edge> centerEdges;

    ListPooler listPooler;

    void Start()
    {
        myMesh = GetComponent<MeshFilter>().mesh;
        listPooler = new ListPooler();
    }

    void OnTriggerEnter(Collider other)
    {
        target = other.gameObject;
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
        uphashVerts = new List<OrderedHashSet<Vector3>>();

        upTris = new List<ProtoMesh>();
        upUVs = new List<List<Vector2>>();
        upNormals = new List<List<Vector3>>();
        upTangents = new List<List<Vector4>>();

        downVerts = new List<List<Vector3>>();
        downhashVerts = new List<OrderedHashSet<Vector3>>();
        downTris = new List<ProtoMesh>();
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

            Vector3 normal1 = normals[tris[i]];
            Vector3 normal2 = normals[tris[i + 1]];
            Vector3 normal3 = normals[tris[i + 2]];

            Vector4 tangents1 = tangents[tris[i]];
            Vector4 tangents2 = tangents[tris[i + 1]];
            Vector4 tangents3 = tangents[tris[i + 2]];

            Vector2[] triUvs = { uv1, uv2, uv3 };
            Vector3[] triVerts = { worldp1, worldp2, worldp3 };
            Vector3[] triNormals = { normal1, normal2, normal3 };
            Vector4[] triTangents = { tangents1, tangents2, tangents3 };

            bool[] intersected = DoesTriIntersectPlane(worldp1, worldp2, worldp3);
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
        for (int i = 0; i < groupedVerts.Count; i++)
        {
            faceLoops.Add(new IntersectionLoop(groupedVerts[i], centerEdges));
        }
        HandleBodyTris(upVerts, upTris);
        HandleBodyTris(downVerts, downTris);
        HandleIntersectedZone(upVerts, uphashVerts, upTris, upUVs, upNormals, upTangents, faceLoops, true);
        HandleIntersectedZone(downVerts, downhashVerts, downTris, downUVs, downNormals, downTangents, faceLoops, false);
        CreateParts(upVerts, upTris, upNormals, upTangents, upUVs);
        CreateParts(downVerts, downTris, downNormals, downTangents, downUVs);
        Destroy(target);
    }

    void MyRemoveAt<T>(List<List<T>> list, int k)
    {
        List<T> tmp = list[list.Count - 1];
        list[list.Count - 1] = list[k];
        list[k] = tmp;
        list.RemoveAt(list.Count - 1);
    }

    void FindSeparateMeshes(Vector3[] wPos, Vector3[] wNormals, Vector4[] tangents, Vector2[] UVs, List<List<Vector3>> vertParts, List<OrderedHashSet<Vector3>> vertPartsHashed,
        List<List<Vector3>> normalParts, List<List<Vector2>> UVParts, List<List<Vector4>> tangentParts)
    {

        List<int> indexFound = listPooler.GetPooledList();
        for (int w = 0; w < vertPartsHashed.Count; w++)
        {
            if (vertPartsHashed[w].Contains(wPos[0]) || vertPartsHashed[w].Contains(wPos[1]) || vertPartsHashed[w].Contains(wPos[2]))
            {
                indexFound.Add(w);
            }
        }

        if (indexFound.Count == 0)
        {
            if (wPos[0] == wPos[1] || wPos[1] == wPos[2] || wPos[2] == wPos[0])
                Debug.Log("how the fuck");

            if (wPos[0] == wPos[1] && wPos[1] == wPos[2] && wPos[2] == wPos[0])
                Debug.Log("how the fuck2");
            vertParts.Add(listPooler.GetPooledListVector3(wPos[0], wPos[1], wPos[2]));
            vertPartsHashed.Add(listPooler.GetPooledHashSet(wPos[0], wPos[1], wPos[2]));
            normalParts.Add(listPooler.GetPooledListVector3(wNormals[0], wNormals[1], wNormals[2]));
            UVParts.Add(listPooler.GetPooledListVector2(UVs[0], UVs[1], UVs[2]));
            tangentParts.Add(listPooler.GetPooledListVector4(tangents[0], tangents[1], tangents[2]));
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

            tangentParts[indexFound[0]].Add(tangents[0]);
            tangentParts[indexFound[0]].Add(tangents[1]);
            tangentParts[indexFound[0]].Add(tangents[2]);

            if (!vertPartsHashed[indexFound[0]].Contains(wPos[0]))
                vertPartsHashed[indexFound[0]].Add(wPos[0]);

            if (!vertPartsHashed[indexFound[0]].Contains(wPos[1]))
                vertPartsHashed[indexFound[0]].Add(wPos[1]);

            if (!vertPartsHashed[indexFound[0]].Contains(wPos[2]))
                vertPartsHashed[indexFound[0]].Add(wPos[2]);

            int index;
            for (int k = indexFound.Count - 1; k > 0; k--)
            {
                index = indexFound[k];

                vertParts[indexFound[0]].AddRange(vertParts[index]);
                normalParts[indexFound[0]].AddRange(normalParts[index]);
                UVParts[indexFound[0]].AddRange(UVParts[index]);
                tangentParts[indexFound[0]].AddRange(tangentParts[index]);
                vertPartsHashed[indexFound[0]].ConcatIt(vertPartsHashed[index]);

                listPooler.PoolList(vertParts[index]);
                listPooler.PoolList(normalParts[index]);
                listPooler.PoolList(UVParts[index]);
                listPooler.PoolList(tangentParts[index]);
                listPooler.PoolHashSet(vertPartsHashed[index]);

                MyRemoveAt(vertParts, index);
                MyRemoveAt(normalParts, index);
                MyRemoveAt(UVParts, index);
                MyRemoveAt(tangentParts, index);
                vertPartsHashed.RemoveAt(index);
            }
        }
        listPooler.PoolList(indexFound);
    }

    void HandleBodyTris(List<List<Vector3>> partVerts, List<ProtoMesh> partTris)
    {
        for (int i = 0; i < partVerts.Count; i++)
        {
            List<int> newListTris = listPooler.GetPooledList();
            for (int k = 0; k < partVerts[i].Count; k++)
            {
                newListTris.Add(k);
            }
            partTris.Add(new ProtoMesh(newListTris, listPooler.GetPooledList()));
        }
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

                if (EdgeA.Equals(EdgeB) && tmpEdgesConnected.Count > 1)// did a loop
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
        List<ProtoMesh> partTris, List<List<Vector2>> partUvs, List<List<Vector3>> partNormals, List<List<Vector4>> partTangents, List<IntersectionLoop> centerGroups, bool top)
    {

        for (int i = 0; i < vertPartsHashed.Count; i++)
        {
            for (int j = 0; j < centerGroups.Count; j++)
            {
                List<Vector3> centerVerts = centerGroups[j].verts;
                if (vertPartsHashed[i].Contains(centerVerts[0]))
                {
                    List<int> centerTris = listPooler.GetPooledList();

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
                    partTris[i].SubmeshTris.AddRange(centerTris);
                    listPooler.PoolList(centerTris);

                    Vector3 normal;

                    if (top)
                        normal = planeNormal;
                    else
                        normal = -planeNormal;
                    for (int k = sizeVertsBeforeCenter; k < partVerts[i].Count; k++)
                    {
                        Vector2 newUV = transform.worldToLocalMatrix.MultiplyPoint(partVerts[i][k]);
                        newUV.x += 0.5f;
                        newUV.y += 0.5f;
                        partUvs[i].Add(newUV);

                        partNormals[i].Add(normal);
                        partTangents[i].Add(new Vector4(1, 0, 0, -1));
                    }

                }

            }

        }
    }

    void CreateParts(List<List<Vector3>> partVerts, List<ProtoMesh> partTris, List<List<Vector3>> partNormals, List<List<Vector4>> partTangents, List<List<Vector2>> partUvs)
    {

        for (int i = 0; i < partVerts.Count; i++)
        {
            GameObject newPart = Instantiate(prefabPart);
            newPart.transform.localScale = target.transform.localScale;
            newPart.transform.rotation = target.transform.rotation;

            for (int k = 0; k < partVerts[i].Count; k++)
            {
                partVerts[i][k] = newPart.transform.InverseTransformPoint(partVerts[i][k]);
                partNormals[i][k] = partNormals[i][k];
            }

            Mesh newPartMesh = newPart.GetComponent<MeshFilter>().mesh;
            newPartMesh.Clear();
            newPartMesh.subMeshCount = 2;
            newPartMesh.SetVertices(partVerts[i]);
            newPartMesh.SetTriangles(partTris[i].BodyTris, 0);
            newPartMesh.SetTriangles(partTris[i].SubmeshTris, 1);
            newPartMesh.SetNormals(partNormals[i]);
            newPartMesh.SetTangents(partTangents[i]);
            newPartMesh.SetUVs(0, partUvs[i]);
            newPartMesh.RecalculateBounds();
            newPart.GetComponent<Renderer>().material = target.GetComponent<Renderer>().material;
            newPart.GetComponent<Renderer>().materials[1] = target.GetComponent<Renderer>().material;
            newPart.GetComponent<MeshCollider>().sharedMesh = newPartMesh;
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
        List<Vector3> tmpUpVerts = listPooler.GetPooledListVector3();
        List<Vector3> tmpDownVerts = listPooler.GetPooledListVector3();
        List<Vector3> tmpUpNormals = listPooler.GetPooledListVector3();
        List<Vector3> tmpDownNormals = listPooler.GetPooledListVector3();
        List<Vector2> tmpUpUvs = listPooler.GetPooledListVector2();
        List<Vector2> tmpDownUvs = listPooler.GetPooledListVector2();
        List<Vector4> tmpUpTangents = listPooler.GetPooledListVector4();
        List<Vector4> tmpDownTangents = listPooler.GetPooledListVector4();

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

        listPooler.PoolList(tmpUpVerts);
        listPooler.PoolList(tmpDownVerts);
        listPooler.PoolList(tmpUpNormals);
        listPooler.PoolList(tmpDownNormals);
        listPooler.PoolList(tmpUpUvs);
        listPooler.PoolList(tmpDownUvs);
        listPooler.PoolList(tmpUpTangents);
        listPooler.PoolList(tmpDownTangents);
    }

    void HandleTriOrder(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts, List<Vector3> tmpUpNormals, List<Vector3> tmpDownNormals, List<Vector2> tmpUpUvs, List<Vector2> tmpDownUvs,
        List<Vector4> tmpUpTangs, List<Vector4> tmpDownTangs)
    {
        FindSeparateMeshes(tmpDownVerts.ToArray(), tmpDownNormals.ToArray(), tmpDownTangs.ToArray(), tmpDownUvs.ToArray(), downVerts, downhashVerts, downNormals, downUVs, downTangents);

        if (tmpDownVerts.Count > 3) //for when a triangle is cut into 3 triangles (2 on 1 side and 1 on the other)
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

        Vector4 t1 = tangents[pIndex1];
        Vector4 t2 = tangents[pIndex2];

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


}
