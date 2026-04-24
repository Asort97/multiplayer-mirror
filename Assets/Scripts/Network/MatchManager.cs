using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MatchManager : NetworkBehaviour
{
    public enum MatchState
    {
        WaitingForPlayers = 0,
        Countdown = 1,
        InProgress = 2,
        Ended = 3
    }

    public static MatchManager Instance { get; private set; }

    [Header("Match Flow")]
    [SerializeField] private int minPlayersForCountdown = 2;
    [SerializeField] private float countdownDuration = 10f;

    [SyncVar]
    public int aliveCount;

    [SyncVar] private MatchState state = MatchState.WaitingForPlayers;
    [SyncVar] private double countdownEndTime;

    private List<NetworkIdentity> alivePlayers = new List<NetworkIdentity>();
    private Dictionary<NetworkIdentity, int> playerKills = new Dictionary<NetworkIdentity, int>();
    private bool matchEnded;

    public MatchState State => state;
    public bool IsJoinAllowed => state == MatchState.WaitingForPlayers || state == MatchState.Countdown;
    public bool HasStarted => state == MatchState.InProgress || state == MatchState.Ended;
    public bool InCountdown => state == MatchState.Countdown;
    public bool WaitingForPlayers => state == MatchState.WaitingForPlayers;
    public int PlayersNeededForCountdown => Mathf.Max(0, minPlayersForCountdown - aliveCount);

    public float RemainingCountdown
    {
        get
        {
            if (state != MatchState.Countdown) return 0f;
            return Mathf.Max(0f, (float)(countdownEndTime - NetworkTime.time));
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!isServer) return;
        if (state != MatchState.Countdown) return;

        if (NetworkTime.time >= countdownEndTime)
        {
            state = MatchState.InProgress;
            RpcMatchStarted();
        }
    }

    [Server]
    public void RegisterPlayer(NetworkIdentity player)
    {
        if (!alivePlayers.Contains(player))
        {
            alivePlayers.Add(player);
            aliveCount = alivePlayers.Count;
        }

        if (state == MatchState.WaitingForPlayers && alivePlayers.Count >= minPlayersForCountdown)
        {
            state = MatchState.Countdown;
            countdownEndTime = NetworkTime.time + countdownDuration;
        }
    }

    [Server]
    public void OnPlayerDisconnected(NetworkIdentity player)
    {
        if (player == null)
            return;

        string playerName = GetPlayerName(player);
        bool wasAlive = alivePlayers.Remove(player);

        if (playerKills.ContainsKey(player))
            playerKills.Remove(player);

        if (!wasAlive)
            return;

        aliveCount = alivePlayers.Count;
        RpcShowKillFeed(playerName + " покинул игру");

        if (state == MatchState.Countdown && alivePlayers.Count < minPlayersForCountdown)
        {
            state = MatchState.WaitingForPlayers;
            countdownEndTime = 0;
            return;
        }

        TryResolveMatchEnd();
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

        TryResolveMatchEnd();
    }

    [Server]
    private void TryResolveMatchEnd()
    {
        if (state != MatchState.InProgress) return;

        if (!matchEnded && aliveCount == 1 && alivePlayers.Count == 1)
        {
            matchEnded = true;
            state = MatchState.Ended;
            NetworkIdentity winner = alivePlayers[0];
            string winnerName = GetPlayerName(winner);
            RpcAnnounceWinner(winner != null ? winner.netId : 0u, winnerName);
            RecordAllStats(alivePlayers[0]);
        }
        else if (!matchEnded && aliveCount <= 0)
        {
            matchEnded = true;
            state = MatchState.Ended;
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

    [ClientRpc]
    private void RpcMatchStarted()
    {
        // Reserved hook for future client-side match-start effects. HUD currently polls State.
    }

    private PlayerHUD FindLocalHUD()
    {
        var player = NetworkClient.localPlayer;
        if (player != null)
            return player.GetComponent<PlayerHUD>();
        return null;
    }
}
