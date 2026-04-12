using UnityEngine;
using Mirror;
using System.Collections.Generic;

[System.Serializable]
public class SpawnableItem
{
    public GameObject prefab;
    [Range(1, 100)]
    public int weight = 10;
}

public class ItemSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private SpawnableItem[] items;
    [SerializeField] private int spawnCount = 80;
    [SerializeField] private int buildingSpawnCount = 5;
    [SerializeField] private float minDistanceBetweenItems = 3f;

    [Header("Map Reference")]
    [SerializeField] private SpriteRenderer mapRenderer;

    private List<Vector2> usedPositions = new List<Vector2>();

    public override void OnStartServer()
    {
        if (items == null || items.Length == 0) return;

        SpawnOnMap();
        SpawnInBuildings();
    }

    [Server]
    private void SpawnOnMap()
    {
        if (mapRenderer == null) return;
        var bounds = mapRenderer.bounds;

        int totalWeight = 0;
        foreach (var item in items)
            totalWeight += item.weight;

        int spawned = 0;
        int attempts = 0;
        while (spawned < spawnCount && attempts < spawnCount * 10)
        {
            attempts++;
            float x = Random.Range(bounds.min.x + 2f, bounds.max.x - 2f);
            float y = Random.Range(bounds.min.y + 2f, bounds.max.y - 2f);
            var pos = new Vector2(x, y);

            if (!IsValidPosition(pos)) continue;

            var prefab = PickRandom(totalWeight);
            if (prefab == null) continue;

            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            NetworkServer.Spawn(go);
            usedPositions.Add(pos);
            spawned++;
        }
    }

    [Server]
    private void SpawnInBuildings()
    {
        var buildings = GameObject.FindGameObjectsWithTag("Untagged");
        var buildingRoots = new List<Transform>();

        foreach (var go in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (go.name.StartsWith("Building") && go.Find("trigger") != null)
                buildingRoots.Add(go);
        }

        int totalWeight = 0;
        foreach (var item in items)
            totalWeight += item.weight;

        foreach (var building in buildingRoots)
        {
            var trigger = building.Find("trigger");
            if (trigger == null) continue;

            var col = trigger.GetComponent<BoxCollider2D>();
            if (col == null) continue;

            var center = (Vector2)trigger.TransformPoint(col.offset);
            var halfSize = col.size * 0.4f;

            for (int i = 0; i < buildingSpawnCount; i++)
            {
                int attempts = 0;
                while (attempts < 20)
                {
                    attempts++;
                    float x = center.x + Random.Range(-halfSize.x, halfSize.x);
                    float y = center.y + Random.Range(-halfSize.y, halfSize.y);
                    var pos = new Vector2(x, y);

                    if (!IsValidPosition(pos)) continue;

                    var prefab = PickRandom(totalWeight);
                    if (prefab == null) continue;

                    var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
                    NetworkServer.Spawn(go);
                    usedPositions.Add(pos);
                    break;
                }
            }
        }
    }

    private bool IsValidPosition(Vector2 pos)
    {
        foreach (var used in usedPositions)
        {
            if (Vector2.Distance(pos, used) < minDistanceBetweenItems)
                return false;
        }

        var hit = Physics2D.OverlapPoint(pos);
        if (hit != null && !hit.isTrigger)
            return false;

        return true;
    }

    private GameObject PickRandom(int totalWeight)
    {
        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var item in items)
        {
            cumulative += item.weight;
            if (roll < cumulative)
                return item.prefab;
        }
        return items[items.Length - 1].prefab;
    }
}
