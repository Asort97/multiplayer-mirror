using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    private const string LogPrefix = "[PlayerHealth]";

    [SerializeField] private int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    [SyncVar]
    public string playerName = "";

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    public override void OnStartServer()
    {
        Debug.Log($"{LogPrefix} OnStartServer netId={netId} name='{gameObject.name}'");
        currentHealth = maxHealth;
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"{LogPrefix} OnStartLocalPlayer netId={netId} name='{gameObject.name}' nick='{LobbyUI.LocalNickname}'");
        CmdSetName(LobbyUI.LocalNickname);
    }

    [Command]
    private void CmdSetName(string name)
    {
        playerName = name;
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
        GameAudioManager.PlayNamed("death");

        var spriteAnimator = GetComponent<PlayerSpriteAnimator>();
        if (spriteAnimator != null)
            spriteAnimator.PlayDeath();
        else
            GetComponent<SpriteRenderer>().enabled = false;

        var collider2d = GetComponent<Collider2D>();
        if (collider2d != null)
            collider2d.enabled = false;

        var rigidbody2d = GetComponent<Rigidbody2D>();
        if (rigidbody2d != null)
            rigidbody2d.linearVelocity = Vector2.zero;

        if (isLocalPlayer)
        {
            GetComponent<PlayerMovement>().enabled = false;
            GetComponent<PlayerCombat>().enabled = false;
        }
    }
}
