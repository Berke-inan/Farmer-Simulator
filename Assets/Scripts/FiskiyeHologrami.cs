using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FiskiyeHologrami : MonoBehaviour
{
    private Mesh mesh;

    public void AlanCiz(float menzil, float aci)
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        int dilimSayisi = 30; // Alanýn ne kadar yuvarlak/pürüzsüz görüneceði
        Vector3[] vertices = new Vector3[dilimSayisi + 2];
        int[] triangles = new int[dilimSayisi * 3];

        // 1. Merkez Nokta (Fýskiyenin dibi)
        vertices[0] = Vector3.zero;

        // 2. Açýyý hesaplayýp noktalarý yerleþtirme (Fýskiyenin dönüþ yönüyle birebir ayný)
        for (int i = 0; i <= dilimSayisi; i++)
        {
            // Fýskiye 0'dan baþlayýp kendi açýsýna (Örn: 120) kadar döner
            float guncelAci = aci * ((float)i / dilimSayisi);
            float rad = guncelAci * Mathf.Deg2Rad;

            // Unity'de Y ekseni etrafýnda dönüþ saat yönündedir (Sin = X, Cos = Z)
            vertices[i + 1] = new Vector3(Mathf.Sin(rad) * menzil, 0, Mathf.Cos(rad) * menzil);
        }

        // 3. Üçgenleri (Yüzeyleri) oluþturma
        for (int i = 0; i < dilimSayisi; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // Iþýklarýn düzgün vurmasý için
    }
}