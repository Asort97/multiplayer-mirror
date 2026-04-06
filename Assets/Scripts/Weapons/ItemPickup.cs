using UnityEngine;
using Mirror;

public class ItemPickup : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;

    [SyncVar(hook = nameof(OnItemChanged))]
    private string itemName;

    [SyncVar]
    private int ammo;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnStartClient()
    {
        UpdateVisual();
    }

    [Server]
    public void SetItem(string name, int ammoCount)
    {
        itemName = name;
        ammo = ammoCount;

        var data = PlayerInventory.LoadItem(name);
        if (data != null && spriteRenderer != null)
            spriteRenderer.sprite = data.itemSprite;
    }

    public override void OnStartServer()
    {
        if (itemData != null)
        {
            itemName = itemData.itemName;
            ammo = itemData.itemType == ItemType.Ranged ? itemData.maxAmmo : 1;
        }
    }

    private void OnItemChanged(string oldName, string newName)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;
        var data = PlayerInventory.LoadItem(itemName);
        if (data != null && data.itemSprite != null)
            spriteRenderer.sprite = data.itemSprite;
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<PlayerInventory>();
        if (inv == null) return;

        if (inv.TryAddItem(itemName, ammo))
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
