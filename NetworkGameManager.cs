using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestor principal de red para Photon Fusion
/// Maneja la conexión, creación de sesión y spawn de jugadores
/// </summary>
public class NetworkGameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("🌐 Configuración de Red")]
    [Tooltip("Nombre de la sesión/room")]
    public string sessionName = "DinosaurGame";

    [Tooltip("Modo de juego (Shared = todos son cliente/servidor)")]
    public GameMode gameMode = GameMode.Shared;

    [Tooltip("Máximo de jugadores")]
    public int maxPlayers = 4;

    [Header("🗺️ Escenas")]
    [Tooltip("Nombre de la escena del lobby")]
    public string lobbySceneName = "Lobby";

    [Tooltip("Nombre de la escena del juego")]
    public string gameSceneName = "GameMap";

    [Header("📍 Spawn")]
    [Tooltip("Puntos de spawn en el mapa")]
    public Transform[] spawnPoints;

    [Tooltip("Usar puntos de spawn aleatorios")]
    public bool randomSpawn = true;

    [Header("🎮 Referencias")]
    [Tooltip("Referencia al NetworkRunner (se crea automáticamente si no existe)")]
    public NetworkRunner networkRunner;

    // Singleton
    public static NetworkGameManager Instance { get; private set; }

    // Datos de jugadores desde el lobby
    private List<PlayerData> playersToSpawn = new List<PlayerData>();

    // Diccionario de jugadores spawneados
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // Índice de spawn actual
    private int currentSpawnIndex = 0;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Crear NetworkRunner si no existe
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
            networkRunner.ProvideInput = true;
        }
    }

    /// <summary>
    /// Configurar jugadores desde el lobby
    /// </summary>
    public void SetPlayersFromLobby(List<PlayerData> players)
    {
        playersToSpawn.Clear();
        playersToSpawn.AddRange(players);
        Debug.Log($"✅ {playersToSpawn.Count} jugadores configurados para spawn");
    }

    /// <summary>
    /// Iniciar sesión como Host (Shared Mode)
    /// </summary>
    public async void StartHost()
    {
        Debug.Log("🌐 Iniciando como Host (Shared Mode)...");

        // Configurar argumentos de inicio
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers
        };

        // Iniciar runner
        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("✅ Host iniciado correctamente");
        }
        else
        {
            Debug.LogError($"❌ Error al iniciar Host: {result.ShutdownReason}");
        }
    }

    /// <summary>
    /// Unirse a una sesión existente (Shared Mode)
    /// </summary>
    public async void JoinSession()
    {
        Debug.Log("🌐 Buscando sesión...");

        // Configurar argumentos de inicio
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers
        };

        // Iniciar runner
        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("✅ Unido a sesión correctamente");
        }
        else
        {
            Debug.LogError($"❌ Error al unirse: {result.ShutdownReason}");
        }
    }

    /// <summary>
    /// Cargar escena del juego y spawnear jugadores
    /// </summary>
    public void LoadGameScene()
    {
        if (networkRunner != null && networkRunner.IsRunning)
        {
            Debug.Log($"🗺️ Cargando escena del juego: {gameSceneName}");
            networkRunner.SetActiveScene(gameSceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ NetworkRunner no está activo. Inicia la sesión primero.");
        }
    }

    /// <summary>
    /// Spawnear jugador en la red
    /// </summary>
    void SpawnPlayer(PlayerRef player)
    {
        // Obtener datos del jugador
        int playerIndex = player.PlayerId % playersToSpawn.Count;
        PlayerData playerData = playersToSpawn[playerIndex];

        if (playerData.dinosaurPrefab == null)
        {
            Debug.LogError($"❌ No hay prefab asignado para el jugador {playerIndex}");
            return;
        }

        // Determinar posición de spawn
        Vector3 spawnPosition = GetSpawnPosition(playerIndex, playerData);

        // Spawnear jugador
        NetworkObject playerObject = networkRunner.Spawn(
            playerData.dinosaurPrefab,
            spawnPosition,
            Quaternion.identity,
            player
        );

        // Guardar referencia
        spawnedPlayers[player] = playerObject;

        Debug.Log($"✅ Jugador {playerData.playerName} spawneado en {spawnPosition}");

        // Configurar UI para el jugador local
        if (player == networkRunner.LocalPlayer)
        {
            ConfigureLocalPlayerUI(playerObject, playerData);
        }
    }

    /// <summary>
    /// Obtener posición de spawn
    /// </summary>
    Vector3 GetSpawnPosition(int playerIndex, PlayerData playerData)
    {
        // Si tiene posición personalizada
        if (playerData.useCustomSpawnPosition)
        {
            return playerData.spawnPosition;
        }

        // Si hay puntos de spawn configurados
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (randomSpawn)
            {
                int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
                return spawnPoints[randomIndex].position;
            }
            else
            {
                int spawnIndex = currentSpawnIndex % spawnPoints.Length;
                currentSpawnIndex++;
                return spawnPoints[spawnIndex].position;
            }
        }

        // Posición por defecto en círculo
        float angle = (playerIndex * 360f / maxPlayers) * Mathf.Deg2Rad;
        float radius = 10f;
        return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
    }

    /// <summary>
    /// Configurar UI para el jugador local
    /// </summary>
    void ConfigureLocalPlayerUI(NetworkObject playerObject, PlayerData playerData)
    {
        // Instanciar Canvas UI si está asignado
        if (playerData.playerUICanvas != null)
        {
            GameObject uiInstance = Instantiate(playerData.playerUICanvas);

            // Obtener componente PlayerUIManager
            PlayerUIManager uiManager = uiInstance.GetComponent<PlayerUIManager>();
            if (uiManager != null)
            {
                uiManager.SetupUI(playerObject.gameObject);
            }

            Debug.Log($"✅ UI configurada para jugador local");
        }
        else
        {
            Debug.LogWarning("⚠️ No hay UI Canvas asignado para este jugador");
        }
    }

    /// <summary>
    /// Despawnear jugador
    /// </summary>
    void DespawnPlayer(PlayerRef player)
    {
        if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
        {
            networkRunner.Despawn(playerObject);
            spawnedPlayers.Remove(player);
            Debug.Log($"❌ Jugador {player.PlayerId} despawneado");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // INetworkRunnerCallbacks Implementation
    // ═══════════════════════════════════════════════════════════

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"✅ Jugador {player.PlayerId} se unió");

        // Solo el host spawnea jugadores en Shared Mode
        if (runner.IsServer || runner.GameMode == GameMode.Shared)
        {
            SpawnPlayer(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"❌ Jugador {player.PlayerId} se fue");
        DespawnPlayer(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Este método se llama para recoger input del jugador
        // Puedes implementar input personalizado aquí si lo necesitas
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"🔌 Runner apagado: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("✅ Conectado al servidor");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("❌ Desconectado del servidor");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Aceptar todas las conexiones por defecto
        request.Accept();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"❌ Conexión fallida: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"📋 Lista de sesiones actualizada: {sessionList.Count} sesiones");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("✅ Escena cargada correctamente");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("🗺️ Cargando escena...");
    }
}
