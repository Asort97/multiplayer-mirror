using UnityEngine;

public class LobbyParallax : MonoBehaviour
{
    [SerializeField] private RectTransform backgroundLayer;
    [SerializeField] private RectTransform farTreesLayer;
    [SerializeField] private RectTransform midTreesLayer;
    [SerializeField] private RectTransform closeTreesLayer;

    [SerializeField] private float backgroundOffset = 6f;
    [SerializeField] private float farTreesOffset = 14f;
    [SerializeField] private float midTreesOffset = 26f;
    [SerializeField] private float closeTreesOffset = 42f;
    [SerializeField] private float verticalFactor = 0.2f;
    [SerializeField] private float maxDownwardPointer = 0.35f;
    [SerializeField] private float maxUpwardPointer = 1f;
    [SerializeField] private float smoothing = 6f;
    [SerializeField] private float idleAmplitude = 5f;
    [SerializeField] private float idleSpeed = 0.35f;

    private Vector2 backgroundBase;
    private Vector2 farBase;
    private Vector2 midBase;
    private Vector2 closeBase;

    private void Awake()
    {
        if (backgroundLayer != null) backgroundBase = backgroundLayer.anchoredPosition;
        if (farTreesLayer != null) farBase = farTreesLayer.anchoredPosition;
        if (midTreesLayer != null) midBase = midTreesLayer.anchoredPosition;
        if (closeTreesLayer != null) closeBase = closeTreesLayer.anchoredPosition;
    }

    private void Update()
    {
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        Vector2 pointer = new Vector2(
            (Input.mousePosition.x / width - 0.5f) * 2f,
            (Input.mousePosition.y / height - 0.5f) * 2f);

        float drift = Mathf.Sin(Time.unscaledTime * idleSpeed) * idleAmplitude;

        UpdateLayer(backgroundLayer, backgroundBase, pointer, backgroundOffset, drift * 0.15f);
        UpdateLayer(farTreesLayer, farBase, pointer, farTreesOffset, drift * 0.35f);
        UpdateLayer(midTreesLayer, midBase, pointer, midTreesOffset, drift * 0.65f);
        UpdateLayer(closeTreesLayer, closeBase, pointer, closeTreesOffset, drift);
    }

    private void UpdateLayer(RectTransform layer, Vector2 basePosition, Vector2 pointer, float strength, float drift)
    {
        if (layer == null)
            return;

        float clampedPointerY = Mathf.Clamp(pointer.y, -maxDownwardPointer, maxUpwardPointer);
        Vector2 target = basePosition + new Vector2(pointer.x * strength + drift, clampedPointerY * strength * verticalFactor);
        float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        layer.anchoredPosition = Vector2.Lerp(layer.anchoredPosition, target, t);
    }
}
