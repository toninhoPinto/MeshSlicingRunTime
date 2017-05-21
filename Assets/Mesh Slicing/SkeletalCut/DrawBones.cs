// DrawBones.cs
using UnityEngine;

public class DrawBones : MonoBehaviour
{
    private SkinnedMeshRenderer m_Renderer;

    void Start()
    {
        m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (m_Renderer == null)
        {
            Debug.LogWarning("No SkinnedMeshRenderer found, script removed");
            Destroy(this);
        }
    }

    void LateUpdate()
    {
        var bones = m_Renderer.bones;
        for (int i = 0; i < bones.Length; i++)
        {
            Debug.DrawLine(bones[i].position, bones[i].parent.position, Color.white);
        }
    }
}