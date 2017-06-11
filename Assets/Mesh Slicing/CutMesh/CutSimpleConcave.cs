using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;

public struct Edge
{
    public Vector3 start;
    public Vector3 end;

    public Edge(Vector3 s, Vector3 e)
    {
        start = s;
        end = e;
    }
}

public class CutSimpleConcave : MonoBehaviour
{

    public GameObject target;
    public GameObject prefabPart;

    Vector3 planeNormal;
    Vector3 planePoint;

    List<Vector3> upVerts;
    List<int> upTris;
    List<Vector2> upUVs;
    List<Vector3> upNormals;

    List<Vector3> downVerts;
    List<int> downTris;
    List<Vector2> downUVs;
    List<Vector3> downNormals;

    List<Edge> centerEdges;

    GameObject topPart;
    GameObject bottomPart;

    Vector3[] triVerts;
    Vector2[] triUvs;
    Vector3[] triNormals;

    void Start()
    {
        triVerts = new Vector3[3];
        triUvs = new Vector2[3];
        triNormals = new Vector3[3];
    }

    void OnTriggerEnter(Collider other)
    {
        target = other.gameObject;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Cut();
        }
    }

    public void Cut()
    {
        if (target == null)
            return;
        CutMesh();
    }

    void CutMesh()
    {
        //SETUP QUAD VARIABLES==================================================
        planeNormal = (-transform.forward).normalized; //cheaper than accessing the normals of the mesh
        planePoint = transform.position; //cheaper than accessing the vertexes of the mesh
        //==================================================

        Mesh targetMesh = target.GetComponent<MeshFilter>().mesh;

        int[] tris = targetMesh.triangles;
        Vector2[] uvs = targetMesh.uv;
        Vector3[] verts = targetMesh.vertices;
        Vector3[] normals = targetMesh.normals;

        upVerts = new List<Vector3>();
        upTris = new List<int>();
        upUVs = new List<Vector2>();
        upNormals = new List<Vector3>();

        downVerts = new List<Vector3>();
        downTris = new List<int>();
        downUVs = new List<Vector2>();
        downNormals = new List<Vector3>();

        centerEdges = new List<Edge>();

        topPart = Instantiate(prefabPart);
        bottomPart = Instantiate(prefabPart);

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
            bool[] intersected = isTriIntersectingPlane(worldp1, worldp2, worldp3);

            if (intersected[0] || intersected[1] || intersected[2]) //if triangle intersects with plane
            {
                triVerts[0] = worldp1;
                triVerts[1] = worldp2;
                triVerts[2] = worldp3;

                triUvs[0] = uv1;
                triUvs[1] = uv2;
                triUvs[2] = uv3;

                triNormals[0] = normal1;
                triNormals[1] = normal2;
                triNormals[2] = normal3;
                TrianglePlaneIntersection(intersected, triVerts, triUvs, triNormals);
            }
            else
            {
                //if the triangle does not intersect but instead is above the plane
                //copy the vertices, the triangles, the uvs and the normals and store it in the lists for the new mesh above the plane
                if (Mathf.Sign(Vector3.Dot(planeNormal, (worldp1 - planePoint))) > 0) 
                {
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp1));
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp2));
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp3));
                    upTris.Add(upVerts.Count - 3);
                    upTris.Add(upVerts.Count - 2);
                    upTris.Add(upVerts.Count - 1);
                    upUVs.Add(uv1);
                    upUVs.Add(uv2);
                    upUVs.Add(uv3);
                    upNormals.Add(topPart.transform.InverseTransformVector(normal1).normalized);
                    upNormals.Add(topPart.transform.InverseTransformVector(normal2).normalized);
                    upNormals.Add(topPart.transform.InverseTransformVector(normal3).normalized);
                }
                else  //if the triangle does not intersect but instead is below the plane
                {//copy the vertices, the triangles, the uvs and the normals and store it in the lists for the new mesh below the plane
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp1)); //below
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp2));
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp3));
                    downTris.Add(downVerts.Count - 3);
                    downTris.Add(downVerts.Count - 2);
                    downTris.Add(downVerts.Count - 1);
                    downUVs.Add(uv1);
                    downUVs.Add(uv2);
                    downUVs.Add(uv3);
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal1).normalized);
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal2).normalized);
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal3).normalized);
                }
            }
        }
        //if it did not intersect, destroy the objects
        if (centerEdges.Count == 0)
        {
            Destroy(topPart);
            Destroy(bottomPart);
            return;
        }

        List<List<int>> groupedVerts = FindGroupNewIntersectedVertecis();
        HandleIntersectedZone(upVerts, upTris, upUVs, upNormals, groupedVerts, true);
        HandleIntersectedZone(downVerts, downTris, downUVs, downNormals, groupedVerts, false);
        CreateParts(topPart, upVerts, upTris, upUVs, upNormals);
        CreateParts(bottomPart, downVerts, downTris, downUVs, downNormals);
        topPart.name = "top";
        bottomPart.name = "bottom";
        Destroy(target);
    }

    // If a single object was cut into three objects, the intersected hulls/caps need to be split 
    // Picks an edge (2 vertecis), and verifies if there is any edge that shares one of the vertecis
    // if it does, it extends the edge
    // keep checking if an edge shares a vertex until the edge meets itself which means it did a loop
    // once it does a loop, check what is the next not-visited edge and restart the algorithm using that edge
    // it returns a list of cycled indexes of the actual edges
    List<List<int>> FindGroupNewIntersectedVertecis()
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

    // Creates the actual cap of the intersections
    // uses the indexs of the center grouped vertexes and creates a list of actual points
    // average the points and create a center
    // depending if the plane is completely vertical or not, order the points in a clockwise fashion
    // create the triangles
    void HandleIntersectedZone(List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals, List<List<int>> centerGroups, bool top)
    {
        for (int k = 0; k < centerGroups.Count; k++)
        {
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

            int sizeVertsBeforeCenter = partVerts.Count;

            if (planeNormal.y != 0)
            {
                float normalDir = Mathf.Sign(planeNormal.y);
                partVerts.AddRange(centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).z, (x - center).x)));
            }
            else
            {
                float normalDir = Mathf.Sign(planeNormal.z);
                partVerts.AddRange(centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).x, (x - center).y)));
            }

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
        }
    }

    // Receive all the data and create the mesh
    void CreateParts(GameObject part, List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals)
    {
        Mesh partMesh = part.GetComponent<MeshFilter>().mesh;

        partMesh.Clear();
        partMesh.vertices = partVerts.ToArray();
        partMesh.triangles = partTris.ToArray();
        partMesh.uv = partUvs.ToArray();
        partMesh.normals = partNormals.ToArray();
        partMesh.RecalculateBounds();
    }

    //Verifies if a tri is intersecting this plane
    bool[] isTriIntersectingPlane(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, p1 - planePoint));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, p2 - planePoint));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, p3 - planePoint));

        bool intersect1 = upOrDown != upOrDown2;
        bool intersect2 = upOrDown2 != upOrDown3;
        bool intersect3 = upOrDown != upOrDown3;

        bool[] intersections = { intersect1, intersect2, intersect3 };

        return intersections;
    }

    //Does 3 line-plane intersection in order to calculate a new vertex
    void TrianglePlaneIntersection(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals)
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
            newVectors[newVectorIndex] = RayPlaneIntersection(upOrDown, 0, 1, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
            newVectorIndex++;
        }
        if (intersections[1])
        {
            newVectors[newVectorIndex] = RayPlaneIntersection(upOrDown2, 1, 2, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
            newVectorIndex++;
        }
        if (intersections[2])
        {
            newVectors[newVectorIndex] = RayPlaneIntersection(upOrDown3, 2, 0, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }

        //only 2 new vectors in all cases of triangle/plane intersection
        centerEdges.Add(new Edge(newVectors[0], newVectors[1]));

        HandleTriOrder(tmpUpVerts, tmpDownVerts);
    }

    //receives the two points of the line
    //receives the normals, uvs of the triangle
    //receives if the line of the triangle is going upwards towards the plane or downards towards the plane
    //create the new point that intersects the line with the plane
    //uses barycentric interpolation to find new uv and new normal
    //depending on the direction add the points in a specific order
    //add points both on the top and bottom vertecis list
    Vector3 RayPlaneIntersection(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, List<Vector3> top, List<Vector3> bottom)
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

        //---------------------------------
        Vector3 topNewVert = topPart.transform.InverseTransformPoint(newVert);
        Vector3 botNewVert = bottomPart.transform.InverseTransformPoint(newVert);
        Vector3 topNewNormal = topPart.transform.InverseTransformVector(newNormal).normalized;
        Vector3 botNewNormal = bottomPart.transform.InverseTransformVector(newNormal).normalized;

        if (upOrDown > 0)
        {
            p1 = topPart.transform.InverseTransformPoint(p1);
            p2 = bottomPart.transform.InverseTransformPoint(p2);
            n1 = topPart.transform.InverseTransformVector(n1).normalized;
            n2 = bottomPart.transform.InverseTransformVector(n2).normalized;

            if (!top.Contains(p1))
            {
                top.Add(p1);
                upUVs.Add(uv1);
                upNormals.Add(n1);
            }

            top.Add(topNewVert);
            upUVs.Add(newUv);
            upNormals.Add(topNewNormal);

            bottom.Add(botNewVert);
            downUVs.Add(newUv);
            downNormals.Add(botNewNormal);

            if (!bottom.Contains(p2))
            {
                bottom.Add(p2);
                downUVs.Add(uv2);
                downNormals.Add(n2);
            }

            return topNewVert;
        }
        else
        {
            p2 = topPart.transform.InverseTransformPoint(p2);
            p1 = bottomPart.transform.InverseTransformPoint(p1);
            n2 = topPart.transform.InverseTransformVector(n2).normalized;
            n1 = bottomPart.transform.InverseTransformVector(n1).normalized;

            top.Add(topNewVert);
            upUVs.Add(newUv);
            upNormals.Add(topPart.transform.InverseTransformVector(newNormal).normalized);

            if (!top.Contains(p2))
            {
                top.Add(p2);
                upUVs.Add(uv2);
                upNormals.Add(n2);
            }
            if (!bottom.Contains(p1))
            {
                bottom.Add(p1);
                downUVs.Add(uv1);
                downNormals.Add(n1);
            }

            bottom.Add(botNewVert);
            downUVs.Add(newUv);
            downNormals.Add(botNewNormal);

            return botNewVert;
        }
    }

    //Create triangles for the new intersected vertecis
    void HandleTriOrder(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts)
    {
        int upLastInsert = upVerts.Count;
        int downLastInsert = downVerts.Count;

        downVerts.AddRange(tmpDownVerts);
        upVerts.AddRange(tmpUpVerts);

        upTris.Add(upLastInsert);
        upTris.Add(upLastInsert + 1);
        upTris.Add(upLastInsert + 2);

        if (tmpUpVerts.Count > 3)
        {
            upTris.Add(upLastInsert);
            upTris.Add(upLastInsert + 2);
            upTris.Add(upLastInsert + 3);
        }

        downTris.Add(downLastInsert);
        downTris.Add(downLastInsert + 1);
        downTris.Add(downLastInsert + 2);

        if (tmpDownVerts.Count > 3)
        {
            downTris.Add(downLastInsert);
            downTris.Add(downLastInsert + 2);
            downTris.Add(downLastInsert + 3);
        }

    }

    //used to be able to create new Uv's and new Normals
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


}
