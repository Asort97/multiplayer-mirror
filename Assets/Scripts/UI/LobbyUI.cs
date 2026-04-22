using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    private const string LogPrefix = "[LobbyUI]";

    public static string LocalNickname = "Player";
    private string instanceKey;
    [SerializeField] private GameObject centerPanel;
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

    [Header("Notice Popup")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text noticeText;
    [SerializeField] private Button noticeOkButton;

    [Header("Connection")]
    [SerializeField] private float connectTimeoutSeconds = 6f;

    private bool connectPending;
    private float connectTimeoutAt;
    private Coroutine hostStartRoutine;

    private void Start()
    {
        LogNetworkState("Start");

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
        InitializeNoticeUi();
        UpdateStats();

        NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
        NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
        NetworkClient.OnConnectedEvent -= HandleClientConnected;
        NetworkClient.OnConnectedEvent += HandleClientConnected;

        if (!string.IsNullOrEmpty(GameNetworkManager.LastDisconnectReason))
        {
            ShowNotice(GameNetworkManager.LastDisconnectReason);
            GameNetworkManager.LastDisconnectReason = null;
        }
    }

    private void OnDestroy()
    {
        LogNetworkState("OnDestroy");

        NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
        NetworkClient.OnConnectedEvent -= HandleClientConnected;

        if (hostStartRoutine != null)
            StopCoroutine(hostStartRoutine);
    }

    private void Update()
    {
        if (connectPending && Time.unscaledTime >= connectTimeoutAt)
        {
            connectPending = false;
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                NetworkManager.singleton.StopClient();
                ShowNotice("Не удалось подключиться к серверу. Проверьте IP-адрес и попробуйте снова.");
                if (statusText != null) statusText.text = "";
            }
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (noticePanel != null && noticePanel.activeSelf)
        {
            HideNotice();
            return;
        }

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
        LogNetworkState("OnHost click");

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

        if (hostStartRoutine != null)
            StopCoroutine(hostStartRoutine);

        EnsureNetworkManagerPersists();
        hostStartRoutine = StartCoroutine(StartHostRoutine());
    }

    private IEnumerator StartHostRoutine()
    {
        LogNetworkState("StartHostRoutine begin");

        statusText.text = "Запуск сервера...";
        GameNetworkManager.LastDisconnectReason = null;
        connectPending = false;

        SafeStopHost();
        LogNetworkState("StartHostRoutine after SafeStopHost");

        float waitUntil = Time.unscaledTime + 3f;
        while (!IsNetworkOffline() && Time.unscaledTime < waitUntil)
            yield return null;

        if (!IsNetworkOffline())
        {
            Debug.LogWarning($"{LogPrefix} StartHostRoutine aborted because network is not offline yet.");
            statusText.text = "";
            ShowNotice("Сеть еще завершает предыдущее подключение. Подождите секунду и нажмите 'Создать игру' снова.");
            hostStartRoutine = null;
            yield break;
        }

        try
        {
            Debug.Log($"{LogPrefix} Calling StartHost()");
            NetworkManager.singleton.StartHost();
            LogNetworkState("StartHost returned");
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            Debug.LogWarning($"StartHost failed: {ex.Message}");
            SafeStopHost();
            statusText.text = "";
            ShowNotice("Не удалось запустить сервер: порт уже занят. Попробуйте другой порт или закройте ранее запущенный сервер.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"StartHost failed: {ex.Message}");
            SafeStopHost();
            statusText.text = "";
            ShowNotice("Не удалось запустить сервер: " + ex.Message);
        }
        finally
        {
            hostStartRoutine = null;
        }
    }

    private static bool IsNetworkOffline()
    {
        if (NetworkManager.singleton == null)
            return true;

        return !NetworkClient.active
            && !NetworkServer.active
            && NetworkManager.singleton.mode == NetworkManagerMode.Offline;
    }

    private static void EnsureNetworkManagerPersists()
    {
        if (NetworkManager.singleton == null)
            return;

        GameObject managerObject = NetworkManager.singleton.gameObject;
        if (managerObject.scene.name == "DontDestroyOnLoad")
            return;

        Debug.Log($"{LogPrefix} Moving NetworkManager '{managerObject.name}' to DontDestroyOnLoad from scene '{managerObject.scene.name}'");
        DontDestroyOnLoad(managerObject);
    }

    private void LogNetworkState(string label)
    {
        Debug.Log($"{LogPrefix} {label}: {DescribeNetworkState()}");
    }

    private static string DescribeNetworkState()
    {
        if (NetworkManager.singleton == null)
            return "NetworkManager.singleton=null";

        string connectionState = NetworkClient.connection == null
            ? "conn=null"
            : $"authenticated={NetworkClient.connection.isAuthenticated}, identity={(NetworkClient.connection.identity != null ? NetworkClient.connection.identity.netId.ToString() : "null")}";

        string localPlayerState = NetworkClient.localPlayer != null
            ? $"localPlayer={NetworkClient.localPlayer.netId}"
            : "localPlayer=null";

        return $"mode={NetworkManager.singleton.mode}, clientActive={NetworkClient.active}, clientConnected={NetworkClient.isConnected}, clientReady={NetworkClient.ready}, serverActive={NetworkServer.active}, networkAddress={NetworkManager.singleton.networkAddress}, {connectionState}, {localPlayerState}";
    }

    private static void SafeStopHost()
    {
        try
        {
            if (NetworkManager.singleton != null)
                Debug.Log($"{LogPrefix} SafeStopHost before stop: {DescribeNetworkState()}");

            if (NetworkServer.active && NetworkClient.isConnected)
                NetworkManager.singleton.StopHost();
            else if (NetworkServer.active)
                NetworkManager.singleton.StopServer();
            else if (NetworkClient.isConnected || NetworkClient.active)
                NetworkManager.singleton.StopClient();

            if (NetworkManager.singleton != null)
                Debug.Log($"{LogPrefix} SafeStopHost after stop call: {DescribeNetworkState()}");
        }
        catch (System.Exception stopEx)
        {
            Debug.LogWarning($"SafeStopHost cleanup error: {stopEx.Message}");
        }
    }

    private void OnConnect()
    {
        LogNetworkState("OnConnect click");

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

        if (hostStartRoutine != null)
        {
            StopCoroutine(hostStartRoutine);
            hostStartRoutine = null;
        }

        EnsureNetworkManagerPersists();
        SafeStopHost();

        NetworkManager.singleton.networkAddress = ip;
        statusText.text = "Подключение...";
        GameNetworkManager.LastDisconnectReason = null;
        connectPending = true;
        connectTimeoutAt = Time.unscaledTime + connectTimeoutSeconds;
        try
        {
            Debug.Log($"{LogPrefix} Calling StartClient() to '{ip}'");
            NetworkManager.singleton.StartClient();
            LogNetworkState("StartClient returned");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"StartClient failed: {ex.Message}");
            connectPending = false;
            SafeStopHost();
            statusText.text = "";
            ShowNotice("Не удалось подключиться к серверу: " + ex.Message);
        }
    }

    private void HandleClientConnected()
    {
        LogNetworkState("HandleClientConnected");
        connectPending = false;
    }

    private void HandleClientDisconnected()
    {
        LogNetworkState("HandleClientDisconnected");
        bool wasPending = connectPending;
        connectPending = false;

        string reason = GameNetworkManager.LastDisconnectReason;
        if (!string.IsNullOrEmpty(reason))
        {
            GameNetworkManager.LastDisconnectReason = null;
            ShowNotice(reason);
            if (statusText != null) statusText.text = "";
            return;
        }

        if (wasPending)
        {
            ShowNotice("Не удалось подключиться к серверу. Проверьте IP-адрес и попробуйте снова.");
            if (statusText != null) statusText.text = "";
        }
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
        centerPanel.SetActive(false);
        settingsPanel.SetActive(true);
        HideExitConfirm();
    }

    private void HideSettings()
    {
        if (settingsPanel == null)
            return;

        HideExitConfirm();
        centerPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    private void ShowExitConfirm()
    {
        if (settingsPanel == null || exitConfirmPanel == null)
            return;

        centerPanel.SetActive(false);
        settingsPanel.SetActive(false);
        exitConfirmPanel.SetActive(true);
    }

    private void HideExitConfirm()
    {
        if (exitConfirmPanel == null)
            return;

        settingsPanel.SetActive(true);
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

    private void InitializeNoticeUi()
    {
        if (noticePanel == null) return;

        noticePanel.SetActive(false);
        if (noticeOkButton != null)
        {
            noticeOkButton.onClick.RemoveAllListeners();
            noticeOkButton.onClick.AddListener(GameAudioManager.PlayButtonClick);
            noticeOkButton.onClick.AddListener(HideNotice);
        }
    }

    public void ShowNotice(string message)
    {
        if (noticePanel == null || noticeText == null)
        {
            if (statusText != null) statusText.text = message;
            Debug.LogWarning("LobbyUI noticePanel/noticeText are not assigned: " + message);
            return;
        }

        noticeText.text = message;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (centerPanel != null) centerPanel.SetActive(true);

        noticePanel.SetActive(true);
    }

    private void HideNotice()
    {
        if (noticePanel == null) return;
        noticePanel.SetActive(false);
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
