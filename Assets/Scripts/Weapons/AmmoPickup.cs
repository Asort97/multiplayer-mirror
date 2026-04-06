using UnityEngine;
using Mirror;

public class AmmoPickup : NetworkBehaviour
{
    [SerializeField] private AmmoType ammoType = AmmoType.Ammo9mm;
    [SerializeField] private int amount = 10;

    [SyncVar(hook = nameof(OnAmmoTypeChanged))]
    private int syncedAmmoType;

    private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite sprite9mm;
    [SerializeField] private Sprite sprite12Shells;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnStartServer()
    {
        syncedAmmoType = (int)ammoType;
    }

    public override void OnStartClient()
    {
        UpdateVisual();
    }

    private void OnAmmoTypeChanged(int oldVal, int newVal)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;
        AmmoType t = (AmmoType)syncedAmmoType;
        if (t == AmmoType.Ammo9mm && sprite9mm != null)
            spriteRenderer.sprite = sprite9mm;
        else if (t == AmmoType.Ammo12Shells && sprite12Shells != null)
            spriteRenderer.sprite = sprite12Shells;
    }

    [Server]
    public void SetAmmo(AmmoType type, int count)
    {
        ammoType = type;
        syncedAmmoType = (int)type;
        amount = count;
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        inventory.AddAmmo(ammoType, amount);
        NetworkServer.Destroy(gameObject);
    }
}
