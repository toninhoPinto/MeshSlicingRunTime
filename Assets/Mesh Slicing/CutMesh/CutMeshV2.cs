using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CutMeshV2 : MonoBehaviour {

    public GameObject target;
    public GameObject prefabPart;
    public bool fancy;
    Vector3 planeNormal;
    Vector3 planePoint;
    Mesh myMesh;

    List<Vector3> upVerts;
    List<int> upTris;
    List<Vector2> upUVs;
    List<Vector3> upNormals;

    List<Vector3> downVerts;
    List<int> downTris;
    List<Vector2> downUVs;
    List<Vector3> downNormals;

    List<Vector3> centerVerts;

    GameObject topPart;
    GameObject bottomPart;

    void Start()
    {
        myMesh = GetComponent<MeshFilter>().mesh;
        Cut();
    }

    public void Cut()
    {
        if (target == null)
            return;
        CutMesh();
        PushParts();
        Destroy(target);
    }

    void CutMesh() {

        //SETUP QUAD VARIABLES==================================================
        planeNormal = transform.TransformVector(myMesh.normals[0]);
        planeNormal = planeNormal.normalized;
        planePoint = transform.TransformPoint(myMesh.vertices[0]);

        Debug.DrawRay(transform.position, planeNormal, Color.red, 1000f);
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

        centerVerts = new List<Vector3>();

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

            Vector3 normal1 = target.transform.TransformVector(normals[tris[i]]).normalized;
            Vector3 normal2 = target.transform.TransformVector(normals[tris[i + 1]]).normalized;
            Vector3 normal3 = target.transform.TransformVector(normals[tris[i + 2]]).normalized;
            bool[] intersected = DoesTriIntersectPlane(worldp1, worldp2, worldp3);

            if (intersected[0] || intersected[1] || intersected[2])
            {
                Vector2[] triUvs = { uv1, uv2, uv3 };
                Vector3[] triVerts = { worldp1, worldp2, worldp3 };
                Vector3[] triNormals = { normal1, normal2, normal3 };

                /*
                Debug.Log(normals[tris[i]]);
                Debug.Log(normals[tris[i + 1]]);
                Debug.Log(normals[tris[i + 2]]);
                Debug.Log(normal1);
                Debug.Log(normal2);
                Debug.Log(normal3);
                */

                HandleIntersectionPoints(intersected, triVerts, triUvs, triNormals);
            }
            else
            {
                if (Mathf.Sign(Vector3.Dot(planeNormal, (worldp1 - planePoint))) > 0)
                {//above
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp1));
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp2));
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp3));
                    upTris.Add(upVerts.Count - 3);
                    upTris.Add(upVerts.Count - 2);
                    upTris.Add(upVerts.Count - 1);
                    upUVs.Add(uv1);
                    upUVs.Add(uv2);
                    upUVs.Add(uv3);
                    upNormals.Add(topPart.transform.InverseTransformVector(normal1).normalized * 3);
                    upNormals.Add(topPart.transform.InverseTransformVector(normal2).normalized * 3);
                    upNormals.Add(topPart.transform.InverseTransformVector(normal3).normalized * 3);
                }
                else
                {
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp1)); //bellow
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp2));
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp3));
                    downTris.Add(downVerts.Count - 3);
                    downTris.Add(downVerts.Count - 2);
                    downTris.Add(downVerts.Count - 1);
                    downUVs.Add(uv1);
                    downUVs.Add(uv2);
                    downUVs.Add(uv3);
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal1).normalized*3);
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal2).normalized * 3);
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal3).normalized * 3);
                }
            }
            //Debug.Log("-------------------------------------");
        }

        Vector3 center = Vector3.zero;
        for (int i = 0; i < centerVerts.Count; i++)
            center += centerVerts[i];
        center /= centerVerts.Count;

        centerVerts = centerVerts.OrderBy(x => Mathf.Atan2((x - center).y, (x - center).x)).ToList();

        HandleIntersectedZone(upVerts, upTris, upUVs, upNormals, center, true);
        HandleIntersectedZone(downVerts, downTris, downUVs, downNormals, center, false);
        CreateParts(topPart, upVerts, upTris, upUVs, upNormals);
        CreateParts(bottomPart, downVerts, downTris, downUVs, downNormals);
    }

    void OnTriggerEnter(Collider other)
    {
        target = other.gameObject;
    }

    void HandleIntersectedZone(List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals, Vector3 center, bool top)
    {
        List<int> centerTris = new List<int>();

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
        } else
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

    void PushParts()
    {
        if (fancy)
        {
            topPart.GetComponent<Rigidbody>().useGravity = true;
            bottomPart.GetComponent<Rigidbody>().useGravity = true;

            topPart.GetComponent<Rigidbody>().AddForce(-transform.forward.normalized * 5f, ForceMode.Impulse);
        }
    }

    void CreateParts(GameObject part, List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals)
    {
        Debug.Log(partVerts.Count);
        Debug.Log(partTris.Count);
        Debug.Log(partUvs.Count);
        Debug.Log(partNormals.Count);

        Mesh partMesh = part.GetComponent<MeshFilter>().mesh;

        partMesh.Clear();
        partMesh.vertices = partVerts.ToArray();
        partMesh.triangles = partTris.ToArray();
        partMesh.uv = partUvs.ToArray();
        partMesh.normals = partNormals.ToArray();
        partMesh.RecalculateBounds();
    }

    bool[] DoesTriIntersectPlane(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, p1 - planePoint));
        //upOrDown = upOrDown > 0 ? 1 : (upOrDown < 0 ? -1 : 0);
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, p2 - planePoint));
        //upOrDown2 = upOrDown2 > 0 ? 1 : (upOrDown2 < 0 ? -1 : 0);
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, p3 - planePoint));
        //upOrDown3 = upOrDown3 > 0 ? 1 : (upOrDown3 < 0 ? -1 : 0);

        bool intersect1 = upOrDown != upOrDown2;
        bool intersect2 = upOrDown2 != upOrDown3;
        bool intersect3 = upOrDown != upOrDown3;

        bool[] intersections = { intersect1, intersect2, intersect3 };

        return intersections;
    }

    void HandleIntersectionPoints(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals)
    {
        List<Vector3> tmpUpVerts = new List<Vector3>();
        List<Vector3> tmpDownVerts = new List<Vector3>();
        bool secondLine = false;

        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, verts[0] - planePoint));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, verts[1] - planePoint));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, verts[2] - planePoint));

        if (intersections[0])
        {
            AddToCorrectSideList(upOrDown, 0, 1, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        if (intersections[1])
        {
            AddToCorrectSideList(upOrDown2, 1, 2, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        if (intersections[2])
        {
            AddToCorrectSideList(upOrDown3, 2, 0, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        HandleTriOrder(tmpUpVerts, tmpDownVerts);
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

    void AddToCorrectSideList(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, List<Vector3> top, List<Vector3> bottom)
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
        Vector2 newUv = new Vector2(0,0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        HandleBaryCentric(newVert, ref newUv, ref newNormal, verts, uvs, normals);

        if (upOrDown>0)
        {

            if (AddUniquelyToList(topPart.transform.InverseTransformPoint(p1), top))
            {
                upUVs.Add(uv1);
                upNormals.Add(topPart.transform.InverseTransformVector(n1).normalized*3);
            }
            if (AddUniquelyToList(topPart.transform.InverseTransformPoint(newVert), top))
            {
                upUVs.Add(newUv);
                upNormals.Add(topPart.transform.InverseTransformVector(newNormal).normalized * 3);
            }

            if (AddUniquelyToList(bottomPart.transform.InverseTransformPoint(newVert), bottom))
            {
                downUVs.Add(newUv);
                downNormals.Add(bottomPart.transform.InverseTransformVector(newNormal).normalized * 3);
            }
            if (AddUniquelyToList(bottomPart.transform.InverseTransformPoint(p2), bottom))
            {
                downUVs.Add(uv2);
                downNormals.Add(bottomPart.transform.InverseTransformVector(n2).normalized * 3);
            }

            centerVerts.Add(topPart.transform.InverseTransformPoint(newVert));
        }
        else
        {
            if (AddUniquelyToList(topPart.transform.InverseTransformPoint(newVert), top))
            {
                upUVs.Add(newUv);
                upNormals.Add(topPart.transform.InverseTransformVector(newNormal).normalized * 3);
            }
            if (AddUniquelyToList(topPart.transform.InverseTransformPoint(p2), top))
            {
                upUVs.Add(uv2);
                upNormals.Add(topPart.transform.InverseTransformVector(n2).normalized * 3);
            }

            if (AddUniquelyToList(bottomPart.transform.InverseTransformPoint(p1), bottom))
            {
                downUVs.Add(uv1);
                downNormals.Add(bottomPart.transform.InverseTransformVector(n1).normalized * 3);
            }
            if (AddUniquelyToList(bottomPart.transform.InverseTransformPoint(newVert), bottom))
            {
                downUVs.Add(newUv);
                downNormals.Add(bottomPart.transform.InverseTransformVector(newNormal).normalized * 3);
            }

            centerVerts.Add(bottomPart.transform.InverseTransformPoint(newVert));
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
