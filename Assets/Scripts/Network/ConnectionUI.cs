using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.singleton;

        hostButton.onClick.AddListener(OnHostClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
        disconnectButton.onClick.AddListener(OnDisconnectClicked);

        disconnectButton.gameObject.SetActive(false);
        UpdateStatus("");
    }

    private void Update()
    {
        bool connected = NetworkClient.isConnected || NetworkServer.active;
        connectionPanel.SetActive(!connected);
        disconnectButton.gameObject.SetActive(connected);

        if (NetworkServer.active && NetworkClient.isConnected)
            UpdateStatus($"Сервер запущен — игроков: {NetworkServer.connections.Count}");
        else if (NetworkClient.isConnected)
            UpdateStatus("Подключено");
        else if (NetworkServer.active)
            UpdateStatus("Сервер работает");
        else
            UpdateStatus("");
    }

    private void OnHostClicked()
    {
        networkManager.StartHost();
    }

    private void OnConnectClicked()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
            ip = "localhost";

        networkManager.networkAddress = ip;
        networkManager.StartClient();
    }

    private void OnDisconnectClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            networkManager.StopHost();
        else if (NetworkClient.isConnected)
            networkManager.StopClient();
        else if (NetworkServer.active)
            networkManager.StopServer();
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
