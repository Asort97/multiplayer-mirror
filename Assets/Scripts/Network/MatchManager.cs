using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [SyncVar]
    public int aliveCount;

    private List<NetworkIdentity> alivePlayers = new List<NetworkIdentity>();
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

        string victimName = victim != null ? victim.gameObject.name : "???";
        string killerName = attacker != null ? attacker.gameObject.name : "";

        if (attacker != null)
            RpcShowKillFeed(killerName + " убил " + victimName);
        else
            RpcShowKillFeed(victimName + " погиб от зоны");

        if (!matchEnded && aliveCount == 1 && alivePlayers.Count == 1)
        {
            matchEnded = true;
            string winnerName = alivePlayers[0].gameObject.name;
            RpcAnnounceWinner(winnerName);
        }
        else if (!matchEnded && aliveCount <= 0)
        {
            matchEnded = true;
            RpcAnnounceWinner("");
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
    private void RpcAnnounceWinner(string winnerName)
    {
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
