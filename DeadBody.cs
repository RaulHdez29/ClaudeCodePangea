using UnityEngine;
using Photon.Pun;

/// <summary>
/// Maneja los cuerpos muertos de dinosaurios que pueden ser comidos
/// Se sincroniza por red para que todos los jugadores vean el mismo estado
/// </summary>
public class DeadBody : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("🍖 Configuración de Carne")]
    [Tooltip("Cantidad inicial de carne en el cuerpo")]
    public float meatAmount = 500f;

    [Tooltip("Tiempo en segundos antes de que el cuerpo desaparezca (default: 5 minutos)")]
    public float despawnTime = 300f; // 5 minutos

    [Header("🔊 Audio")]
    [Tooltip("Sonido cuando se come carne del cuerpo")]
    public AudioClip[] eatingSounds;

    [Header("📊 Estado Actual")]
    [Tooltip("Carne restante en el cuerpo")]
    public float currentMeat;

    private float despawnTimer = 0f;
    private AudioSource audioSource;

    void Start()
    {
        // Inicializar cantidad de carne
        currentMeat = meatAmount;

        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 20f;
        }

        Debug.Log($"🍖 Cuerpo muerto creado con {currentMeat} de carne. Se destruirá en {despawnTime} segundos.");
    }

    void Update()
    {
        // Solo el master client maneja el timer de destrucción
        if (PhotonNetwork.IsMasterClient)
        {
            despawnTimer += Time.deltaTime;

            // Verificar si se agotó el tiempo o la carne
            if (despawnTimer >= despawnTime || currentMeat <= 0f)
            {
                Debug.Log($"🗑️ Destruyendo cuerpo muerto. Razón: {(currentMeat <= 0f ? "Carne agotada" : "Tiempo expirado")}");
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Consume carne del cuerpo. Retorna la cantidad consumida.
    /// </summary>
    public float ConsumeMeat(float amount)
    {
        // Solo el dueño (master client) puede modificar la carne
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("⚠️ Solo el Master Client puede consumir carne del cuerpo");
            return 0f;
        }

        if (currentMeat <= 0f)
        {
            Debug.Log("❌ No hay más carne en este cuerpo");
            return 0f;
        }

        // Consumir la cantidad solicitada (o lo que quede)
        float consumed = Mathf.Min(amount, currentMeat);
        currentMeat -= consumed;

        Debug.Log($"🍖 Consumido {consumed} de carne. Restante: {currentMeat}");

        // Reproducir sonido de comer (en todos los clientes)
        photonView.RPC("RPC_PlayEatingSound", RpcTarget.All);

        // Si se agotó la carne, marcar para destrucción en el próximo Update
        if (currentMeat <= 0f)
        {
            Debug.Log("💀 Carne agotada, el cuerpo será destruido");
        }

        return consumed;
    }

    [PunRPC]
    void RPC_PlayEatingSound()
    {
        if (eatingSounds != null && eatingSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = eatingSounds[Random.Range(0, eatingSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    /// <summary>
    /// Verifica si hay carne disponible
    /// </summary>
    public bool HasMeat()
    {
        return currentMeat > 0f;
    }

    /// <summary>
    /// Obtiene el porcentaje de carne restante (0-1)
    /// </summary>
    public float GetMeatPercentage()
    {
        return currentMeat / meatAmount;
    }

    /// <summary>
    /// Sincronización de Photon - envía/recibe el estado de la carne
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Enviar datos (solo el master client escribe)
            stream.SendNext(currentMeat);
            stream.SendNext(despawnTimer);
        }
        else
        {
            // Recibir datos (otros clientes leen)
            currentMeat = (float)stream.ReceiveNext();
            despawnTimer = (float)stream.ReceiveNext();
        }
    }
}
