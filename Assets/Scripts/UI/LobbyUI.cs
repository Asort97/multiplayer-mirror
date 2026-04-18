using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class LobbyUI : MonoBehaviour
{
    public static string LocalNickname = "Player";
    private string instanceKey;

    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button closeStatsButton;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.Init();

        instanceKey = BuildInstanceKey();

        string savedNick = "";
        if (DatabaseManager.Instance != null)
            savedNick = DatabaseManager.Instance.GetSavedNickname(instanceKey);

        if (string.IsNullOrWhiteSpace(savedNick))
            savedNick = BuildDefaultNickname(instanceKey);

        nicknameInput.text = savedNick;
        ipInput.text = PlayerPrefs.GetString("LastIP", "localhost");

        hostButton.onClick.AddListener(OnHost);
        connectButton.onClick.AddListener(OnConnect);
        if (statsButton != null)
            statsButton.onClick.AddListener(OpenStats);
        if (closeStatsButton != null)
            closeStatsButton.onClick.AddListener(CloseStats);

        nicknameInput.onEndEdit.AddListener(OnNicknameChanged);
        CloseStats();
        UpdateStats();
    }

    private void OnNicknameChanged(string value)
    {
        string nick = value.Trim();
        if (DatabaseManager.Instance != null && !string.IsNullOrEmpty(nick))
            DatabaseManager.Instance.SaveNickname(instanceKey, nick);

        UpdateStats();
    }

    private void UpdateStats()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick) || DatabaseManager.Instance == null)
        {
            statsText.text = "";
            return;
        }

        var stats = DatabaseManager.Instance.GetPlayerStats(nick);
        statsText.text = string.Format(
            "Победы: {0}\nУбийства: {1}\nСмерти: {2}",
            stats.wins, stats.kills, stats.deaths
        );
    }

    private void OnHost()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick))
        {
            statusText.text = "Введите никнейм!";
            return;
        }

        LocalNickname = nick;
        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.SaveNickname(instanceKey, nick);
        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.GetOrCreatePlayer(nick);

        statusText.text = "Запуск сервера...";
        NetworkManager.singleton.StartHost();
    }

    private void OnConnect()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick))
        {
            statusText.text = "Введите никнейм!";
            return;
        }

        LocalNickname = nick;
        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.SaveNickname(instanceKey, nick);
        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.GetOrCreatePlayer(nick);

        string ip = ipInput.text.Trim();
        if (string.IsNullOrEmpty(ip)) ip = "localhost";
        PlayerPrefs.SetString("LastIP", ip);

        NetworkManager.singleton.networkAddress = ip;
        statusText.text = "Подключение...";
        NetworkManager.singleton.StartClient();
    }

    public void OpenStats()
    {
        UpdateStats();
        if (statsPanel != null)
            statsPanel.SetActive(true);
    }

    public void CloseStats()
    {
        if (statsPanel != null)
            statsPanel.SetActive(false);
    }

    private string BuildInstanceKey()
    {
        return Application.dataPath.ToLowerInvariant();
    }

    private string BuildDefaultNickname(string key)
    {
        int hash = Mathf.Abs(key.GetHashCode());
        int suffix = hash % 10000;
        return "Player" + suffix.ToString("D4");
    }
}
