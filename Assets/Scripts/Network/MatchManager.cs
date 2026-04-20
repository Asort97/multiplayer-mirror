using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [SyncVar]
    public int aliveCount;

    private List<NetworkIdentity> alivePlayers = new List<NetworkIdentity>();
    private Dictionary<NetworkIdentity, int> playerKills = new Dictionary<NetworkIdentity, int>();
    private bool matchEnded;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [Server]
    public void RegisterPlayer(NetworkIdentity player)
    {
        if (!alivePlayers.Contains(player))
        {
            alivePlayers.Add(player);
            aliveCount = alivePlayers.Count;
        }
    }

    [Server]
    public void OnPlayerKilled(NetworkIdentity victim, NetworkIdentity attacker)
    {
        if (alivePlayers.Contains(victim))
        {
            alivePlayers.Remove(victim);
            aliveCount = alivePlayers.Count;
        }

        if (attacker != null)
        {
            if (!playerKills.ContainsKey(attacker))
                playerKills[attacker] = 0;
            playerKills[attacker]++;
        }

        string victimName = victim != null ? GetPlayerName(victim) : "???";
        string killerName = attacker != null ? GetPlayerName(attacker) : "";

        if (attacker != null)
            RpcShowKillFeed(killerName + " убил " + victimName);
        else
            RpcShowKillFeed(victimName + " погиб от зоны");

        if (!matchEnded && aliveCount == 1 && alivePlayers.Count == 1)
        {
            matchEnded = true;
            NetworkIdentity winner = alivePlayers[0];
            string winnerName = GetPlayerName(winner);
            RpcAnnounceWinner(winner != null ? winner.netId : 0u, winnerName);
            RecordAllStats(alivePlayers[0]);
        }
        else if (!matchEnded && aliveCount <= 0)
        {
            matchEnded = true;
            RpcAnnounceWinner(0u, "");
            RecordAllStats(null);
        }
    }

    [Server]
    private string GetPlayerName(NetworkIdentity player)
    {
        if (player == null) return "???";
        var health = player.GetComponent<PlayerHealth>();
        if (health != null && !string.IsNullOrEmpty(health.playerName))
            return health.playerName;
        return player.gameObject.name;
    }

    [Server]
    private void RecordAllStats(NetworkIdentity winner)
    {
        if (DatabaseManager.Instance == null) return;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;
            var health = conn.identity.GetComponent<PlayerHealth>();
            if (health == null || string.IsNullOrEmpty(health.playerName)) continue;

            bool won = (winner != null && conn.identity == winner);
            int kills = 0;
            if (playerKills.ContainsKey(conn.identity))
                kills = playerKills[conn.identity];

            DatabaseManager.Instance.RecordMatchResult(health.playerName, won, kills);
        }
    }

    [ClientRpc]
    private void RpcShowKillFeed(string message)
    {
        var hud = FindLocalHUD();
        if (hud != null)
            hud.AddKillFeedEntry(message);
    }

    [ClientRpc]
    private void RpcAnnounceWinner(uint winnerNetId, string winnerName)
    {
        if (winnerNetId != 0u && (NetworkClient.localPlayer == null || NetworkClient.localPlayer.netId != winnerNetId))
            return;

        var hud = FindLocalHUD();
        if (hud != null)
            hud.ShowWinScreen(winnerName);
    }

    private PlayerHUD FindLocalHUD()
    {
        var player = NetworkClient.localPlayer;
        if (player != null)
            return player.GetComponent<PlayerHUD>();
        return null;
    }
}
