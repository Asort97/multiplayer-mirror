using UnityEngine;
using Mirror;

public class PlayerCombat : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Fist (no weapon)")]
    [SerializeField] private int fistDamage = 5;
    [SerializeField] private float fistRange = 0.8f;
    [SerializeField] private float fistCooldown = 0.5f;

    private PlayerInventory inventory;
    private float nextAttackTime;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetMouseButton(0) && Time.time >= nextAttackTime)
        {
            var item = inventory.GetActiveItemData();

            if (item == null)
            {
                nextAttackTime = Time.time + fistCooldown;
                CmdFistAttack();
            }
            else if (item.itemType == ItemType.Melee)
            {
                nextAttackTime = Time.time + item.attackRate;
                CmdMeleeAttack(item.damage, item.meleeRange);
            }
            else if (item.itemType == ItemType.Ranged)
            {
                if (inventory.GetActiveAmmo() > 0)
                {
                    nextAttackTime = Time.time + item.attackRate;
                    CmdShoot();
                }
                else
                {
                    nextAttackTime = Time.time + fistCooldown;
                    CmdFistAttack();
                }
            }
            else if (item.itemType == ItemType.Heal)
            {
                nextAttackTime = Time.time + item.useTime;
                CmdUseHeal();
            }
        }
    }

    [Command]
    private void CmdFistAttack()
    {
        DoMeleeHit(fistDamage, fistRange);
    }

    [Command]
    private void CmdMeleeAttack(int damage, float range)
    {
        DoMeleeHit(damage, range);
    }

    private void DoMeleeHit(int damage, float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            var health = hit.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damage);
        }
    }

    [Command]
    private void CmdShoot()
    {
        var item = inventory.GetActiveItemData();
        if (item == null || item.itemType != ItemType.Ranged) return;
        if (inventory.GetActiveAmmo() <= 0) return;

        inventory.ConsumeAmmo();

        int pellets = Mathf.Max(1, item.pelletCount);
        float halfSpread = item.spreadAngle / 2f;

        for (int i = 0; i < pellets; i++)
        {
            float offset = (pellets == 1) ? 0f : Random.Range(-halfSpread, halfSpread);
            Quaternion rot = firePoint.rotation * Quaternion.Euler(0f, 0f, offset);

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rot);
            var bulletComp = bullet.GetComponent<Bullet>();
            bulletComp.damage = item.damage;
            bulletComp.speed = item.bulletSpeed;
            bulletComp.range = item.bulletRange;
            bulletComp.owner = gameObject;

            NetworkServer.Spawn(bullet);
        }
    }

    [Command]
    private void CmdUseHeal()
    {
        var item = inventory.GetActiveItemData();
        if (item == null || item.itemType != ItemType.Heal) return;

        var health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(item.healAmount);
        }

        inventory.ConsumeCurrentItem();
    }
}
