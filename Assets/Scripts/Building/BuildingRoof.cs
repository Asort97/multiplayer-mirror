using UnityEngine;

public class BuildingRoof : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private SpriteRenderer roofRenderer;

    private bool playerInside;
    private SpriteRenderer[] roofRenderers;
    private float[] baseAlphas;

    private void Awake()
    {
        if (roofRenderer != null)
            roofRenderers = roofRenderer.GetComponentsInChildren<SpriteRenderer>(true);
        else
            roofRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if ((roofRenderers == null || roofRenderers.Length == 0) && roofRenderer != null)
            roofRenderers = new[] { roofRenderer };

        if (roofRenderers == null)
            roofRenderers = new SpriteRenderer[0];

        baseAlphas = new float[roofRenderers.Length];
        for (int i = 0; i < roofRenderers.Length; i++)
        {
            if (roofRenderers[i] != null)
                baseAlphas[i] = roofRenderers[i].color.a;
        }
    }

    private void Update()
    {
        if (roofRenderers == null || roofRenderers.Length == 0) return;

        for (int i = 0; i < roofRenderers.Length; i++)
        {
            var currentRenderer = roofRenderers[i];
            if (currentRenderer == null) continue;

            float targetAlpha = playerInside ? 0f : baseAlphas[i];
            var color = currentRenderer.color;
            color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
            currentRenderer.color = color;
        }
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
