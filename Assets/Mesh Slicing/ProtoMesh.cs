using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtoMesh {

    public List<int> BodyTris
    {
        get; set;
    }

    public List<int> SubmeshTris
    {
        get; set;
    }

    public ProtoMesh(List<int> bodyTris, List<int> submeshTris)
    {
        this.BodyTris = bodyTris;
        this.SubmeshTris = submeshTris;
    }

    
}
