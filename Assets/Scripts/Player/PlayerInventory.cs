using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    public const int SlotCount = 5;

    [Header("References")]
    [SerializeField] private GameObject droppedItemPrefab;

    public readonly SyncList<string> slotItemNames = new SyncList<string>();
    public readonly SyncList<int> slotAmmo = new SyncList<int>();

    [SyncVar] private int activeSlot;

    [SyncVar] public int ammo9mm;
    [SyncVar] public int ammo12Shells;

    public int ActiveSlot => activeSlot;

    public override void OnStartServer()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            slotItemNames.Add("");
            slotAmmo.Add(0);
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                CmdSwitchSlot(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            CmdDropItem();
        }
    }

    [Command]
    private void CmdSwitchSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        activeSlot = slot;
    }

    [Command]
    private void CmdDropItem()
    {
        DropCurrentItem();
    }

    [Server]
    public void DropCurrentItem()
    {
        if (string.IsNullOrEmpty(slotItemNames[activeSlot])) return;

        string itemName = slotItemNames[activeSlot];
        int amount = slotAmmo[activeSlot];

        if (droppedItemPrefab != null)
        {
            Vector3 dropPos = transform.position + transform.right * 1f;
            GameObject dropped = Instantiate(droppedItemPrefab, dropPos, Quaternion.identity);
            var pickup = dropped.GetComponent<ItemPickup>();
            pickup.SetItem(itemName, amount);
            NetworkServer.Spawn(dropped);
        }

        slotItemNames[activeSlot] = "";
        slotAmmo[activeSlot] = 0;
    }

    [Server]
    public bool TryAddItem(string itemName, int ammo)
    {
        var data = LoadItem(itemName);
        if (data == null) return false;

        if (data.itemType == ItemType.Ammo)
        {
            AddAmmo(data.ammoType, ammo > 0 ? ammo : data.ammoAmount);
            return true;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slotItemNames[i] == itemName)
                return false;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (string.IsNullOrEmpty(slotItemNames[i]))
            {
                slotItemNames[i] = itemName;
                slotAmmo[i] = 0;

                if (data.itemType == ItemType.Ranged && data.ammoType != AmmoType.None)
                {
                    MoveAmmoSlotsToPool(data.ammoType);
                }

                return true;
            }
        }

        return false;
    }

    [Server]
    public void AddAmmo(AmmoType type, int amount)
    {
        if (HasWeaponForAmmoType(type))
        {
            AddToPool(type, amount);
        }
        else
        {
            string ammoItemName = GetAmmoItemName(type);
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItemNames[i] == ammoItemName)
                {
                    slotAmmo[i] += amount;
                    return;
                }
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (string.IsNullOrEmpty(slotItemNames[i]))
                {
                    slotItemNames[i] = ammoItemName;
                    slotAmmo[i] = amount;
                    return;
                }
            }
        }
    }

    [Server]
    private void AddToPool(AmmoType type, int amount)
    {
        switch (type)
        {
            case AmmoType.Ammo9mm: ammo9mm += amount; break;
            case AmmoType.Ammo12Shells: ammo12Shells += amount; break;
        }
    }

    [Server]
    private void MoveAmmoSlotsToPool(AmmoType type)
    {
        string ammoItemName = GetAmmoItemName(type);
        for (int i = 0; i < SlotCount; i++)
        {
            if (slotItemNames[i] == ammoItemName)
            {
                AddToPool(type, slotAmmo[i]);
                slotItemNames[i] = "";
                slotAmmo[i] = 0;
            }
        }
    }

    public bool HasWeaponForAmmoType(AmmoType type)
    {
        for (int i = 0; i < slotItemNames.Count; i++)
        {
            var data = LoadItem(slotItemNames[i]);
            if (data != null && data.itemType == ItemType.Ranged && data.ammoType == type)
                return true;
        }
        return false;
    }

    private static string GetAmmoItemName(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Ammo9mm: return "Ammo9mm";
            case AmmoType.Ammo12Shells: return "Ammo12Shells";
            default: return "";
        }
    }

    [Server]
    public void ConsumeAmmo()
    {
        var item = GetActiveItemData();
        if (item == null) return;
        switch (item.ammoType)
        {
            case AmmoType.Ammo9mm:
                if (ammo9mm > 0) ammo9mm--;
                break;
            case AmmoType.Ammo12Shells:
                if (ammo12Shells > 0) ammo12Shells--;
                break;
        }
    }

    [Server]
    public void ConsumeCurrentItem()
    {
        slotItemNames[activeSlot] = "";
        slotAmmo[activeSlot] = 0;
    }

    public ItemData GetActiveItemData()
    {
        if (activeSlot < 0 || activeSlot >= slotItemNames.Count) return null;
        string name = slotItemNames[activeSlot];
        if (string.IsNullOrEmpty(name)) return null;
        return LoadItem(name);
    }

    public int GetActiveAmmo()
    {
        var item = GetActiveItemData();
        if (item == null) return 0;
        return GetAmmo(item.ammoType);
    }

    public int GetAmmo(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Ammo9mm: return ammo9mm;
            case AmmoType.Ammo12Shells: return ammo12Shells;
            default: return 0;
        }
    }

    public ItemData GetSlotItemData(int slot)
    {
        if (slot < 0 || slot >= slotItemNames.Count) return null;
        string name = slotItemNames[slot];
        if (string.IsNullOrEmpty(name)) return null;
        return LoadItem(name);
    }

    public static ItemData LoadItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        return Resources.Load<ItemData>("Items/" + itemName);
    }
}
