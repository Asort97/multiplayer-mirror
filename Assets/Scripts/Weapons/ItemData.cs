using UnityEngine;

public enum ItemType
{
    Melee,
    Ranged,
    Heal,
    Ammo
}

public enum AmmoType
{
    None,
    Ammo9mm,
    Ammo12Shells
}

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public Sprite itemSprite;

    [Header("Combat (Melee & Ranged)")]
    public int damage = 10;
    public float attackRate = 0.3f;
    public float meleeRange = 1f;

    [Header("Ranged Only")]
    public AmmoType ammoType = AmmoType.None;
    public float bulletSpeed = 15f;
    public float bulletRange = 10f;
    public int maxAmmo = 30;
    public Sprite bulletSprite;
    public int pelletCount = 1;
    public float spreadAngle = 30f;

    [Header("Heal Only")]
    public int healAmount = 25;
    public float useTime = 1f;

    [Header("Ammo Only")]
    public int ammoAmount = 15;
}
