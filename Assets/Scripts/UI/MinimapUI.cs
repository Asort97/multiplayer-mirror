using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MinimapUI : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private GameObject fullscreenPanel;
    [SerializeField] private RawImage fullscreenMapImage;

    [Header("Map")]
    [SerializeField] private SpriteRenderer mapRenderer;

    [Header("Settings")]
    [SerializeField] private float minimapZoom = 40f;
    [SerializeField] private float fullscreenZoom = 80f;
    [SerializeField] private int minimapTextureSize = 512;
    [SerializeField] private int fullscreenTextureSize = 2048;
    [SerializeField] private int minimapLayer = 6;
    [SerializeField] private Color markerColor = Color.yellow;
    [SerializeField] private float markerSize = 2f;

    private Camera minimapCam;
    private RenderTexture rtMini;
    private RenderTexture rtFull;
    private bool fullscreen;

    public override void OnStartLocalPlayer()
    {
        rtMini = new RenderTexture(minimapTextureSize, minimapTextureSize, 16);
        rtFull = new RenderTexture(fullscreenTextureSize, fullscreenTextureSize, 16);

        var camGO = new GameObject("MinimapCamera");
        minimapCam = camGO.AddComponent<Camera>();
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = minimapZoom;
        minimapCam.targetTexture = rtMini;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.1f, 0.14f, 0.1f, 1f);
        minimapCam.cullingMask = Camera.main.cullingMask | (1 << minimapLayer);
        minimapCam.depth = Camera.main.depth - 1;

        var mainCamData = Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (mainCamData != null)
        {
            var mmCamData = minimapCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            mmCamData.renderType = mainCamData.renderType;
        }

        Camera.main.cullingMask &= ~(1 << minimapLayer);

        if (minimapImage != null)
        {
            minimapImage.texture = rtMini;
            minimapImage.gameObject.SetActive(true);
        }
        if (fullscreenMapImage != null)
        {
            fullscreenMapImage.texture = rtFull;
            var fitter = fullscreenMapImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }
        if (fullscreenPanel != null)
            fullscreenPanel.SetActive(false);

        if (mapRenderer == null)
            mapRenderer = ResolveMapRenderer();

        CreatePlayerMarker();
    }

    private void CreatePlayerMarker()
    {
        var marker = new GameObject("MinimapMarker");
        marker.transform.SetParent(transform);
        marker.transform.localPosition = Vector3.zero;
        marker.layer = minimapLayer;

        var sr = marker.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        sr.color = markerColor;
        sr.sortingOrder = 200;
        marker.transform.localScale = Vector3.one * markerSize;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (minimapCam != null)
        {
            minimapCam.orthographicSize = fullscreen ? fullscreenZoom : minimapZoom;
            minimapCam.targetTexture = fullscreen ? rtFull : rtMini;

            Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, -10f);
            minimapCam.transform.position = ClampCameraPosition(targetPosition, minimapCam.orthographicSize);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            fullscreen = !fullscreen;
            if (fullscreenPanel != null)
                fullscreenPanel.SetActive(fullscreen);
            if (minimapImage != null)
                minimapImage.gameObject.transform.parent.gameObject.SetActive(!fullscreen);
        }
    }

    private void OnDestroy()
    {
        if (minimapCam != null) Destroy(minimapCam.gameObject);
        if (rtMini != null) rtMini.Release();
        if (rtFull != null) rtFull.Release();
    }

    private SpriteRenderer ResolveMapRenderer()
    {
        SpriteRenderer bestRenderer = null;
        float bestArea = 0f;

        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var spriteRenderer in renderers)
        {
            if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null)
                continue;

            if (spriteRenderer.gameObject.layer == minimapLayer)
                continue;

            var bounds = spriteRenderer.bounds;
            float area = bounds.size.x * bounds.size.y;
            if (area > bestArea)
            {
                bestArea = area;
                bestRenderer = spriteRenderer;
            }
        }

        return bestRenderer;
    }

    private Vector3 ClampCameraPosition(Vector3 targetPosition, float orthographicSize)
    {
        if (mapRenderer == null)
            return targetPosition;

        Bounds bounds = mapRenderer.bounds;
        float aspect = 1f;
        if (minimapCam != null && minimapCam.targetTexture != null && minimapCam.targetTexture.height > 0)
            aspect = (float)minimapCam.targetTexture.width / minimapCam.targetTexture.height;

        float halfHeight = orthographicSize;
        float halfWidth = orthographicSize * aspect;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        float clampedX = minX <= maxX ? Mathf.Clamp(targetPosition.x, minX, maxX) : bounds.center.x;
        float clampedY = minY <= maxY ? Mathf.Clamp(targetPosition.y, minY, maxY) : bounds.center.y;

        return new Vector3(clampedX, clampedY, targetPosition.z);
    }
}
