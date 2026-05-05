using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;
    public int currentMoney = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log(amount + " eklendi. Güncel bakiye: " + currentMoney);
        // İleride buraya UI güncellemelerini de ekleyebilirsin.
    }
}