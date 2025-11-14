using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Manager simple de servidores con Photon PUN2
/// Conecta a Photon y carga el mapa
/// </summary>
public class SimpleServerManager : MonoBehaviourPunCallbacks
{
    [Header("Configuración")]
    [Tooltip("Nombre del juego (versión)")]
    public string gameVersion = "1.0";

    [Tooltip("Nombre por defecto de la sala")]
    public string roomName = "Room1";

    [Tooltip("Máximo de jugadores")]
    public byte maxPlayers = 4;

    [Tooltip("Nombre de la escena del mapa (debe estar en Build Settings)")]
    public string mapSceneName = "GameMap";

    [Header("UI")]
    [Tooltip("Input para el nombre de la sala (opcional)")]
    public InputField roomNameInput;

    [Tooltip("Texto de estado")]
    public Text statusText;

    [Tooltip("Panel de servidores")]
    public GameObject serverPanel;

    void Start()
    {
        // Configurar Photon
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.AutomaticallySyncScene = true;

        UpdateStatus("Listo para conectar");
    }

    /// <summary>
    /// Conecta y crea/une a una sala
    /// </summary>
    public void ConnectToServer()
    {
        // Obtener nombre de sala del input si existe
        if (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text))
        {
            roomName = roomNameInput.text;
        }

        Debug.Log($"🌐 Conectando a sala: {roomName}");
        UpdateStatus("Conectando...");

        // Si ya está conectado, crear/unirse directamente
        if (PhotonNetwork.IsConnected)
        {
            JoinOrCreateRoom();
        }
        else
        {
            // Conectar a Photon
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    /// <summary>
    /// Callback: Conectado a Photon
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Conectado a Photon");
        UpdateStatus("Conectado, uniéndose a sala...");
        JoinOrCreateRoom();
    }

    /// <summary>
    /// Crea o se une a la sala
    /// </summary>
    void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    /// <summary>
    /// Callback: Unido a la sala
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"✅ En sala: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"👥 Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        UpdateStatus($"En sala ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");

        // Esperar un momento y cargar el mapa
        Invoke("LoadMap", 1f);
    }

    /// <summary>
    /// Carga el mapa del juego
    /// </summary>
    void LoadMap()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"🗺️ Cargando mapa: {mapSceneName}");
            UpdateStatus("Cargando mapa...");
            PhotonNetwork.LoadLevel(mapSceneName);
        }
        else
        {
            UpdateStatus("Esperando al host...");
        }
    }

    /// <summary>
    /// Callback: Error al crear/unirse
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Error: {message}");
        UpdateStatus("Error al unirse");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Error: {message}");
        UpdateStatus("Error al crear sala");
    }

    /// <summary>
    /// Callback: Desconectado
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"⚠️ Desconectado: {cause}");
        UpdateStatus($"Desconectado: {cause}");
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
    }
}
