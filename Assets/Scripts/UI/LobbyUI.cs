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
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button settingsQuitButton;
    [SerializeField] private GameObject exitConfirmPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        var audioManager = GameAudioManager.EnsureInstance();
        audioManager.PlayMusic("menu_music");

        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.Init();

        instanceKey = BuildInstanceKey();

        string savedNick = "";
        if (DatabaseManager.Instance != null)
            savedNick = DatabaseManager.Instance.GetSavedNickname(instanceKey);

        if (string.IsNullOrWhiteSpace(savedNick))
            savedNick = BuildDefaultNickname(instanceKey);

        nicknameInput.text = savedNick;
        LocalNickname = savedNick;
        ipInput.text = PlayerPrefs.GetString("LastIP", "localhost");

        hostButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        hostButton.onClick.AddListener(OnHost);
        connectButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        connectButton.onClick.AddListener(OnConnect);
        if (statsButton != null)
        {
            statsButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
            statsButton.onClick.AddListener(OpenStats);
        }
        if (closeStatsButton != null)
        {
            closeStatsButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
            closeStatsButton.onClick.AddListener(CloseStats);
        }

        nicknameInput.onEndEdit.AddListener(OnNicknameChanged);
        CloseStats();
        InitializeSettingsUi();
        UpdateStats();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (exitConfirmPanel != null && exitConfirmPanel.activeSelf)
        {
            HideExitConfirm();
            return;
        }

        ShowExitConfirm();
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

    private void InitializeSettingsUi()
    {
        if (settingsButton == null || settingsPanel == null || musicSlider == null || sfxSlider == null || settingsCloseButton == null || settingsQuitButton == null || exitConfirmPanel == null || exitYesButton == null || exitNoButton == null)
        {
            Debug.LogWarning("LobbyUI settings references are not fully assigned in the Lobby scene.");
            return;
        }

        settingsPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);

        var audioManager = GameAudioManager.EnsureInstance();
        musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
        sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);

        musicSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
        sfxSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.AddListener(audioManager.SetSfxVolume);

        settingsButton.onClick.RemoveAllListeners();
        settingsButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        settingsButton.onClick.AddListener(ToggleSettings);

        settingsCloseButton.onClick.RemoveAllListeners();
        settingsCloseButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        settingsCloseButton.onClick.AddListener(HideSettings);

        settingsQuitButton.onClick.RemoveAllListeners();
        settingsQuitButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        settingsQuitButton.onClick.AddListener(ShowExitConfirm);

        exitYesButton.onClick.RemoveAllListeners();
        exitYesButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        exitYesButton.onClick.AddListener(ConfirmQuit);

        exitNoButton.onClick.RemoveAllListeners();
        exitNoButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
        exitNoButton.onClick.AddListener(HideExitConfirm);
    }

    private void ToggleSettings()
    {
        if (settingsPanel == null)
            return;

        if (settingsPanel.activeSelf)
            HideSettings();
        else
            ShowSettings();
    }

    private void ShowSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(true);
        HideExitConfirm();
    }

    private void HideSettings()
    {
        if (settingsPanel == null)
            return;

        HideExitConfirm();
        settingsPanel.SetActive(false);
    }

    private void ShowExitConfirm()
    {
        if (settingsPanel == null || exitConfirmPanel == null)
            return;

        settingsPanel.SetActive(true);
        exitConfirmPanel.SetActive(true);
    }

    private void HideExitConfirm()
    {
        if (exitConfirmPanel == null)
            return;

        exitConfirmPanel.SetActive(false);
    }

    private void ConfirmQuit()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        Application.Quit();
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
