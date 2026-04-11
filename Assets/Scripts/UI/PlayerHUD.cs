using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections.Generic;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoPoolText;

    [Header("Slots")]
    [SerializeField] private Image[] slotImages = new Image[5];
    [SerializeField] private Image[] slotIcons = new Image[5];
    [SerializeField] private TextMeshProUGUI[] slotAmmoTexts = new TextMeshProUGUI[5];

    [Header("Slot Colors")]
    [SerializeField] private Color activeSlotColor = new Color(1f, 0.85f, 0.3f, 0.9f);
    [SerializeField] private Color inactiveSlotColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0.1f);

    [Header("Match Info")]
    [SerializeField] private TextMeshProUGUI aliveCountText;

    [Header("Kill Feed")]
    [SerializeField] private Transform killFeedParent;
    [SerializeField] private GameObject killFeedEntryPrefab;

    [Header("Death / Win")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winText;

    private PlayerHealth playerHealth;
    private PlayerInventory inventory;
    private bool deathShown;

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

        if (ammoPoolText == null && hudCanvas != null)
        {
            var t = hudCanvas.transform.Find("AmmoPoolText");
            if (t != null) ammoPoolText = t.GetComponent<TMPro.TextMeshProUGUI>();
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (hudCanvas != null)
            hudCanvas.SetActive(true);
        if (deathPanel != null)
            deathPanel.SetActive(false);
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

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
                if (item != null && item.itemType == ItemType.Ammo && i < inventory.slotAmmo.Count)
                {
                    slotAmmoTexts[i].text = inventory.slotAmmo[i].ToString();
                    slotAmmoTexts[i].enabled = true;
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

        if (!deathShown && playerHealth != null && playerHealth.IsDead)
        {
            deathShown = true;
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
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winText != null)
            {
                if (!string.IsNullOrEmpty(winnerName) && playerHealth != null && !playerHealth.IsDead)
                    winText.text = "Победа!";
                else if (!string.IsNullOrEmpty(winnerName))
                    winText.text = winnerName + " победил";
                else
                    winText.text = "Ничья";
            }
        }
    }
}
