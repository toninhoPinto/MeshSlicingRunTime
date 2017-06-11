using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CutMultiplePartsConcaveAndConcavePolygon : MonoBehaviour
{

    public GameObject target;
    public GameObject prefabPart;
    Vector3 planeNormal;
    Vector3 planePoint;
    Vector4 planeTangent;
    Mesh myMesh;
    Renderer targetRenderer;

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

    Vector3[] newTriangleEdge = new Vector3[2];

    Vector3[] triVerts = new Vector3[3];
    Vector2[] triUvs = new Vector2[3];
    Vector3[] triNormals = new Vector3[3];
    Vector4[] triTangents = new Vector4[3];

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
        planeNormal = -transform.forward;
        planeNormal = planeNormal.normalized;
        planePoint = transform.position;
        planeTangent = new Vector4(transform.right.x, transform.right.y, transform.right.z, 1);
        //==================================================

        Mesh targetMesh = target.GetComponent<MeshFilter>().mesh;
        targetRenderer = target.GetComponent<Renderer>();

        List<int> tris;
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
        bool[] intersected = new bool[3];

        float submeshCount = targetMesh.subMeshCount;

        List<int> bigMeshVertsSizeUp = listPooler.GetPooledList();
        List<int> bigMeshVertsSizeDown = listPooler.GetPooledList();

        for (int j = 0; j < submeshCount; j++)
        {
            tris = listPooler.GetPooledList();
            targetMesh.GetTriangles(tris, j);
            for (int i = 0; i < tris.Count; i += 3)
            {
                triVerts[0] = target.transform.TransformPoint(verts[tris[i]]);
                triVerts[1] = target.transform.TransformPoint(verts[tris[i + 1]]);
                triVerts[2] = target.transform.TransformPoint(verts[tris[i + 2]]);

                triUvs[0] = uvs[tris[i]];
                triUvs[1] = uvs[tris[i + 1]];
                triUvs[2] = uvs[tris[i + 2]];

                triNormals[0] = normals[tris[i]];
                triNormals[1] = normals[tris[i + 1]];
                triNormals[2] = normals[tris[i + 2]];

                triTangents[0] = tangents[tris[i]];
                triTangents[1] = tangents[tris[i + 1]];
                triTangents[2] = tangents[tris[i + 2]];

                DoesTriIntersectPlane(triVerts[0], triVerts[1], triVerts[2], intersected);
                if (intersected[0] || intersected[1] || intersected[2])
                {
                    TriIntersectionPoints(intersected, triVerts, triUvs, triNormals, triTangents);
                }
                else
                {
                    if (Mathf.Sign(Vector3.Dot(planeNormal, (triVerts[0] - planePoint))) > 0)
                    {//above
                        AddTriToCorrectMeshObject(triVerts, triNormals, triTangents, triUvs, upVerts, uphashVerts, upNormals, upUVs, upTangents);
                    }
                    else
                    {
                        AddTriToCorrectMeshObject(triVerts, triNormals, triTangents, triUvs, downVerts, downhashVerts, downNormals, downUVs, downTangents);
                    }
                }

            }

            if (j == 0 )
            {
                for (int k = 0; k < upVerts.Count; k++)
                {
                    bigMeshVertsSizeUp.Add(upVerts[k].Count);
                }

                for (int k = 0; k < downVerts.Count; k++)
                {
                    bigMeshVertsSizeDown.Add(downVerts[k].Count);
                }
            }
                
            listPooler.PoolList(tris);
        }

        if (centerEdges.Count == 0 || upVerts.Count == 0 || downVerts.Count == 0)
        {
            return;
        }

        CreateBodyTris(upVerts, upTris, bigMeshVertsSizeUp);
        CreateBodyTris(downVerts, downTris, bigMeshVertsSizeDown);

        List<List<Vector3>> groupedVerts = GroupConnectedCenterVerts();

        List<IntersectionLoop> faceLoops = new List<IntersectionLoop>();
        for (int i = 0; i < groupedVerts.Count; i++)
        {
            faceLoops.Add(new IntersectionLoop(groupedVerts[i]));
        }

        CreateHullMeshFromEdgeLoop(upVerts, uphashVerts, upTris, upUVs, upNormals, upTangents, faceLoops, true);
        CreateHullMeshFromEdgeLoop(downVerts, downhashVerts, downTris, downUVs, downNormals, downTangents, faceLoops, false);

        CreateFinalGameObjects(upVerts, upTris, upNormals, upTangents, upUVs);
        CreateFinalGameObjects(downVerts, downTris, downNormals, downTangents, downUVs);

        Destroy(target);
    }

    void MyRemoveAt<T>(List<List<T>> list, int k)
    {
        List<T> tmp = list[list.Count - 1];
        list[list.Count - 1] = list[k];
        list[k] = tmp;
        list.RemoveAt(list.Count - 1);
    }

    void AddTriToCorrectMeshObject(Vector3[] wPos, Vector3[] wNormals, Vector4[] tangents, Vector2[] UVs, List<List<Vector3>> vertParts, List<OrderedHashSet<Vector3>> vertPartsHashed,
        List<List<Vector3>> normalParts, List<List<Vector2>> UVParts, List<List<Vector4>> tangentParts)
    {
        if (wPos[0] == wPos[1] || wPos[1] == wPos[2] || wPos[2] == wPos[0])
            Debug.Log("how the fuck");

        if (wPos[0] == wPos[1] && wPos[1] == wPos[2] && wPos[2] == wPos[0])
            Debug.Log("how the fuck2");

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

    void CreateBodyTris(List<List<Vector3>> partVerts, List<ProtoMesh> partTris, List<int> limit)
    {
        for (int i = 0; i < partVerts.Count; i++)
        {
            List<int> newListBodyTris = listPooler.GetPooledList();
            List<int> newListSubMeshTris = listPooler.GetPooledList();

            for (int k = 0; k < partVerts[i].Count; k++)
            {
                if (k < limit[i])
                    newListBodyTris.Add(k);
                else
                    newListSubMeshTris.Add(k);
            }
            partTris.Add(new ProtoMesh(newListBodyTris, newListSubMeshTris));
        }
        listPooler.PoolList(limit);
    }

    List<List<Vector3>> GroupConnectedCenterVerts()
    {
        bool[] visited = new bool[centerEdges.Count];

        int nextEdge = 0;
        int EdgeA = nextEdge;
        Vector3 start = centerEdges[nextEdge].start;
        int EdgeB = nextEdge;
        Vector3 end = centerEdges[nextEdge].end;
        visited[nextEdge] = true;

        List<List<Vector3>> groupedEdgesConnected = new List<List<Vector3>>();
        List<Vector3> tmpEdgesConnected = listPooler.GetPooledListVector3();
        tmpEdgesConnected.Add(start);
        tmpEdgesConnected.Add(end);
       
        bool finished = false;
        while (!finished)
        {
            for (int i = 0; i < centerEdges.Count; i++)
            {

                if ((EdgeA == EdgeB || start == end) && tmpEdgesConnected.Count > 2)// did a loop
                {
                    tmpEdgesConnected.RemoveAt(tmpEdgesConnected.Count - 1);
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
                    tmpEdgesConnected = listPooler.GetPooledListVector3();
                    visited[nextEdge] = true;
                    tmpEdgesConnected.Add(start);
                }

                if (visited[i])
                    continue;

                if (start == centerEdges[i].start || start == centerEdges[i].end)
                {
                    if (start == centerEdges[i].start)
                        start = centerEdges[i].end;
                    else
                        start = centerEdges[i].start;

                    if (!visited[i])
                        tmpEdgesConnected.Add(start);
                    EdgeA = i;
                    visited[EdgeA] = true;
                }

                if (end == centerEdges[i].start || end == centerEdges[i].end)
                {
                    if (end == centerEdges[i].start)
                        end = centerEdges[i].end;
                    else
                        end = centerEdges[i].start;

                    if (!visited[i])
                        tmpEdgesConnected.Add(end);
                    EdgeB = i;
                    visited[EdgeB] = true;
                }


            }

        }


        return groupedEdgesConnected;
    }

    void CreateHullMeshFromEdgeLoop(List<List<Vector3>> partVerts, List<OrderedHashSet<Vector3>> vertPartsHashed,
        List<ProtoMesh> partTris, List<List<Vector2>> partUvs, List<List<Vector3>> partNormals, List<List<Vector4>> partTangents, List<IntersectionLoop> centerGroups, bool top)
    {
        List<Vector3> centerVerts;
        for (int i = 0; i < vertPartsHashed.Count; i++)
        {
            for (int j = 0; j < centerGroups.Count; j++)
            {
                centerVerts = centerGroups[j].verts;
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
                        partVerts[i].AddRange(centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).x, (x - center).y)));
                    }

                    partVerts[i].Add(center);

                    if (top)
                    {
                        for (int k = sizeVertsBeforeCenter; k < partVerts[i].Count - 2; k++)
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
                        for (int k = sizeVertsBeforeCenter; k < partVerts[i].Count - 2; k++)
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
                        partTangents[i].Add(planeTangent);
                    }

                }

            }

        }
    }

    void CreateFinalGameObjects(List<List<Vector3>> partVerts, List<ProtoMesh> partTris, List<List<Vector3>> partNormals, List<List<Vector4>> partTangents, List<List<Vector2>> partUvs)
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
            newPart.GetComponent<Renderer>().material = targetRenderer.material;
            newPart.GetComponent<MeshCollider>().sharedMesh = newPartMesh;

            //return protoMesh lists to the pool (need to think if the structs themselves could be pooled aswell somehow)
            listPooler.PoolList(partTris[i].BodyTris);
            listPooler.PoolList(partTris[i].SubmeshTris);
        }
    }

    void DoesTriIntersectPlane(Vector3 p1, Vector3 p2, Vector3 p3, bool[] intersections)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, p1 - planePoint));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, p2 - planePoint));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, p3 - planePoint));

        intersections[0] = upOrDown != upOrDown2;
        intersections[1] = upOrDown2 != upOrDown3;
        intersections[2] = upOrDown != upOrDown3;
    }

    void TriIntersectionPoints(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals, Vector4[] tangents)
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

        int newVectorIndex = 0;

        if (intersections[0])
        {
            newTriangleEdge[newVectorIndex] = EdgeIntersectionPoints(upOrDown, 0, 1, verts, uvs, normals, tangents, tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs,
                tmpUpTangents, tmpDownTangents);
            newVectorIndex++;
        }
        if (intersections[1])
        {
            newTriangleEdge[newVectorIndex] = EdgeIntersectionPoints(upOrDown2, 1, 2, verts, uvs, normals, tangents, tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs,
                 tmpUpTangents, tmpDownTangents);
            newVectorIndex++;
        }
        if (intersections[2])
        {
            newTriangleEdge[newVectorIndex] = EdgeIntersectionPoints(upOrDown3, 2, 0, verts, uvs, normals, tangents, tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs,
                 tmpUpTangents, tmpDownTangents);
        }

        //only 2 new vectors in all cases
        centerEdges.Add(new Edge(newTriangleEdge[0], newTriangleEdge[1]));

        TriIntersectionType(tmpUpVerts, tmpDownVerts, tmpUpNormals, tmpDownNormals, tmpUpUvs, tmpDownUvs, tmpUpTangents, tmpDownTangents);

        listPooler.PoolList(tmpUpVerts);
        listPooler.PoolList(tmpDownVerts);
        listPooler.PoolList(tmpUpNormals);
        listPooler.PoolList(tmpDownNormals);
        listPooler.PoolList(tmpUpUvs);
        listPooler.PoolList(tmpDownUvs);
        listPooler.PoolList(tmpUpTangents);
        listPooler.PoolList(tmpDownTangents);
    }

    Vector3 EdgeIntersectionPoints(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, Vector4[] tangents,
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
        BarycentricInterpolation(newVert, ref newUv, ref newNormal, ref newTangent, verts, uvs, normals, tangents);

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

    void TriIntersectionType(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts, List<Vector3> tmpUpNormals, List<Vector3> tmpDownNormals, List<Vector2> tmpUpUvs, List<Vector2> tmpDownUvs,
        List<Vector4> tmpUpTangs, List<Vector4> tmpDownTangs)
    {
        triVerts[0] = tmpDownVerts[0];
        triVerts[1] = tmpDownVerts[1];
        triVerts[2] = tmpDownVerts[2];

        triNormals[0] = tmpDownNormals[0];
        triNormals[1] = tmpDownNormals[1];
        triNormals[2] = tmpDownNormals[2];

        triUvs[0] = tmpDownUvs[0];
        triUvs[1] = tmpDownUvs[1];
        triUvs[2] = tmpDownUvs[2];

        triTangents[0] = tmpDownTangs[0];
        triTangents[1] = tmpDownTangs[1];
        triTangents[2] = tmpDownTangs[2];

        AddTriToCorrectMeshObject(triVerts, triNormals, triTangents, triUvs, downVerts, downhashVerts, downNormals, downUVs, downTangents);

        if (tmpDownVerts.Count > 3)
        { //for when a triangle is cut into 3 triangles (2 on 1 side and 1 on the other)

            triVerts[0] = tmpDownVerts[0];
            triVerts[1] = tmpDownVerts[2];
            triVerts[2] = tmpDownVerts[3];

            triNormals[0] = tmpDownNormals[0];
            triNormals[1] = tmpDownNormals[2];
            triNormals[2] = tmpDownNormals[3];

            triUvs[0] = tmpDownUvs[0];
            triUvs[1] = tmpDownUvs[2];
            triUvs[2] = tmpDownUvs[3];

            triTangents[0] = tmpDownTangs[0];
            triTangents[1] = tmpDownTangs[2];
            triTangents[2] = tmpDownTangs[3];

            AddTriToCorrectMeshObject(triVerts, triNormals, triTangents, triUvs, downVerts, downhashVerts, downNormals, downUVs, downTangents);
        }



        triVerts[0] = tmpUpVerts[0];
        triVerts[1] = tmpUpVerts[1];
        triVerts[2] = tmpUpVerts[2];

        triNormals[0] = tmpUpNormals[0];
        triNormals[1] = tmpUpNormals[1];
        triNormals[2] = tmpUpNormals[2];

        triUvs[0] = tmpUpUvs[0];
        triUvs[1] = tmpUpUvs[1];
        triUvs[2] = tmpUpUvs[2];

        triTangents[0] = tmpUpTangs[0];
        triTangents[1] = tmpUpTangs[1];
        triTangents[2] = tmpUpTangs[2];

        AddTriToCorrectMeshObject(triVerts, triNormals, triTangents, triUvs, upVerts, uphashVerts, upNormals, upUVs, upTangents);

        if (tmpUpVerts.Count > 3)
        { //for when a triangle is cut into 3 triangles (2 on 1 side and 1 on the other)

            triVerts[0] = tmpUpVerts[0];
            triVerts[1] = tmpUpVerts[2];
            triVerts[2] = tmpUpVerts[3];

            triNormals[0] = tmpUpNormals[0];
            triNormals[1] = tmpUpNormals[2];
            triNormals[2] = tmpUpNormals[3];

            triUvs[0] = tmpUpUvs[0];
            triUvs[1] = tmpUpUvs[2];
            triUvs[2] = tmpUpUvs[3];

            triTangents[0] = tmpUpTangs[0];
            triTangents[1] = tmpUpTangs[2];
            triTangents[2] = tmpUpTangs[3];

            AddTriToCorrectMeshObject(triVerts, triNormals, triTangents, triUvs, upVerts, uphashVerts, upNormals, upUVs, upTangents);
        }
    }

    void BarycentricInterpolation(Vector3 newPoint, ref Vector2 newUV, ref Vector3 newNormal, ref Vector4 newTangent, Vector3[] points, Vector2[] uvs, Vector3[] normals, Vector4[] tangents)
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

}