using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
    private PlayerHUD playerHud;
    private PlayerSpriteAnimator spriteAnimator;
    private float nextAttackTime;
    private bool isHealing;
    private float healStartTime;
    private float currentHealDuration;
    private string healingItemName;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        playerHud = GetComponent<PlayerHUD>();
        spriteAnimator = GetComponent<PlayerSpriteAnimator>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        bool pointerOverBlockingUi = IsPointerOverBlockingUi();

        if (isHealing)
        {
            if (pointerOverBlockingUi)
            {
                CancelHealing();
                return;
            }

            UpdateHealing();
            return;
        }

        var item = inventory.GetActiveItemData();

        if (item != null && item.itemType == ItemType.Heal)
        {
            if (Input.GetMouseButtonDown(0) && !pointerOverBlockingUi)
                StartHealing(item);
            return;
        }

        if (pointerOverBlockingUi)
            return;

        if (Input.GetMouseButton(0) && Time.time >= nextAttackTime)
        {
            if (item == null)
            {
                nextAttackTime = Time.time + fistCooldown;
                PlayLocalAttackAnimation(PlayerAttackAnimationType.Fist);
                GameAudioManager.PlayNamed("default_attack");
                CmdFistAttack();
            }
            else if (item.itemType == ItemType.Melee)
            {
                nextAttackTime = Time.time + item.attackRate;
                PlayLocalAttackAnimation(PlayerAttackAnimationType.Knife);
                GameAudioManager.PlayNamed("knife");
                CmdMeleeAttack(item.damage, item.meleeRange);
            }
            else if (item.itemType == ItemType.Ranged)
            {
                if (inventory.GetActiveAmmo() > 0)
                {
                    nextAttackTime = Time.time + item.attackRate;
                    PlayLocalAttackAnimation(item.itemName == "Shotgun" ? PlayerAttackAnimationType.Shotgun : PlayerAttackAnimationType.Pistol);
                    GameAudioManager.PlayNamed(item.itemName == "Shotgun" ? "shotgun" : "pistol");
                    CmdShoot();
                }
                else
                {
                    nextAttackTime = Time.time + fistCooldown;
                    PlayLocalAttackAnimation(PlayerAttackAnimationType.Fist);
                    GameAudioManager.PlayNamed("default_attack");
                    CmdFistAttack();
                }
            }
        }
    }

    private void StartHealing(ItemData item)
    {
        if (item == null || item.itemType != ItemType.Heal)
            return;

        isHealing = true;
        healStartTime = Time.time;
        currentHealDuration = Mathf.Max(0.1f, item.useTime);
        healingItemName = item.itemName;
        if (playerHud != null)
            playerHud.SetHealProgress(0f, true);
    }

    private void UpdateHealing()
    {
        var item = inventory.GetActiveItemData();
        if (item == null || item.itemType != ItemType.Heal || item.itemName != healingItemName || !Input.GetMouseButton(0))
        {
            CancelHealing();
            return;
        }

        float progress = (Time.time - healStartTime) / currentHealDuration;
        if (playerHud != null)
            playerHud.SetHealProgress(progress, true);

        if (progress < 1f)
            return;

        isHealing = false;
        if (playerHud != null)
            playerHud.SetHealProgress(0f, false);
        nextAttackTime = Time.time + 0.1f;
        CmdUseHeal();
    }

    private void CancelHealing()
    {
        isHealing = false;
        healingItemName = null;
        if (playerHud != null)
            playerHud.SetHealProgress(0f, false);
    }

    private bool IsPointerOverBlockingUi()
    {
        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == null)
                continue;

            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    private void PlayLocalAttackAnimation(PlayerAttackAnimationType attackType)
    {
        if (spriteAnimator != null)
            spriteAnimator.PlayAttack(attackType);
    }

    [ClientRpc]
    private void RpcPlayAttackAnimation(int attackType)
    {
        if (isLocalPlayer)
            return;

        if (spriteAnimator != null)
            spriteAnimator.PlayAttack((PlayerAttackAnimationType)attackType);
    }

    [Command]
    private void CmdFistAttack()
    {
        RpcPlayAttackAnimation((int)PlayerAttackAnimationType.Fist);
        DoMeleeHit(fistDamage, fistRange);
    }

    [Command]
    private void CmdMeleeAttack(int damage, float range)
    {
        RpcPlayAttackAnimation((int)PlayerAttackAnimationType.Knife);
        DoMeleeHit(damage, range);
    }

    private void DoMeleeHit(int damage, float range)
    {
        var myIdentity = GetComponent<NetworkIdentity>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            var health = hit.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damage, myIdentity);
        }
    }

    [Command]
    private void CmdShoot()
    {
        var item = inventory.GetActiveItemData();
        if (item == null || item.itemType != ItemType.Ranged) return;
        if (inventory.GetActiveAmmo() <= 0) return;

        RpcPlayAttackAnimation((int)(item.itemName == "Shotgun" ? PlayerAttackAnimationType.Shotgun : PlayerAttackAnimationType.Pistol));

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
