using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CutSimpleConcave))]
public class ObjectBuilderEditor3 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CutSimpleConcave myScript = (CutSimpleConcave)target;
        if (GUILayout.Button("Cut Object"))
        {
            myScript.Cut();
        }
    }
}

[CustomEditor(typeof(CutMultiplePartsConcave))]
public class ObjectBuilderEditor8 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CutMultiplePartsConcave myScript = (CutMultiplePartsConcave)target;
        if (GUILayout.Button("Cut Object"))
        {
            myScript.Cut();
        }
    }
}