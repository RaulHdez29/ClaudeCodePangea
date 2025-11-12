using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gestor del Lobby - Permite seleccionar jugadores y configurar la sesión
/// Se usa en la escena del lobby antes de cargar el mapa
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("🎮 Configuración de Jugadores")]
    [Tooltip("Lista de jugadores configurables (máximo 4)")]
    public List<PlayerData> playerSlots = new List<PlayerData>(4);

    [Header("🖼️ UI - Slots de Jugadores")]
    [Tooltip("Paneles UI para cada slot de jugador")]
    public GameObject[] playerSlotPanels = new GameObject[4];

    [Tooltip("Toggles para activar/desactivar cada slot")]
    public Toggle[] playerActiveToggles = new Toggle[4];

    [Tooltip("Input fields para nombres de jugadores")]
    public TMP_InputField[] playerNameInputs = new TMP_InputField[4];

    [Tooltip("Dropdowns para seleccionar prefabs")]
    public TMP_Dropdown[] prefabDropdowns = new TMP_Dropdown[4];

    [Tooltip("Dropdowns para seleccionar UI")]
    public TMP_Dropdown[] uiDropdowns = new TMP_Dropdown[4];

    [Header("📦 Prefabs Disponibles")]
    [Tooltip("Lista de prefabs de dinosaurios disponibles")]
    public GameObject[] availableDinosaurPrefabs;

    [Header("🖼️ UI Disponibles")]
    [Tooltip("Lista de Canvas UI disponibles")]
    public GameObject[] availableUICanvases;

    [Header("🌐 UI - Botones de Red")]
    [Tooltip("Botón para crear sesión (Host)")]
    public Button createSessionButton;

    [Tooltip("Botón para unirse a sesión")]
    public Button joinSessionButton;

    [Tooltip("Botón para iniciar juego (cargar mapa)")]
    public Button startGameButton;

    [Tooltip("Input field para nombre de sesión")]
    public TMP_InputField sessionNameInput;

    [Header("📊 UI - Información")]
    [Tooltip("Texto para mostrar estado de conexión")]
    public TMP_Text connectionStatusText;

    [Tooltip("Panel de lobby (se oculta al conectar)")]
    public GameObject lobbyPanel;

    [Tooltip("Panel de sesión activa (se muestra al conectar)")]
    public GameObject connectedPanel;

    // Estado
    private bool isConnected = false;

    void Start()
    {
        // Inicializar slots de jugadores
        InitializePlayerSlots();

        // Configurar botones
        SetupButtons();

        // Actualizar UI
        UpdateUI();
    }

    /// <summary>
    /// Inicializar slots de jugadores con valores por defecto
    /// </summary>
    void InitializePlayerSlots()
    {
        // Asegurar que hay 4 slots
        while (playerSlots.Count < 4)
        {
            playerSlots.Add(new PlayerData());
        }

        // Configurar cada slot
        for (int i = 0; i < playerSlots.Count; i++)
        {
            playerSlots[i].playerName = $"Player {i + 1}";
            playerSlots[i].isActive = (i == 0); // Solo el primer slot activo por defecto

            // Configurar dropdowns de prefabs
            if (prefabDropdowns[i] != null && availableDinosaurPrefabs.Length > 0)
            {
                prefabDropdowns[i].ClearOptions();
                List<string> prefabNames = new List<string>();
                foreach (var prefab in availableDinosaurPrefabs)
                {
                    prefabNames.Add(prefab.name);
                }
                prefabDropdowns[i].AddOptions(prefabNames);
                prefabDropdowns[i].value = i % availableDinosaurPrefabs.Length;

                int slotIndex = i; // Capturar índice para closure
                prefabDropdowns[i].onValueChanged.AddListener((value) => OnPrefabSelected(slotIndex, value));
            }

            // Configurar dropdowns de UI
            if (uiDropdowns[i] != null && availableUICanvases.Length > 0)
            {
                uiDropdowns[i].ClearOptions();
                List<string> uiNames = new List<string>();
                foreach (var ui in availableUICanvases)
                {
                    uiNames.Add(ui.name);
                }
                uiDropdowns[i].AddOptions(uiNames);
                uiDropdowns[i].value = i % availableUICanvases.Length;

                int slotIndex = i;
                uiDropdowns[i].onValueChanged.AddListener((value) => OnUISelected(slotIndex, value));
            }

            // Configurar toggles
            if (playerActiveToggles[i] != null)
            {
                playerActiveToggles[i].isOn = playerSlots[i].isActive;
                int slotIndex = i;
                playerActiveToggles[i].onValueChanged.AddListener((value) => OnPlayerToggled(slotIndex, value));
            }

            // Configurar input de nombre
            if (playerNameInputs[i] != null)
            {
                playerNameInputs[i].text = playerSlots[i].playerName;
                int slotIndex = i;
                playerNameInputs[i].onEndEdit.AddListener((value) => OnNameChanged(slotIndex, value));
            }

            // Asignar prefab y UI iniciales
            if (availableDinosaurPrefabs.Length > 0)
            {
                playerSlots[i].dinosaurPrefab = availableDinosaurPrefabs[i % availableDinosaurPrefabs.Length];
            }

            if (availableUICanvases.Length > 0)
            {
                playerSlots[i].playerUICanvas = availableUICanvases[i % availableUICanvases.Length];
            }
        }
    }

    /// <summary>
    /// Configurar listeners de botones
    /// </summary>
    void SetupButtons()
    {
        if (createSessionButton != null)
        {
            createSessionButton.onClick.RemoveAllListeners();
            createSessionButton.onClick.AddListener(OnCreateSession);
        }

        if (joinSessionButton != null)
        {
            joinSessionButton.onClick.RemoveAllListeners();
            joinSessionButton.onClick.AddListener(OnJoinSession);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGame);
            startGameButton.interactable = false; // Desactivado hasta conectar
        }
    }

    /// <summary>
    /// Callback cuando se activa/desactiva un jugador
    /// </summary>
    void OnPlayerToggled(int slotIndex, bool isActive)
    {
        playerSlots[slotIndex].isActive = isActive;

        // Actualizar panel UI
        if (playerSlotPanels[slotIndex] != null)
        {
            playerSlotPanels[slotIndex].SetActive(isActive);
        }

        Debug.Log($"Slot {slotIndex}: {(isActive ? "Activado" : "Desactivado")}");
    }

    /// <summary>
    /// Callback cuando se cambia el nombre del jugador
    /// </summary>
    void OnNameChanged(int slotIndex, string newName)
    {
        playerSlots[slotIndex].playerName = newName;
        Debug.Log($"Slot {slotIndex}: Nombre cambiado a '{newName}'");
    }

    /// <summary>
    /// Callback cuando se selecciona un prefab
    /// </summary>
    void OnPrefabSelected(int slotIndex, int prefabIndex)
    {
        if (prefabIndex >= 0 && prefabIndex < availableDinosaurPrefabs.Length)
        {
            playerSlots[slotIndex].dinosaurPrefab = availableDinosaurPrefabs[prefabIndex];
            Debug.Log($"Slot {slotIndex}: Prefab '{availableDinosaurPrefabs[prefabIndex].name}' seleccionado");
        }
    }

    /// <summary>
    /// Callback cuando se selecciona una UI
    /// </summary>
    void OnUISelected(int slotIndex, int uiIndex)
    {
        if (uiIndex >= 0 && uiIndex < availableUICanvases.Length)
        {
            playerSlots[slotIndex].playerUICanvas = availableUICanvases[uiIndex];
            Debug.Log($"Slot {slotIndex}: UI '{availableUICanvases[uiIndex].name}' seleccionada");
        }
    }

    /// <summary>
    /// Crear sesión (Host)
    /// </summary>
    void OnCreateSession()
    {
        Debug.Log("🌐 Creando sesión...");

        // Actualizar nombre de sesión
        if (sessionNameInput != null && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.sessionName = sessionNameInput.text;
        }

        // Configurar jugadores en el NetworkGameManager
        List<PlayerData> activePlayers = GetActivePlayers();
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.SetPlayersFromLobby(activePlayers);
            NetworkGameManager.Instance.StartHost();
        }
        else
        {
            Debug.LogError("❌ NetworkGameManager no encontrado!");
            return;
        }

        // Actualizar UI
        isConnected = true;
        UpdateUI();
        UpdateConnectionStatus("✅ Sesión creada. Esperando jugadores...");
    }

    /// <summary>
    /// Unirse a sesión
    /// </summary>
    void OnJoinSession()
    {
        Debug.Log("🌐 Uniéndose a sesión...");

        // Actualizar nombre de sesión
        if (sessionNameInput != null && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.sessionName = sessionNameInput.text;
        }

        // Configurar jugadores en el NetworkGameManager
        List<PlayerData> activePlayers = GetActivePlayers();
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.SetPlayersFromLobby(activePlayers);
            NetworkGameManager.Instance.JoinSession();
        }
        else
        {
            Debug.LogError("❌ NetworkGameManager no encontrado!");
            return;
        }

        // Actualizar UI
        isConnected = true;
        UpdateUI();
        UpdateConnectionStatus("🔍 Buscando sesión...");
    }

    /// <summary>
    /// Iniciar juego (cargar mapa)
    /// </summary>
    void OnStartGame()
    {
        if (!isConnected)
        {
            Debug.LogWarning("⚠️ Debes crear o unirte a una sesión primero!");
            return;
        }

        Debug.Log("🚀 Iniciando juego...");

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("❌ NetworkGameManager no encontrado!");
        }
    }

    /// <summary>
    /// Obtener lista de jugadores activos
    /// </summary>
    List<PlayerData> GetActivePlayers()
    {
        List<PlayerData> activePlayers = new List<PlayerData>();
        foreach (var player in playerSlots)
        {
            if (player.isActive)
            {
                activePlayers.Add(player);
            }
        }
        return activePlayers;
    }

    /// <summary>
    /// Actualizar UI según el estado
    /// </summary>
    void UpdateUI()
    {
        // Ocultar lobby, mostrar panel de sesión conectada
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(!isConnected);
        }

        if (connectedPanel != null)
        {
            connectedPanel.SetActive(isConnected);
        }

        // Activar botón de inicio de juego cuando está conectado
        if (startGameButton != null)
        {
            startGameButton.interactable = isConnected;
        }

        // Desactivar botones de conexión cuando ya está conectado
        if (createSessionButton != null)
        {
            createSessionButton.interactable = !isConnected;
        }

        if (joinSessionButton != null)
        {
            joinSessionButton.interactable = !isConnected;
        }
    }

    /// <summary>
    /// Actualizar texto de estado de conexión
    /// </summary>
    void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }
    }

    /// <summary>
    /// Obtener resumen de jugadores activos
    /// </summary>
    public string GetPlayersSummary()
    {
        List<PlayerData> activePlayers = GetActivePlayers();
        string summary = $"Jugadores activos: {activePlayers.Count}\n";
        foreach (var player in activePlayers)
        {
            summary += $"- {player.playerName} ({player.dinosaurPrefab.name})\n";
        }
        return summary;
    }
}
