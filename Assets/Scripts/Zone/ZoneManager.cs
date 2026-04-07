using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ZoneManager : NetworkBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [Header("Zone Phases")]
    [SerializeField] private ZonePhase[] phases;

    [Header("Damage")]
    [SerializeField] private float damageTickInterval = 1f;

    [Header("Visual")]
    [SerializeField] private LineRenderer zoneCircle;
    [SerializeField] private LineRenderer nextZoneCircle;
    [SerializeField] private int circleSegments = 256;
    [SerializeField] private int minimapLayer = 6;

    [SyncVar] private Vector2 currentCenter;
    [SyncVar] private float currentRadius;
    [SyncVar] private Vector2 targetCenter;
    [SyncVar] private float targetRadius;
    [SyncVar] private int currentPhase;
    [SyncVar] private float phaseTimer;
    [SyncVar] private bool isShrinking;
    [SyncVar] private bool isFinished;

    private Vector2 shrinkStartCenter;
    private float shrinkStartRadius;
    private float damageTimer;

    public Vector2 CurrentCenter => currentCenter;
    public float CurrentRadius => currentRadius;
    public Vector2 TargetCenter => targetCenter;
    public float TargetRadius => targetRadius;
    public int CurrentPhase => currentPhase;
    public float PhaseTimer => phaseTimer;
    public bool IsShrinking => isShrinking;
    public int TotalPhases => phases != null ? phases.Length : 0;

    public float CurrentPhaseDuration
    {
        get
        {
            if (phases == null || currentPhase >= phases.Length) return 0f;
            return isShrinking ? phases[currentPhase].shrinkDuration : phases[currentPhase].waitDuration;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        if (phases == null || phases.Length == 0) return;

        currentRadius = phases[0].startRadius;
        currentCenter = Vector2.zero;
        targetRadius = phases[0].endRadius;
        targetCenter = Random.insideUnitCircle * phases[0].centerOffset;
        currentPhase = 0;
        phaseTimer = phases[0].waitDuration;
        isShrinking = false;
    }

    public override void OnStartClient()
    {
        SetupCircle(zoneCircle, Color.cyan);
        SetupCircle(nextZoneCircle, new Color(1f, 1f, 1f, 0.3f));

        if (nextZoneCircle != null)
            nextZoneCircle.gameObject.layer = minimapLayer;
    }

    private void Update()
    {
        DrawCircle(zoneCircle, currentCenter, currentRadius);

        if (!isFinished && currentPhase < phases.Length)
            DrawCircle(nextZoneCircle, targetCenter, targetRadius);
        else if (nextZoneCircle != null)
            nextZoneCircle.enabled = false;

        if (!isServer) return;
        if (phases == null || phases.Length == 0) return;

        if (!isFinished)
        {
            phaseTimer -= Time.deltaTime;

            if (!isShrinking)
            {
                if (phaseTimer <= 0f)
                {
                    isShrinking = true;
                    phaseTimer = phases[currentPhase].shrinkDuration;
                    shrinkStartCenter = currentCenter;
                    shrinkStartRadius = currentRadius;
                }
            }
            else
            {
                float t = 1f - (phaseTimer / phases[currentPhase].shrinkDuration);
                t = Mathf.Clamp01(t);

                currentRadius = Mathf.Lerp(shrinkStartRadius, targetRadius, t);
                currentCenter = Vector2.Lerp(shrinkStartCenter, targetCenter, t);

                if (phaseTimer <= 0f)
                {
                    currentRadius = targetRadius;
                    currentCenter = targetCenter;
                    isShrinking = false;
                    currentPhase++;

                    if (currentPhase < phases.Length)
                    {
                        phaseTimer = phases[currentPhase].waitDuration;
                        targetRadius = phases[currentPhase].endRadius;
                        targetCenter = currentCenter + Random.insideUnitCircle * phases[currentPhase].centerOffset;
                    }
                    else
                    {
                        isFinished = true;
                    }
                }
            }
        }

        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            damageTimer = damageTickInterval;
            DamagePlayers();
        }
    }

    [Server]
    private void DamagePlayers()
    {
        int dmg = GetCurrentDamage();
        if (dmg <= 0) return;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;
            var health = conn.identity.GetComponent<PlayerHealth>();
            if (health == null || health.IsDead) continue;

            float dist = Vector2.Distance(conn.identity.transform.position, currentCenter);
            if (dist > currentRadius)
                health.TakeDamage(dmg);
        }
    }

    private int GetCurrentDamage()
    {
        if (phases == null || phases.Length == 0) return 0;
        int idx = Mathf.Min(currentPhase, phases.Length - 1);
        return phases[idx].damagePerTick;
    }

    private void SetupCircle(LineRenderer lr, Color color)
    {
        if (lr == null) return;
        lr.positionCount = circleSegments;
        lr.loop = true;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.allowOcclusionWhenDynamic = false;
    }

    private void DrawCircle(LineRenderer lr, Vector2 center, float radius)
    {
        if (lr == null) return;
        lr.enabled = true;
        lr.positionCount = circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}

[System.Serializable]
public class ZonePhase
{
    public float startRadius = 50f;
    public float endRadius = 30f;
    public float centerOffset = 5f;
    public float waitDuration = 60f;
    public float shrinkDuration = 30f;
    public int damagePerTick = 5;
}
