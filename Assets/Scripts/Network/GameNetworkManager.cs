using UnityEngine;
using Mirror;

public struct ServerRejectionMessage : NetworkMessage
{
    public string reason;
}

public class GameNetworkManager : NetworkManager
{
    public const string ReasonMatchInProgress = "На этом сервере уже идёт матч. Дождитесь окончания, чтобы сыграть снова.";
    public const string ReasonServerFull = "Сервер заполнен.";

    public static string LastDisconnectReason;

    [Header("Game")]
    [SerializeField] private Transform[] spawnPoints;

    private int spawnIndex;

    public override void OnStartServer()
    {
        base.OnStartServer();
        maxConnections = 20;

        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.Init();

        NetworkServer.RegisterHandler<ServerRejectionMessage>((conn, msg) => { }, false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<ServerRejectionMessage>(OnServerRejection, false);
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        if (MatchManager.Instance != null && !MatchManager.Instance.IsJoinAllowed)
        {
            conn.Send(new ServerRejectionMessage { reason = ReasonMatchInProgress });
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (MatchManager.Instance != null && !MatchManager.Instance.IsJoinAllowed)
        {
            conn.Send(new ServerRejectionMessage { reason = ReasonMatchInProgress });
            conn.Disconnect();
            return;
        }

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

    private static void OnServerRejection(ServerRejectionMessage msg)
    {
        LastDisconnectReason = msg.reason;
    }
}
