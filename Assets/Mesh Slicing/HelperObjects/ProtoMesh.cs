using System.Collections.Generic;

//This struct encapsulates both the body triangles and the cut-hull triangles
public struct ProtoMesh {

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
        BodyTris = bodyTris;
        SubmeshTris = submeshTris;
    }

}
