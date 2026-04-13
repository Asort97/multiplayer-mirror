using UnityEngine;
using Mirror;

public class GameNetworkManager : NetworkManager
{
    [Header("Game")]
    [SerializeField] private Transform[] spawnPoints;

    private int spawnIndex;

    public override void OnStartServer()
    {
        base.OnStartServer();
        maxConnections = 20;

        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.Init();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Vector3 spawnPos = Vector3.zero;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            spawnPos = spawnPoints[spawnIndex % spawnPoints.Length].position;
            spawnIndex++;
        }
        else
        {
            spawnPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0f);
        }

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);

        if (MatchManager.Instance != null)
            MatchManager.Instance.RegisterPlayer(player.GetComponent<NetworkIdentity>());
    }
}
