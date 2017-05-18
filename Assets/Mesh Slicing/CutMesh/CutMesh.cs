using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CutMesh : MonoBehaviour {

    public GameObject target;
    public GameObject prefabPart;
    public bool fancy;
    Vector3 planeNormal;
    Vector3 planePoint;
    Mesh myMesh;

    List<Vector3> upVerts;
    List<int> upTris;
    List<Vector3> downVerts;
    List<int> downTris;
    List<Vector3> centerVerts;

    GameObject topPart;
    GameObject bottomPart;

    // Use this for initialization
    void Start () {
        //needs verification for object itself intersection 

        //SETUP QUAD VARIABLES==================================================
        myMesh = GetComponent<MeshFilter>().mesh;
        planeNormal = transform.rotation * myMesh.normals[0];
        planeNormal = planeNormal.normalized;
        planePoint = transform.TransformPoint(myMesh.vertices[0]);

        Debug.DrawRay(transform.position, transform.rotation * myMesh.normals[0] * 10, Color.red, 1000f);
        //==================================================

        Mesh targetMesh = target.GetComponent<MeshFilter>().mesh;

        int[] tris = targetMesh.triangles;
        upVerts = new List<Vector3>();
        upTris = new List<int>();
        downVerts = new List<Vector3>();
        downTris = new List<int>();
        centerVerts = new List<Vector3>();

        topPart = Instantiate(prefabPart);
        bottomPart = Instantiate(prefabPart);

        for (int i = 0; i < tris.Length; i+=3)
        {
            Vector3 worldp1 = target.transform.TransformPoint(targetMesh.vertices[tris[i]]);
            Vector3 worldp2 = target.transform.TransformPoint(targetMesh.vertices[tris[i+1]]);
            Vector3 worldp3 = target.transform.TransformPoint(targetMesh.vertices[tris[i+2]]);

            Vector3 triNormal = target.transform.TransformVector(targetMesh.normals[tris[i]]) + target.transform.TransformVector(targetMesh.normals[tris[i+1]]) + target.transform.TransformVector(targetMesh.normals[tris[i+2]]);
            triNormal /= 3;

            /*
            Debug.DrawRay(worldp1, -Vector3.right * 1 * .5f, Color.blue, 110f);
            Debug.DrawRay(worldp2, -Vector3.right * 2 * .5f, Color.blue, 110f);
            Debug.DrawRay(worldp3, -Vector3.right * 3 * .5f, Color.blue, 110f);
            */

            bool[] intersected = DoesTriIntersectPlane(worldp1, worldp2, worldp3, triNormal);

            if (intersected[0] || intersected[1] || intersected[2])
            {
                HandleIntersectionPoints(intersected, worldp1, worldp2, worldp3, triNormal);
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
                }
                else
                {
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp1)); //bellow
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp2));
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp3));
                    downTris.Add(downVerts.Count - 3);
                    downTris.Add(downVerts.Count - 2);
                    downTris.Add(downVerts.Count - 1);
                }
            }
            
        }

        Vector3 center = Vector3.zero;
        for (int i = 0; i < centerVerts.Count; i++)
            center += centerVerts[i];
        center /= centerVerts.Count;

        Debug.Log(centerVerts.Count);
        centerVerts = centerVerts.OrderBy(x => Mathf.Atan2((x - center).y, (x - center).x)).ToList();
        Debug.Log(centerVerts.Count);

        HandleIntersectedZone(upVerts, upTris, center, true);
        HandleIntersectedZone(downVerts, downTris, center, false);
        CreateParts(topPart.GetComponent<MeshFilter>().mesh, upVerts, upTris);
        CreateParts(bottomPart.GetComponent<MeshFilter>().mesh, downVerts, downTris);
        PushParts();
        target.GetComponent<Renderer>().enabled = false;
        
    }

    void HandleIntersectedZone(List<Vector3> partVerts, List<int> partTris, Vector3 center, bool top)  //bool = preguiça
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
        }else
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

    void CreateParts(Mesh partMesh, List<Vector3> partVerts, List<int> partTris)
    {
        Debug.Log(partVerts.Count);
        Debug.Log(partTris.Count);

        partMesh.Clear();
        partMesh.vertices = partVerts.ToArray();
        partMesh.triangles = partTris.ToArray();
        partMesh.RecalculateBounds();
        partMesh.RecalculateNormals();
    }

    bool[] DoesTriIntersectPlane(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 triNormal)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, (p1 - planePoint)));
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

    void HandleIntersectionPoints(bool[] intersections, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 triNormal)
    {
            List<Vector3> tmpUpVerts = new List<Vector3>();
            List<Vector3> tmpDownVerts = new List<Vector3>();
            bool secondLine = false;

            float upOrDown = Mathf.Sign(Vector3.Dot(planeNormal, p1 - planePoint));
            float upOrDown2 = Mathf.Sign(Vector3.Dot(planeNormal, p2 - planePoint));
            float upOrDown3 = Mathf.Sign(Vector3.Dot(planeNormal, p3 - planePoint));

            if (intersections[0])
            {
                Vector3 rayDir = (p2 - p1).normalized;
                float t = Vector3.Dot(planePoint - p1, planeNormal) / Vector3.Dot(rayDir, planeNormal);
                Vector3 newVert = p1 + rayDir * t;
                AddToCorrectSideList(upOrDown, newVert, p1, p2, tmpUpVerts, tmpDownVerts, secondLine);
                secondLine = true;
            }
            if (intersections[1])
            {
                Vector3 rayDir = (p3 - p2).normalized;
                float t = Vector3.Dot(planePoint - p2, planeNormal) / Vector3.Dot(rayDir, planeNormal);
                Vector3 newVert = p2 + rayDir * t;
                AddToCorrectSideList(upOrDown2, newVert, p2, p3, tmpUpVerts, tmpDownVerts, secondLine);
            }
            if (intersections[2])
            {
                Vector3 rayDir = (p1 - p3).normalized;
                float t = Vector3.Dot(planePoint - p3, planeNormal) / Vector3.Dot(rayDir, planeNormal);
                Vector3 newVert = p3 + rayDir * t;
                AddToCorrectSideList(upOrDown3, newVert, p3, p1, tmpUpVerts, tmpDownVerts, true);
            }
            HandleTriOrder(tmpUpVerts, tmpDownVerts, triNormal);
    }

    void HandleTriOrder(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts, Vector3 triNormal)
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

    void AddToCorrectSideList(float upOrDown, Vector3 newVert, Vector3 p1, Vector3 p2, List<Vector3> top, List<Vector3> bottom, bool secondLine)
    {
        if (upOrDown>0)
        {

            AddUniquelyToList(topPart.transform.InverseTransformPoint(p1), top);
            AddUniquelyToList(topPart.transform.InverseTransformPoint(newVert), top);

            AddUniquelyToList(bottomPart.transform.InverseTransformPoint(newVert), bottom);
            AddUniquelyToList(bottomPart.transform.InverseTransformPoint(p2), bottom);
            centerVerts.Add(topPart.transform.InverseTransformPoint(newVert));
        }
        else
        {
            AddUniquelyToList(topPart.transform.InverseTransformPoint(newVert), top);
            AddUniquelyToList(topPart.transform.InverseTransformPoint(p2), top);

            AddUniquelyToList(bottomPart.transform.InverseTransformPoint(p1), bottom);
            AddUniquelyToList(bottomPart.transform.InverseTransformPoint(newVert), bottom);
            centerVerts.Add(bottomPart.transform.InverseTransformPoint(newVert));
        }
    }

    void AddUniquelyToList(Vector3 vertex, List<Vector3> list) //order is important so cant use HashSet
    {
        if (!list.Contains(vertex))
            list.Add(vertex);
    }

    /*
    void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        if (centerVerts != null)
        {
            for (int i = 0; i < centerVerts.Count; i++)
            {
                Vector3 currentVertice = topPart.transform.TransformPoint(centerVerts[i]);
                Gizmos.DrawRay(currentVertice, Vector3.down * i * 0.003f);
                Gizmos.DrawSphere(currentVertice, 0.008f);
            }
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(topPart.transform.TransformPoint(upVerts[upVerts.Count - 1]), 0.01f);
    }*/
}
