using UnityEngine;
using Mirror;
using System.Collections;

public struct ServerRejectionMessage : NetworkMessage
{
    public string reason;
}

public class GameNetworkManager : NetworkManager
{
    private const string LogPrefix = "[GameNetworkManager]";

    public const string ReasonMatchInProgress = "На этом сервере уже идёт матч. Дождитесь окончания, чтобы сыграть снова.";
    public const string ReasonServerFull = "Сервер заполнен.";

    public static string LastDisconnectReason;

    [Header("Game")]
    [SerializeField] private Transform[] spawnPoints;

    private int spawnIndex;
    private Coroutine ensureLocalPlayerRoutine;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"{LogPrefix} OnStartServer mode={mode} playerPrefab={(playerPrefab != null ? playerPrefab.name : "null")} autoCreatePlayer={autoCreatePlayer}");
        maxConnections = 20;

        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.Init();

        NetworkServer.RegisterHandler<ServerRejectionMessage>((conn, msg) => { }, false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"{LogPrefix} OnStartClient mode={mode} localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")}");
        NetworkClient.RegisterHandler<ServerRejectionMessage>(OnServerRejection, false);
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log($"{LogPrefix} OnStartHost mode={mode} localConnection={(NetworkServer.localConnection != null)}");
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        Debug.Log($"{LogPrefix} OnServerSceneChanged scene='{sceneName}' localConnection={(NetworkServer.localConnection != null)} localIdentity={(NetworkServer.localConnection != null && NetworkServer.localConnection.identity != null ? NetworkServer.localConnection.identity.netId.ToString() : "null")}");

        if (mode == NetworkManagerMode.Host && NetworkServer.localConnection != null && NetworkServer.localConnection.identity == null)
        {
            if (NetworkClient.connection != null && !NetworkClient.ready)
            {
                Debug.LogWarning($"{LogPrefix} Host local client was not ready during scene change. Calling Ready() before fallback spawn.");
                NetworkClient.Ready();
            }

            Debug.LogWarning($"{LogPrefix} Host local connection has no player after scene change. Spawning fallback player.");
            OnServerAddPlayer(NetworkServer.localConnection);
        }
    }

    public override void OnClientConnect()
    {
        if (ShouldSkipHostAutoAddPlayer())
        {
            Debug.Log($"{LogPrefix} OnClientConnect skipping base auto AddPlayer because host player already exists on server.");
        }
        else
        {
            base.OnClientConnect();
        }

        Debug.Log($"{LogPrefix} OnClientConnect ready={NetworkClient.ready} localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")} connIdentity={(NetworkClient.connection != null && NetworkClient.connection.identity != null ? NetworkClient.connection.identity.netId.ToString() : "null")}");
        EnsureLocalPlayerExists();
    }

    public override void OnClientSceneChanged()
    {
        if (ShouldSkipHostAutoAddPlayer())
        {
            if (NetworkClient.connection != null && NetworkClient.connection.isAuthenticated && !NetworkClient.ready)
                NetworkClient.Ready();

            Debug.Log($"{LogPrefix} OnClientSceneChanged skipping base auto AddPlayer because host player already exists on server.");
        }
        else
        {
            base.OnClientSceneChanged();
        }

        Debug.Log($"{LogPrefix} OnClientSceneChanged ready={NetworkClient.ready} localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")} connIdentity={(NetworkClient.connection != null && NetworkClient.connection.identity != null ? NetworkClient.connection.identity.netId.ToString() : "null")}");
        EnsureLocalPlayerExists();
    }

    public override void OnStopClient()
    {
        Debug.Log($"{LogPrefix} OnStopClient mode={mode} localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")}");
        if (ensureLocalPlayerRoutine != null)
        {
            StopCoroutine(ensureLocalPlayerRoutine);
            ensureLocalPlayerRoutine = null;
        }

        base.OnStopClient();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"{LogPrefix} OnServerConnect connId={conn.connectionId} hasIdentity={conn.identity != null} isJoinAllowed={(MatchManager.Instance == null || MatchManager.Instance.IsJoinAllowed)}");

        if (MatchManager.Instance != null && !MatchManager.Instance.IsJoinAllowed)
        {
            conn.Send(new ServerRejectionMessage { reason = ReasonMatchInProgress });
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"{LogPrefix} OnServerAddPlayer start connId={conn.connectionId} hasIdentity={conn.identity != null} playerPrefab={(playerPrefab != null ? playerPrefab.name : "null")} autoCreatePlayer={autoCreatePlayer}");

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
        Debug.Log($"{LogPrefix} Instantiate player name={player.name} at={spawnPos}");
        NetworkServer.AddPlayerForConnection(conn, player);
        Debug.Log($"{LogPrefix} AddPlayerForConnection done connId={conn.connectionId} connIdentity={(conn.identity != null ? conn.identity.netId.ToString() : "null")}");

        if (MatchManager.Instance != null)
            MatchManager.Instance.RegisterPlayer(player.GetComponent<NetworkIdentity>());
    }

    private static void OnServerRejection(ServerRejectionMessage msg)
    {
        Debug.LogWarning($"{LogPrefix} OnServerRejection reason='{msg.reason}'");
        LastDisconnectReason = msg.reason;
    }

    private bool ShouldSkipHostAutoAddPlayer()
    {
        return mode == NetworkManagerMode.Host
            && NetworkServer.localConnection != null
            && NetworkServer.localConnection.identity != null;
    }

    private void EnsureLocalPlayerExists()
    {
        Debug.Log($"{LogPrefix} EnsureLocalPlayerExists connected={NetworkClient.isConnected} ready={NetworkClient.ready} localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")} connIdentity={(NetworkClient.connection != null && NetworkClient.connection.identity != null ? NetworkClient.connection.identity.netId.ToString() : "null")}");

        if (!autoCreatePlayer || !NetworkClient.isConnected)
            return;

        if (ensureLocalPlayerRoutine != null)
            StopCoroutine(ensureLocalPlayerRoutine);

        ensureLocalPlayerRoutine = StartCoroutine(EnsureLocalPlayerRoutine());
    }

    private IEnumerator EnsureLocalPlayerRoutine()
    {
        Debug.Log($"{LogPrefix} EnsureLocalPlayerRoutine start");
        yield return null;
        yield return null;

        if (!NetworkClient.isConnected || NetworkClient.localPlayer != null || NetworkClient.connection == null)
        {
            Debug.Log($"{LogPrefix} EnsureLocalPlayerRoutine early-exit connected={NetworkClient.isConnected} localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")} hasConnection={NetworkClient.connection != null}");
            ensureLocalPlayerRoutine = null;
            yield break;
        }

        if (!NetworkClient.connection.isAuthenticated)
        {
            Debug.LogWarning($"{LogPrefix} EnsureLocalPlayerRoutine aborted because connection is not authenticated.");
            ensureLocalPlayerRoutine = null;
            yield break;
        }

        if (!NetworkClient.ready)
        {
            Debug.Log($"{LogPrefix} EnsureLocalPlayerRoutine calling Ready()");
            NetworkClient.Ready();
        }

        if (NetworkClient.localPlayer == null && NetworkClient.connection.identity == null)
        {
            Debug.LogWarning($"{LogPrefix} EnsureLocalPlayerRoutine calling AddPlayer() manually.");
            NetworkClient.AddPlayer();
        }

        Debug.Log($"{LogPrefix} EnsureLocalPlayerRoutine end localPlayer={(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "null")} connIdentity={(NetworkClient.connection != null && NetworkClient.connection.identity != null ? NetworkClient.connection.identity.netId.ToString() : "null")}");

        ensureLocalPlayerRoutine = null;
    }
}
