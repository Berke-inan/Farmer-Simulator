using UnityEngine;
using Unity.Netcode;

public class Candle : NetworkBehaviour, IInteractable
{
    public GameObject flameObject;
    public Light candleLight;

    private bool isLit = false;

    [Header("Titreme Ayarlarý")]
    public float minIntensity = 3f;
    public float maxIntensity = 3.5f;
    public float flickerSpeed = 0.08f;

    private float targetIntensity;

    void Start()
    {
        targetIntensity = maxIntensity;
    }

    void Update()
    {
        if (isLit && candleLight != null)
        {
            candleLight.intensity = Mathf.Lerp(
                candleLight.intensity,
                targetIntensity,
                Time.deltaTime * 10f
            );

            if (Mathf.Abs(candleLight.intensity - targetIntensity) < 0.05f)
            {
                targetIntensity = Random.Range(minIntensity, maxIntensity);
            }
        }
    }

    public void Interact(NetworkObject interactor)
    {
        isLit = !isLit;

        if (flameObject != null)
            flameObject.SetActive(isLit);

        if (candleLight != null)
            candleLight.enabled = isLit;
    }
}