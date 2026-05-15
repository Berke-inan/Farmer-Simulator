using UnityEngine;
using System.Collections.Generic;

public class OutlineGlow : MonoBehaviour
{
    [Tooltip("Sadece OutlineMat materyalini buraya sürükle, gerisini kod halledecek!")]
    public Material outlineMaterial;

    private MeshRenderer[] renderers;
    private Dictionary<MeshRenderer, Material[]> originalMaterials = new Dictionary<MeshRenderer, Material[]>();
    private bool isGlowing = false;

    private void Start()
    {
        // SÝHÝRLÝ SATIR: Kodu attýðýn ana objenin içindeki (Child) tüm Mesh'leri otomatik bulur
        renderers = GetComponentsInChildren<MeshRenderer>(true);

        // Baþlangýçtaki orijinal dokularý hafýzaya al
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
        if (isGlowing || outlineMaterial == null) return;
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
        if (!isGlowing) return;
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