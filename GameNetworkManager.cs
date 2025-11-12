using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

/// <summary>
/// Gestiona la conexión a Photon PUN2 y la transición entre escenas
/// Maneja crear servidor, unirse a sala, y cargar el mapa del juego
/// </summary>
public class GameNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Configuración de Photon")]
    [Tooltip("Nombre del juego/aplicación (debe coincidir con el configurado en Photon Dashboard)")]
    public string gameVersion = "1.0";

    [Tooltip("Región de Photon (deja vacío para auto)")]
    public string preferredRegion = ""; // us, eu, asia, etc. (vacío = auto)

    [Header("Configuración de Sala")]
    [Tooltip("Nombre por defecto de la sala")]
    public string defaultRoomName = "PangeaRoom";

    [Tooltip("Máximo de jugadores por sala")]
    public byte maxPlayersPerRoom = 4;

    [Header("UI - Referencias")]
    [Tooltip("Panel de selección de servidor/cliente")]
    public GameObject serverSelectionPanel;

    [Tooltip("Botón para crear/hostear servidor")]
    public Button hostButton;

    [Tooltip("Botón para unirse como cliente")]
    public Button joinButton;

    [Tooltip("Input field para el nombre de la sala")]
    public InputField roomNameInput;

    [Tooltip("Panel de estado/loading")]
    public GameObject statusPanel;

    [Tooltip("Texto de estado de conexión")]
    public Text statusText;

    [Header("Escenas")]
    [Tooltip("Nombre de la escena del mapa del juego (debe estar en Build Settings)")]
    public string gameSceneName = "GameMap";

    [Header("Referencia al Character Selection")]
    [Tooltip("Referencia al CharacterSelectionManager (para verificar personaje seleccionado)")]
    public CharacterSelectionManager characterSelectionManager;

    // Variables internas
    private bool isConnecting = false;
    private bool isHost = false;
    private string roomToJoin = "";

    void Start()
    {
        // Configurar botones
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(OnHostButtonClicked);
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }

        // Ocultar panel de estado al inicio
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }

        // Configurar versión del juego
        PhotonNetwork.GameVersion = gameVersion;

        // Sincronizar escenas automáticamente
        PhotonNetwork.AutomaticallySyncScene = true;

        Debug.Log("🌐 GameNetworkManager inicializado");
    }

    /// <summary>
    /// Botón para crear/hostear un servidor
    /// </summary>
    public void OnHostButtonClicked()
    {
        // Verificar que haya un personaje seleccionado
        if (characterSelectionManager != null && !characterSelectionManager.HasSelectedCharacter())
        {
            Debug.LogWarning("⚠️ Debes seleccionar un personaje primero");
            UpdateStatus("⚠️ Selecciona un personaje primero");
            return;
        }

        isHost = true;

        // Obtener nombre de la sala del input field
        if (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text))
        {
            roomToJoin = roomNameInput.text;
        }
        else
        {
            roomToJoin = defaultRoomName + "_" + Random.Range(1000, 9999);
        }

        Debug.Log($"🌐 Creando servidor: {roomToJoin}");
        UpdateStatus($"🌐 Creando servidor: {roomToJoin}...");

        // Ocultar panel de selección
        if (serverSelectionPanel != null)
        {
            serverSelectionPanel.SetActive(false);
        }

        // Mostrar panel de estado
        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
        }

        // Conectar a Photon
        ConnectToPhoton();
    }

    /// <summary>
    /// Botón para unirse como cliente
    /// </summary>
    public void OnJoinButtonClicked()
    {
        // Verificar que haya un personaje seleccionado
        if (characterSelectionManager != null && !characterSelectionManager.HasSelectedCharacter())
        {
            Debug.LogWarning("⚠️ Debes seleccionar un personaje primero");
            UpdateStatus("⚠️ Selecciona un personaje primero");
            return;
        }

        isHost = false;

        // Obtener nombre de la sala del input field
        if (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text))
        {
            roomToJoin = roomNameInput.text;
        }
        else
        {
            roomToJoin = defaultRoomName;
        }

        Debug.Log($"🌐 Uniéndose a: {roomToJoin}");
        UpdateStatus($"🌐 Uniéndose a: {roomToJoin}...");

        // Ocultar panel de selección
        if (serverSelectionPanel != null)
        {
            serverSelectionPanel.SetActive(false);
        }

        // Mostrar panel de estado
        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
        }

        // Conectar a Photon
        ConnectToPhoton();
    }

    /// <summary>
    /// Conecta a los servidores de Photon
    /// </summary>
    void ConnectToPhoton()
    {
        if (isConnecting)
        {
            Debug.LogWarning("⚠️ Ya está conectando...");
            return;
        }

        isConnecting = true;

        // Verificar si ya está conectado
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("✅ Ya conectado a Photon, creando/uniéndose a sala...");
            JoinOrCreateRoom();
        }
        else
        {
            Debug.Log("🌐 Conectando a Photon...");
            UpdateStatus("🌐 Conectando a Photon...");

            // Conectar a Photon Cloud
            if (!string.IsNullOrEmpty(preferredRegion))
            {
                PhotonNetwork.ConnectToRegion(preferredRegion);
            }
            else
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }
    }

    /// <summary>
    /// Callback: Conectado al Master Server de Photon
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Conectado al Master Server de Photon");
        UpdateStatus("✅ Conectado a Photon");

        // Unirse al lobby para ver salas disponibles
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("🌐 Uniéndose al lobby...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            JoinOrCreateRoom();
        }
    }

    /// <summary>
    /// Callback: Unido al Lobby
    /// </summary>
    public override void OnJoinedLobby()
    {
        Debug.Log("✅ Unido al Lobby");
        UpdateStatus("✅ En lobby, buscando sala...");

        JoinOrCreateRoom();
    }

    /// <summary>
    /// Intenta crear o unirse a la sala
    /// </summary>
    void JoinOrCreateRoom()
    {
        if (isHost)
        {
            // Crear nueva sala
            Debug.Log($"🌐 Creando sala: {roomToJoin} (max {maxPlayersPerRoom} jugadores)");
            UpdateStatus($"🌐 Creando sala: {roomToJoin}...");

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = maxPlayersPerRoom;
            roomOptions.IsVisible = true;
            roomOptions.IsOpen = true;

            PhotonNetwork.CreateRoom(roomToJoin, roomOptions);
        }
        else
        {
            // Unirse a sala existente
            Debug.Log($"🌐 Uniéndose a sala: {roomToJoin}");
            UpdateStatus($"🌐 Uniéndose a sala: {roomToJoin}...");

            PhotonNetwork.JoinRoom(roomToJoin);
        }
    }

    /// <summary>
    /// Callback: Sala creada exitosamente
    /// </summary>
    public override void OnCreatedRoom()
    {
        Debug.Log($"✅ Sala creada: {PhotonNetwork.CurrentRoom.Name}");
        UpdateStatus($"✅ Sala creada: {PhotonNetwork.CurrentRoom.Name}");
    }

    /// <summary>
    /// Callback: Unido a la sala
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"✅ Unido a sala: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"👥 Jugadores en sala: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        UpdateStatus($"✅ En sala ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} jugadores)");

        // Esperar un momento antes de cargar la escena
        StartCoroutine(LoadGameSceneAfterDelay(1.5f));
    }

    /// <summary>
    /// Carga la escena del juego después de un delay
    /// </summary>
    IEnumerator LoadGameSceneAfterDelay(float delay)
    {
        UpdateStatus($"🎮 Cargando mapa en {delay:F1}s...");
        yield return new WaitForSeconds(delay);

        // Solo el host carga la escena (se sincroniza automáticamente)
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"🗺️ Master Client cargando escena: {gameSceneName}");
            UpdateStatus("🗺️ Cargando mapa...");
            PhotonNetwork.LoadLevel(gameSceneName);
        }
        else
        {
            Debug.Log("🗺️ Esperando que el Master Client cargue la escena...");
            UpdateStatus("🗺️ Esperando al host...");
        }
    }

    /// <summary>
    /// Callback: Error al unirse a sala
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Error al unirse a sala: {message} (código: {returnCode})");
        UpdateStatus($"❌ Sala no encontrada");

        // Si no se puede unir, intentar crear la sala
        Debug.Log("🌐 Intentando crear nueva sala...");
        UpdateStatus("🌐 Creando nueva sala...");

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(roomToJoin, roomOptions);
    }

    /// <summary>
    /// Callback: Error al crear sala
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Error al crear sala: {message} (código: {returnCode})");
        UpdateStatus($"❌ Error al crear sala");

        isConnecting = false;

        // Mostrar panel de selección de nuevo
        if (serverSelectionPanel != null)
        {
            serverSelectionPanel.SetActive(true);
        }

        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Callback: Desconectado de Photon
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"⚠️ Desconectado de Photon: {cause}");
        UpdateStatus($"⚠️ Desconectado: {cause}");

        isConnecting = false;

        // Mostrar panel de selección de nuevo
        if (serverSelectionPanel != null)
        {
            serverSelectionPanel.SetActive(true);
        }

        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Actualiza el texto de estado
    /// </summary>
    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log($"[STATUS] {message}");
    }

    /// <summary>
    /// Callback: Otro jugador se unió a la sala
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"👥 Jugador {newPlayer.NickName} se unió. Total: {PhotonNetwork.CurrentRoom.PlayerCount}");
        UpdateStatus($"👥 {newPlayer.NickName} se unió ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
    }

    /// <summary>
    /// Callback: Otro jugador salió de la sala
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"👥 Jugador {otherPlayer.NickName} salió. Total: {PhotonNetwork.CurrentRoom.PlayerCount}");
        UpdateStatus($"👥 {otherPlayer.NickName} salió ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
    }

    /// <summary>
    /// Método público para desconectar y volver al menú
    /// </summary>
    public void Disconnect()
    {
        Debug.Log("🌐 Desconectando...");
        UpdateStatus("🌐 Desconectando...");

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        // Volver a la escena de selección
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
