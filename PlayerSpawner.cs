using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Spawnea al jugador en el mapa después de conectarse a la sala
/// Usa el personaje seleccionado en la escena de selección
/// IMPORTANTE: Agregar este script a un GameObject en la escena del mapa (GameMap)
/// </summary>
public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Configuration")]
    [Tooltip("Puntos de spawn disponibles (si está vacío, usa posiciones aleatorias)")]
    public Transform[] spawnPoints;

    [Tooltip("Usar spawn aleatorio si no hay spawn points asignados")]
    public bool useRandomSpawn = true;

    [Tooltip("Radio del spawn aleatorio (en metros)")]
    public float randomSpawnRadius = 20f;

    [Tooltip("Altura del spawn")]
    public float spawnHeight = 1f;

    [Header("Prefab por Defecto")]
    [Tooltip("Prefab por defecto si no se seleccionó ningún personaje (debe estar en Resources folder)")]
    public string defaultPrefabPath = "DinosaurPlayer";

    [Header("Debug")]
    [Tooltip("Mostrar logs de debug")]
    public bool showDebugLogs = true;

    void Start()
    {
        // Esperar un frame para asegurar que PhotonNetwork esté listo
        StartCoroutine(SpawnPlayerAfterDelay());
    }

    /// <summary>
    /// Spawnea al jugador después de un pequeño delay
    /// </summary>
    IEnumerator SpawnPlayerAfterDelay()
    {
        // Esperar un momento para que todo esté cargado
        yield return new WaitForSeconds(0.5f);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("❌ No estás conectado a Photon. No se puede spawnear.");
            yield break;
        }

        SpawnPlayer();
    }

    /// <summary>
    /// Spawnea al jugador en el mapa
    /// </summary>
    void SpawnPlayer()
    {
        // Obtener el prefab seleccionado de PlayerPrefs
        string selectedPrefabPath = PlayerPrefs.GetString("SelectedCharacterPrefab", defaultPrefabPath);
        string characterName = PlayerPrefs.GetString("SelectedCharacterName", "Player");

        if (showDebugLogs)
        {
            Debug.Log($"🎮 Spawneando jugador: {characterName}");
            Debug.Log($"📂 Prefab Path: {selectedPrefabPath}");
        }

        // Obtener posición de spawn
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();

        if (showDebugLogs)
        {
            Debug.Log($"📍 Spawn Position: {spawnPosition}");
        }

        // Spawnear el jugador a través de Photon
        GameObject player = PhotonNetwork.Instantiate(selectedPrefabPath, spawnPosition, spawnRotation);

        if (player != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"✅ Jugador spawneado: {characterName}");
                Debug.Log($"🌐 PhotonView ID: {player.GetComponent<PhotonView>().ViewID}");
            }

            // Configurar la cámara para seguir al jugador local
            ConfigureCamera(player);
        }
        else
        {
            Debug.LogError($"❌ Error al spawnear jugador. Verifica que el prefab '{selectedPrefabPath}' esté en Resources folder.");
        }
    }

    /// <summary>
    /// Obtiene una posición de spawn válida
    /// </summary>
    Vector3 GetSpawnPosition()
    {
        // Si hay spawn points asignados, usar uno aleatorio
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }

        // Si no hay spawn points, usar spawn aleatorio
        if (useRandomSpawn)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomSpawnRadius;
            return new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
        }

        // Por defecto, spawnear en el origen
        return new Vector3(0, spawnHeight, 0);
    }

    /// <summary>
    /// Obtiene la rotación de spawn (aleatoria)
    /// </summary>
    Quaternion GetSpawnRotation()
    {
        // Rotación aleatoria en el eje Y
        return Quaternion.Euler(0, Random.Range(0f, 360f), 0);
    }

    /// <summary>
    /// Configura la cámara para seguir al jugador local
    /// </summary>
    void ConfigureCamera(GameObject player)
    {
        // Verificar que sea el jugador local
        PhotonView pv = player.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine)
        {
            return; // No es nuestro jugador
        }

        // Buscar la cámara principal
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("⚠️ No se encontró la cámara principal");
            return;
        }

        // Si tienes un script de cámara que sigue al jugador, configurarlo aquí
        // Ejemplo:
        // CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        // if (cameraFollow != null)
        // {
        //     cameraFollow.target = player.transform;
        // }

        if (showDebugLogs)
        {
            Debug.Log("📷 Cámara configurada para seguir al jugador local");
        }
    }

    /// <summary>
    /// Callback: Cuando un jugador se une a la sala
    /// </summary>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (showDebugLogs)
        {
            Debug.Log($"👥 Nuevo jugador en sala: {newPlayer.NickName} (Total: {PhotonNetwork.CurrentRoom.PlayerCount})");
        }
    }

    /// <summary>
    /// Callback: Cuando un jugador sale de la sala
    /// </summary>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (showDebugLogs)
        {
            Debug.Log($"👥 Jugador salió: {otherPlayer.NickName} (Total: {PhotonNetwork.CurrentRoom.PlayerCount})");
        }
    }

    /// <summary>
    /// Visualiza los spawn points en el editor
    /// </summary>
    void OnDrawGizmos()
    {
        // Dibujar spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform sp in spawnPoints)
            {
                if (sp != null)
                {
                    Gizmos.DrawWireSphere(sp.position, 1f);
                    Gizmos.DrawLine(sp.position, sp.position + Vector3.up * 2f);
                }
            }
        }

        // Dibujar área de spawn aleatorio
        if (useRandomSpawn && (spawnPoints == null || spawnPoints.Length == 0))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(0, spawnHeight, 0), randomSpawnRadius);
        }
    }
}
