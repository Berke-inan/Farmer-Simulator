using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Envanter Ayarları")]
    public int maxSlots = 10;
    public InventorySlot[] slots;

    [Header("El Görselleri")]
    public Transform handTransform;
    public GameObject eldekiObje;

    public NetworkVariable<bool> isHolstered = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> activeHotbarIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public event Action<int, InventorySlot> OnSlotChanged;

    private void Awake()
    {
        if (slots == null || slots.Length != maxSlots)
        {
            slots = new InventorySlot[maxSlots];
            for (int i = 0; i < maxSlots; i++) slots[i] = new InventorySlot();
        }
    }

    public override void OnNetworkSpawn()
    {
        activeHotbarIndex.OnValueChanged += HandleHotbarChanged;
        isHolstered.OnValueChanged += HandleHolsterChanged; // YENİ EKLENDİ
        UpdateHeldItemVisuals(activeHotbarIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        activeHotbarIndex.OnValueChanged -= HandleHotbarChanged;
        isHolstered.OnValueChanged -= HandleHolsterChanged; // YENİ EKLENDİ
    }

    private void HandleHotbarChanged(int previousIndex, int newIndex)
    {
        UpdateHeldItemVisuals(newIndex);
    }

    private void HandleHolsterChanged(bool previousVal, bool newVal)
    {
        UpdateHeldItemVisuals(activeHotbarIndex.Value);
    }

    // --- YENİ: Zıplatma İsteği ---
    [Rpc(SendTo.Server)]
    public void RequestBounceServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out Rigidbody rb))
            {
                // Eşyayı havaya zıplatma fiziği
                rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestPickupServerRpc(ulong networkObjectId, RpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj)) return;

        // --- YENİ: ÇALIŞAN FISKİYEYİ ALMA KORUMASI ---
        if (netObj.TryGetComponent(out SprinkleController sprinkler))
        {
            if (sprinkler.calisiyorMu.Value)
            {
                Debug.Log("Çalışan fıskiyeyi yerden alamazsın! Önce vanayı kapatmalısın.");
                return; // Fıskiye çalışıyorsa yerden alma işlemini İPTAL ET
            }
        }

        if (netObj.TryGetComponent(out InteractableItem groundItem))
        {
            int idToSend = groundItem.itemID;
            int amountToSend = groundItem.amount;
            netObj.Despawn();

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
            };

            AddLocalItemClientRpc(idToSend, amountToSend, clientRpcParams);
        }
    }

    [ClientRpc]
    private void AddLocalItemClientRpc(int itemID, int amount, ClientRpcParams clientRpcParams = default)
    {
        if (ItemRegistry.Instance == null || ItemRegistry.Instance.itemDatabase == null) return;
        ItemData fetchedData = ItemRegistry.Instance.itemDatabase.GetItemByID(itemID);
        if (fetchedData == null) return;

        // --- DEĞİŞİKLİK: Sadece aktif slota bakıyoruz ---
        int i = activeHotbarIndex.Value;

        // 1. Durum: Slot aynı eşya ile dolu mu ve stacklenebilir mi?
        if (!slots[i].IsEmpty && slots[i].itemData == fetchedData && slots[i].amount < fetchedData.maxStack)
        {
            int space = fetchedData.maxStack - slots[i].amount;
            int addAmount = Mathf.Min(space, amount);
            slots[i].amount += addAmount;
            OnSlotChanged?.Invoke(i, slots[i]);
            UpdateHeldItemVisuals(i);
        }
        // 2. Durum: Slot boş mu?
        else if (slots[i].IsEmpty)
        {
            slots[i].AddItem(fetchedData, amount);
            OnSlotChanged?.Invoke(i, slots[i]);
            UpdateHeldItemVisuals(i);
        }
    }

    public void ToggleHolster()
    {
        if (!IsOwner) return;
        isHolstered.Value = !isHolstered.Value; // Durumu tersine çevir
    }

    public void SetHolstered(bool state)
    {
        if (!IsOwner) return;
        isHolstered.Value = state; // Durumu zorla ayarla (Traktör için)
    }

    public void ChangeHotbarSlot(int index)
    {
        if (!IsOwner) return;
        if (index >= 0 && index < maxSlots)
        {
            activeHotbarIndex.Value = index;

            // YENİ: 1-9 arası bir tuşa basılırsa eşya otomatik olarak geri gelsin
            if (isHolstered.Value)
            {
                isHolstered.Value = false;
            }
        }
    }

    private void UpdateHeldItemVisuals(int slotIndex)
    {
        if (eldekiObje != null) Destroy(eldekiObje);

        // YENİ: Eğer gizlenme modu açıksa, eskiyi sildikten sonra yenisini oluşturmadan çık
        if (isHolstered.Value) return;

        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        InventorySlot currentSlot = slots[slotIndex];
        if (currentSlot.IsEmpty || currentSlot.itemData == null || currentSlot.itemData.heldModelPrefab == null) return;

        eldekiObje = Instantiate(currentSlot.itemData.heldModelPrefab, handTransform);
        eldekiObje.transform.localPosition = currentSlot.itemData.holdPositionOffset;
        eldekiObje.transform.localEulerAngles = currentSlot.itemData.holdRotationOffset;
    }

    // --- YENİ: Slot Dolu mu Kontrolü (Local) ---
    public bool CanPickupToActiveSlot(int itemID)
    {
        InventorySlot activeSlot = slots[activeHotbarIndex.Value];
        if (activeSlot.IsEmpty) return true;

        if (activeSlot.itemData.itemID == itemID && activeSlot.amount < activeSlot.itemData.maxStack)
            return true;

        return false;
    }

    public void EldekiniYereAt()
    {
        if (!IsOwner) return;
        InventorySlot activeSlot = slots[activeHotbarIndex.Value];
        if (activeSlot.IsEmpty) return;

        int idToDrop = activeSlot.itemData.itemID;
        activeSlot.amount--;

        if (activeSlot.amount <= 0)
        {
            activeSlot.ClearSlot();
            UpdateHeldItemVisuals(activeHotbarIndex.Value);
        }

        OnSlotChanged?.Invoke(activeHotbarIndex.Value, activeSlot);
        Transform camTransform = GetComponent<PlayerInteractor>().playerCamera;
        DropItemServerRpc(idToDrop, handTransform.position, camTransform.forward);
    }

    [Rpc(SendTo.Server)]
    private void DropItemServerRpc(int itemID, Vector3 dropPosition, Vector3 forwardDirection, RpcParams rpcParams = default)
    {
        if (ItemRegistry.Instance == null || ItemRegistry.Instance.itemDatabase == null) return;
        ItemData data = ItemRegistry.Instance.itemDatabase.GetItemByID(itemID);
        if (data == null || data.groundPrefab == null) return;

        Vector3 spawnPos = dropPosition + forwardDirection * 1.5f;
        GameObject droppedObj = Instantiate(data.groundPrefab, spawnPos, Quaternion.identity);

        // 1. Yerden alabilmek için
        if (droppedObj.TryGetComponent(out InteractableItem groundItem))
            groundItem.itemID = itemID;



        NetworkObject netObj = droppedObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        if (droppedObj.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(forwardDirection * 2f + Vector3.up * 2f, ForceMode.Impulse);
        }
    }
    [Header("Sepet Verileri")]
    public NetworkVariable<int> sepetDoluluk = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Rpc(SendTo.Server)]
    public void ToplamaIstegiServerRpc(ulong agacID)
    {
        if (sepetDoluluk.Value >= 2) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(agacID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TreeController agac))
            {
                if (agac.MeyveHasatEt()) sepetDoluluk.Value++;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void YakitAktarServerRpc(ulong traktorID, float miktar, ulong istasyonID)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(traktorID, out NetworkObject tNet) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(istasyonID, out NetworkObject iNet))
        {
            var traktor = tNet.GetComponent<TractorFuelSystem>();
            var istasyon = iNet.GetComponent<YakitIstasyonu>();

            float gercektenCekilen = istasyon.YakitCek(miktar);
            if (gercektenCekilen > 0) traktor.AddFuelServerRpc(gercektenCekilen);
        }
    }

    [Rpc(SendTo.Server)]
    public void EldeTuketimYapServerRpc()
    {
        int index = activeHotbarIndex.Value;
        if (slots[index].IsEmpty) return;

        slots[index].amount--;
        if (slots[index].amount <= 0) slots[index].ClearSlot();

        TuketimSonucuClientRpc(index, slots[index].amount);
    }

    [Rpc(SendTo.Everyone)]
    private void TuketimSonucuClientRpc(int index, int newAmount)
    {
        if (newAmount <= 0)
        {
            slots[index].ClearSlot();
            UpdateHeldItemVisuals(index);
        }
        else
        {
            slots[index].amount = newAmount;
        }
        OnSlotChanged?.Invoke(index, slots[index]);
    }

    [Header("Alet Verileri")]
    public NetworkVariable<float> bidonMevcutYakit = new NetworkVariable<float>(25f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Rpc(SendTo.Server)]
    public void BidondanTraktoreServerRpc(ulong traktorID, float miktar)
    {
        if (bidonMevcutYakit.Value < miktar) miktar = bidonMevcutYakit.Value;
        if (miktar <= 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(traktorID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TractorFuelSystem traktor))
            {
                float bosYer = traktor.maxFuel - traktor.currentFuel.Value;
                float eklenecek = Mathf.Min(miktar, bosYer);

                if (eklenecek > 0)
                {
                    traktor.AddFuelServerRpc(eklenecek);
                    bidonMevcutYakit.Value -= eklenecek;
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void DecreaseItemAmountServerRpc(int slotIndex, int count)
    {
        if (slots[slotIndex].IsEmpty) return;

        slots[slotIndex].amount -= count;
        if (slots[slotIndex].amount <= 0) slots[slotIndex].ClearSlot();

        DecreaseItemClientRpc(slotIndex, slots[slotIndex].amount);
    }

    [Rpc(SendTo.Everyone)]
    private void DecreaseItemClientRpc(int slotIndex, int newAmount)
    {
        if (newAmount <= 0)
        {
            slots[slotIndex].ClearSlot();
            UpdateHeldItemVisuals(slotIndex);
        }
        else
        {
            slots[slotIndex].amount = newAmount;
        }
        OnSlotChanged?.Invoke(slotIndex, slots[slotIndex]);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GiveSpecificItemServerRpc(int itemID, int amount, RpcParams rpcParams = default)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
        };

        AddLocalItemClientRpc(itemID, amount, clientRpcParams);
    }

    [Rpc(SendTo.Server)]
    public void DikmeIstegiServerRpc(Vector3 nokta, int slotIndex)
    {
        if (slots[slotIndex].IsEmpty || slots[slotIndex].itemData == null) return;

        ItemData data = slots[slotIndex].itemData;

        // DEĞİŞİKLİK: Artık groundPrefab değil, ekinPrefab arıyoruz
        if (data.ekinPrefab != null)
        {
            GameObject yeniFide = Instantiate(data.ekinPrefab, nokta + (Vector3.up * 0.05f), Quaternion.identity);
            NetworkObject netObj = yeniFide.GetComponent<NetworkObject>();
            netObj.Spawn();

            // Ekilen bitkiye tohumun ID'sini otomatik veriyoruz
            if (yeniFide.TryGetComponent(out ModularCrop crop))
            {
                crop.tohumID.Value = data.itemID;
            }

            DecreaseItemAmountServerRpc(slotIndex, 1);
        }
    }

    // --- YENİ EKLENEN KISIM: Ekin Prefabına İhtiyaç Duymayan Yerleştirme ---
    [Rpc(SendTo.Server)]
    public void FiskiyeYerlestirServerRpc(Vector3 nokta, int slotIndex)
    {
        if (slots[slotIndex].IsEmpty || slots[slotIndex].itemData == null) return;
        ItemData data = slots[slotIndex].itemData;

        // DEĞİŞİKLİK: ekinPrefab yerine doğrudan groundPrefab kullanıyoruz!
        if (data.groundPrefab != null)
        {
            GameObject yeniObje = Instantiate(data.groundPrefab, nokta, Quaternion.identity);
            yeniObje.GetComponent<NetworkObject>().Spawn();

            // SİHİRLİ DOKUNUŞ: Sol tıkla ekildiğinde fiziksel olarak yere düşmesini/yuvarlanmasını donduruyoruz.
            // Böylece G tuşuyla atılan fıskiyeden ayrılıyor ve dimdik ayakta kalıyor!
            if (yeniObje.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }

            DecreaseItemAmountServerRpc(slotIndex, 1);
        }
    }
    // -------------------------------------------------------------------

    [Rpc(SendTo.Server)]
    public void HasatEtServerRpc(ulong ekinNetID, Vector3 pos)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ekinNetID, out NetworkObject obj))
        {
            if (obj.TryGetComponent(out ModularCrop ekin))
            {
                // YENİ SİSTEM: İsme göre değil, direkt ID'ye göre çekiyoruz
                TohumVerisi v = TerrainLayerManager.Instance.GetTohumVerisi(ekin.tohumID.Value);

                if (v != null && v.dusecekTohumPrefab != null)
                {
                    int toplamUrun = v.hasatMiktari + ekin.extraYield.Value;
                    for (int i = 0; i < toplamUrun; i++)
                    {
                        // Eşyaları etrafa saçma efekti
                        Vector3 off = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1f, UnityEngine.Random.Range(-0.5f, 0.5f));
                        GameObject t = Instantiate(v.dusecekTohumPrefab, pos + off, Quaternion.identity);
                        t.GetComponent<NetworkObject>().Spawn();
                    }
                }

                bool wasWet = TerrainLayerManager.Instance.IsSoilWet(pos);

                // Güvenli silme işlemi
                if (obj.IsSceneObject == true)
                {
                    obj.Despawn(false);
                    obj.gameObject.SetActive(false);
                }
                else
                {
                    obj.Despawn(true);
                }

                // Toprak ıslaksa hasattan sonra tekrar ıslak görünüme çevir
                if (wasWet)
                {
                    TerrainLayerManager.Instance.PaintSoilServerRpc(pos, TerrainLayerManager.Instance.tilledLayerIndex, 3);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void GubreleServerRpc(ulong cropNetId, float growthMultiplier, int yBonus, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropNetId, out NetworkObject cropObj))
        {
            if (cropObj.TryGetComponent(out ModularCrop targetCrop))
            {
                if (!targetCrop.isFertilized.Value)
                {
                    targetCrop.ApplyFertilizer(growthMultiplier, yBonus);

                    // Gübreyi envanterden düş
                    DecreaseItemAmountServerRpc(slotIndex, 1);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RemoveTerrainDetailsServerRpc(Vector3 worldPos, float radius)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        TerrainData tData = terrain.terrainData;

        float prcEx = (worldPos.x - terrain.transform.position.x) / tData.size.x;
        float prcEz = (worldPos.z - terrain.transform.position.z) / tData.size.z;

        int posX = (int)(prcEx * tData.detailWidth);
        int posZ = (int)(prcEz * tData.detailHeight);
        int detRadius = Mathf.RoundToInt((radius / tData.size.x) * tData.detailWidth);

        int startX = Mathf.Clamp(posX - detRadius, 0, tData.detailWidth);
        int startZ = Mathf.Clamp(posZ - detRadius, 0, tData.detailHeight);
        int width = Mathf.Clamp(detRadius * 2, 0, tData.detailWidth - startX);
        int height = Mathf.Clamp(detRadius * 2, 0, tData.detailHeight - startZ);

        for (int i = 0; i < tData.detailPrototypes.Length; i++)
        {
            int[,] details = tData.GetDetailLayer(startX, startZ, width, height, i);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(detRadius, detRadius)) <= detRadius)
                    {
                        details[x, y] = 0;
                    }
                }
            }
            tData.SetDetailLayer(startX, startZ, i, details);
        }
    }
}