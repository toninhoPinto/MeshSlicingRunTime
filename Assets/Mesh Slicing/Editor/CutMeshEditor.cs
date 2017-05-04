using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CutMeshV3))]
public class ObjectBuilderEditor3 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CutMeshV3 myScript = (CutMeshV3)target;
        if (GUILayout.Button("Cut Object"))
        {
            myScript.Cut();
        }
    }
}

[CustomEditor(typeof(CutMeshV4))]
public class ObjectBuilderEditor4 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CutMeshV4 myScript = (CutMeshV4)target;
        if (GUILayout.Button("Cut Object"))
        {
            myScript.Cut();
        }
    }
}

[CustomEditor(typeof(CutMeshV5))]
public class ObjectBuilderEditor5 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CutMeshV5 myScript = (CutMeshV5)target;
        if (GUILayout.Button("Cut Object"))
        {
            myScript.Cut();
        }
    }
}

[CustomEditor(typeof(CutMeshV6))]
public class ObjectBuilderEditor6 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CutMeshV6 myScript = (CutMeshV6)target;
        if (GUILayout.Button("Cut Object"))
        {
            myScript.Cut();
        }
    }
}