using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoPoolText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button settingsQuitButton;
    [SerializeField] private GameObject exitConfirmPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;

    [Header("Slots")]
    [SerializeField] private Image[] slotImages = new Image[5];
    [SerializeField] private Image[] slotIcons = new Image[5];
    [SerializeField] private TextMeshProUGUI[] slotAmmoTexts = new TextMeshProUGUI[5];

    [Header("Slot Colors")]
    [SerializeField] private Color activeSlotColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color inactiveSlotColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);

    [Header("Match Info")]
    [SerializeField] private TextMeshProUGUI aliveCountText;

    [Header("Kill Feed")]
    [SerializeField] private Transform killFeedParent;
    [SerializeField] private GameObject killFeedEntryPrefab;

    [Header("Death / Win")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winText;

    [Header("Healing")]
    [SerializeField] private GameObject healProgressRoot;
    [SerializeField] private Image healProgressFill;

    [Header("Match Countdown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    private PlayerHealth playerHealth;
    private PlayerInventory inventory;
    private bool deathShown;
    private string lastShownNickname = "";

    private struct KillFeedEntry
    {
        public GameObject obj;
        public float spawnTime;
    }
    private List<KillFeedEntry> killFeedEntries = new List<KillFeedEntry>();

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inventory = GetComponent<PlayerInventory>();

        if (hudCanvas != null)
            hudCanvas.SetActive(false);

        if (ammoPoolText == null && hudCanvas != null)
        {
            var t = hudCanvas.transform.Find("AmmoPoolText");
            if (t != null) ammoPoolText = t.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (nicknameText == null && hudCanvas != null)
        {
            var t = hudCanvas.transform.Find("Nickname");
            if (t != null) nicknameText = t.GetComponent<TMP_Text>();
        }

        BindSlotCounters();
        ClearSlotCounters();

    }

    public override void OnStartLocalPlayer()
    {
        EnsureEventSystem();
        GameAudioManager.EnsureInstance();

        if (hudCanvas != null)
            hudCanvas.SetActive(true);
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
            var btn = deathPanel.transform.Find("LobbyButton");
            if (btn != null)
            {
                var b = btn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.AddListener(GameAudioManager.PlayButtonClick);
                    b.onClick.AddListener(ExitToLobby);
                }
            }
        }
        if (winPanel != null)
        {
            winPanel.SetActive(false);
            var btn = winPanel.transform.Find("LobbyButton");
            if (btn != null)
            {
                var b = btn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.AddListener(GameAudioManager.PlayButtonClick);
                    b.onClick.AddListener(ExitToLobby);
                }
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        InitializeSettingsUi();
        if (healProgressRoot != null)
            healProgressRoot.SetActive(false);
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        ClearSlotCounters();
        UpdateNicknameLabel();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (exitConfirmPanel != null && exitConfirmPanel.activeSelf)
                HideExitConfirm();
            else if (settingsPanel != null && settingsPanel.activeSelf)
                HideSettings();
            else
                ShowSettings();
        }

        if (healthBar != null && playerHealth != null)
        {
            healthBar.fillAmount = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth;
        }

        var activeItem = inventory.GetActiveItemData();

        if (ammoText != null)
        {
            if (activeItem != null && activeItem.itemType == ItemType.Ranged)
                ammoText.text = $"{inventory.GetActiveAmmo()}";
            else
                ammoText.text = "";
        }

        if (weaponNameText != null)
        {
            weaponNameText.text = activeItem != null ? activeItem.itemName : "Fists";
        }

        if (ammoPoolText != null)
        {
            ammoPoolText.text = $"9mm: {inventory.ammo9mm}  |  12sh: {inventory.ammo12Shells}";
        }

        UpdateNicknameLabel();

        for (int i = 0; i < PlayerInventory.SlotCount; i++)
        {
            bool isActive = i == inventory.ActiveSlot;
            var item = inventory.GetSlotItemData(i);

            if (i < slotImages.Length && slotImages[i] != null)
            {
                slotImages[i].color = isActive ? activeSlotColor : inactiveSlotColor;
            }

            if (i < slotIcons.Length && slotIcons[i] != null)
            {
                if (item != null && item.itemSprite != null)
                {
                    slotIcons[i].sprite = item.itemSprite;
                    slotIcons[i].color = Color.white;
                    slotIcons[i].enabled = true;
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].color = emptyIconColor;
                }
            }

            if (i < slotAmmoTexts.Length && slotAmmoTexts[i] != null)
            {
                if (item != null && i < inventory.slotAmmo.Count)
                {
                    bool stackableItem = item.itemType == ItemType.Ammo || item.itemType == ItemType.Heal;
                    if (stackableItem && inventory.slotAmmo[i] > 1)
                    {
                        slotAmmoTexts[i].text = $"x{inventory.slotAmmo[i]}";
                        slotAmmoTexts[i].enabled = true;
                    }
                    else
                    {
                        slotAmmoTexts[i].text = "";
                        slotAmmoTexts[i].enabled = false;
                    }
                }
                else
                {
                    slotAmmoTexts[i].text = "";
                    slotAmmoTexts[i].enabled = false;
                }
            }
        }

        if (aliveCountText != null && MatchManager.Instance != null)
        {
            aliveCountText.text = "Живых: " + MatchManager.Instance.aliveCount;
        }

        UpdateCountdownUi();

        if (!deathShown && playerHealth != null && playerHealth.IsDead)
        {
            deathShown = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (deathPanel != null)
                deathPanel.SetActive(true);
        }

        for (int i = killFeedEntries.Count - 1; i >= 0; i--)
        {
            if (Time.time - killFeedEntries[i].spawnTime > 5f)
            {
                Destroy(killFeedEntries[i].obj);
                killFeedEntries.RemoveAt(i);
            }
        }
    }

    private void BindSlotCounters()
    {
        for (int i = 0; i < slotAmmoTexts.Length && i < slotImages.Length; i++)
        {
            if (slotAmmoTexts[i] != null || slotImages[i] == null)
                continue;

            Transform counter = slotImages[i].transform.Find("counter");
            if (counter != null)
                slotAmmoTexts[i] = counter.GetComponent<TextMeshProUGUI>();
        }
    }

    private void ClearSlotCounters()
    {
        for (int i = 0; i < slotAmmoTexts.Length; i++)
        {
            if (slotAmmoTexts[i] == null)
                continue;

            slotAmmoTexts[i].text = "";
            slotAmmoTexts[i].enabled = false;
        }
    }

    public void AddKillFeedEntry(string message)
    {
        if (killFeedParent == null || killFeedEntryPrefab == null) return;

        var entry = Instantiate(killFeedEntryPrefab, killFeedParent);
        var tmp = entry.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = message;

        killFeedEntries.Add(new KillFeedEntry { obj = entry, spawnTime = Time.time });

        if (killFeedEntries.Count > 4)
        {
            Destroy(killFeedEntries[0].obj);
            killFeedEntries.RemoveAt(0);
        }
    }

    public void ShowWinScreen(string winnerName)
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameAudioManager.StopCurrentLoop();
        GameAudioManager.PlayNamed("victory");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winText != null)
            {
                if (!string.IsNullOrEmpty(winnerName))
                    winText.text = "Победа!";
                else
                    winText.text = "Ничья";
            }
        }
    }

    public void ExitToLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();

        var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
            es.AddComponent(inputSystemModuleType);
        else
            es.AddComponent<StandaloneInputModule>();
    }

    private void InitializeSettingsUi()
    {
        if (settingsButton == null || settingsPanel == null || musicSlider == null || sfxSlider == null || settingsCloseButton == null || settingsQuitButton == null || exitConfirmPanel == null || exitYesButton == null || exitNoButton == null)
        {
            Debug.LogWarning("PlayerHUD settings references are not fully assigned in MainPlayer prefab.");
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
        exitYesButton.onClick.AddListener(QuitGame);

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

    private void QuitGame()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        Application.Quit();
    }

    public void SetHealProgress(float progress, bool visible)
    {
        if (healProgressRoot == null || healProgressFill == null)
            return;

        healProgressRoot.SetActive(visible);
        healProgressFill.fillAmount = Mathf.Clamp01(progress);
    }

    private void UpdateCountdownUi()
    {
        if (countdownPanel == null && countdownText == null)
            return;

        var match = MatchManager.Instance;
        bool show = match != null && !match.HasStarted;

        if (countdownPanel != null && countdownPanel.activeSelf != show)
            countdownPanel.SetActive(show);

        if (show && countdownText != null)
        {
            if (match.WaitingForPlayers)
            {
                int playersNeeded = match.PlayersNeededForCountdown;
                countdownText.text = playersNeeded > 1
                    ? $"Ожидание еще {playersNeeded} игроков"
                    : "Ожидание второго игрока";
            }
            else
            {
                int seconds = Mathf.CeilToInt(match.RemainingCountdown);
                if (seconds < 0) seconds = 0;
                countdownText.text = seconds > 0 ? seconds.ToString() : "GO!";
            }
        }
    }

    private void UpdateNicknameLabel()
    {
        if (nicknameText == null)
            return;

        string nick = "";
        if (playerHealth != null && !string.IsNullOrWhiteSpace(playerHealth.playerName))
            nick = playerHealth.playerName;
        else if (!string.IsNullOrWhiteSpace(LobbyUI.LocalNickname))
            nick = LobbyUI.LocalNickname;

        if (string.IsNullOrWhiteSpace(nick))
            nick = "Player";

        if (nick == lastShownNickname)
            return;

        lastShownNickname = nick;
        nicknameText.text = nick;
    }
}
