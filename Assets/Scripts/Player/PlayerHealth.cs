using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
    }

    [Server]
    public void TakeDamage(int amount, NetworkIdentity attacker = null)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (IsDead)
        {
            GetComponent<PlayerInventory>().DropAllItems();
            if (MatchManager.Instance != null)
                MatchManager.Instance.OnPlayerKilled(GetComponent<NetworkIdentity>(), attacker);
            RpcOnDeath();
        }
    }

    [Server]
    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        if (isLocalPlayer)
        {
            GetComponent<PlayerMovement>().enabled = false;
            GetComponent<PlayerCombat>().enabled = false;
        }
    }
}
