using UnityEngine;

public class BuildingRoof : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private SpriteRenderer roofRenderer;

    private bool playerInside;
    private float targetAlpha = 1f;

    private void Update()
    {
        if (roofRenderer == null) return;

        targetAlpha = playerInside ? 0f : 1f;
        var c = roofRenderer.color;
        c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
        roofRenderer.color = c;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var nb = other.GetComponent<Mirror.NetworkBehaviour>();
        if (nb != null && nb.isLocalPlayer)
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var nb = other.GetComponent<Mirror.NetworkBehaviour>();
        if (nb != null && nb.isLocalPlayer)
            playerInside = false;
    }
}
