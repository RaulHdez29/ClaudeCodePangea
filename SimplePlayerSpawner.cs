using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Spawner simple para jugadores en el mapa
/// Lee el personaje seleccionado y lo spawnea
/// IMPORTANTE: Agregar a un GameObject en la escena del mapa
/// </summary>
public class SimplePlayerSpawner : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Lista de prefabs (debe coincidir con SimpleCharacterSelector y estar en Resources)")]
    public GameObject[] characterPrefabs;

    [Tooltip("Puntos de spawn (opcional, si está vacío usa spawn aleatorio)")]
    public Transform[] spawnPoints;

    [Tooltip("Radio de spawn aleatorio si no hay spawn points")]
    public float randomSpawnRadius = 10f;

    [Tooltip("Altura de spawn")]
    public float spawnHeight = 1f;

    void Start()
    {
        // Esperar un momento para que Photon esté listo
        StartCoroutine(SpawnPlayerAfterDelay(0.5f));
    }

    IEnumerator SpawnPlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("❌ No conectado a Photon");
            yield break;
        }

        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        // Leer el personaje seleccionado de PlayerPrefs
        string prefabName = PlayerPrefs.GetString("SelectedCharacter", "");
        int selectedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError("❌ No hay personaje seleccionado");
            return;
        }

        Debug.Log($"🎮 Spawneando: {prefabName}");

        // Obtener posición de spawn
        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // Spawnear el jugador a través de Photon
        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPos, spawnRot);

        if (player != null)
        {
            Debug.Log($"✅ Jugador spawneado: {prefabName}");
        }
        else
        {
            Debug.LogError($"❌ Error al spawnear. Verifica que '{prefabName}' esté en Resources folder");
        }
    }

    /// <summary>
    /// Obtiene una posición de spawn
    /// </summary>
    Vector3 GetSpawnPosition()
    {
        // Si hay spawn points, usar uno aleatorio
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }

        // Si no, spawn aleatorio
        Vector2 randomCircle = Random.insideUnitCircle * randomSpawnRadius;
        return new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
    }

    /// <summary>
    /// Visualiza spawn points en el editor
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
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(0, spawnHeight, 0), randomSpawnRadius);
        }
    }
}
