using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    [HideInInspector] public int damage;
    [SyncVar] public float speed;
    [SyncVar] public float range;
    [HideInInspector] public GameObject owner;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        if (!isServer) return;

        if (Vector3.Distance(startPosition, transform.position) >= range)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return;
        if (other.GetComponent<Bullet>() != null) return;
        if (other.GetComponent<ItemPickup>() != null) return;
        if (other.GetComponent<AmmoPickup>() != null) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            var attackerIdentity = owner != null ? owner.GetComponent<NetworkIdentity>() : null;
            health.TakeDamage(damage, attackerIdentity);
        }

        NetworkServer.Destroy(gameObject);
    }
}
