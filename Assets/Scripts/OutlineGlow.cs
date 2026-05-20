using UnityEngine;
using System.Collections.Generic;

public class OutlineGlow : MonoBehaviour
{
    [Tooltip("Sadece OutlineMat materyalini buraya sürükle, gerisini kod halledecek!")]
    public Material outlineMaterial;

    private MeshRenderer[] renderers;
    private Dictionary<MeshRenderer, Material[]> originalMaterials = new Dictionary<MeshRenderer, Material[]>();
    private bool isGlowing = false;

    // Start yerine Awake kullanýyoruz! Obje yaratýldýðý an listeleri doldurur.
    private void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                originalMaterials[renderer] = renderer.sharedMaterials;
            }
        }
    }

    public void EnableGlow()
    {
        // renderers henüz dolmadýysa patlamamasý için ekstra güvenlik kontrolü
        if (isGlowing || outlineMaterial == null || renderers == null) return;
        isGlowing = true;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            Material[] original = originalMaterials[renderer];
            Material[] newMaterials = new Material[original.Length + 1];
            original.CopyTo(newMaterials, 0);
            newMaterials[newMaterials.Length - 1] = outlineMaterial;

            renderer.sharedMaterials = newMaterials;
        }
    }

    public void DisableGlow()
    {
        if (!isGlowing || renderers == null) return;
        isGlowing = false;

        foreach (var renderer in renderers)
        {
            if (renderer != null && originalMaterials.ContainsKey(renderer))
            {
                renderer.sharedMaterials = originalMaterials[renderer];
            }
        }
    }
}