using UnityEngine;

/// <summary>
/// Maneja los cuerpos muertos de dinosaurios que pueden ser comidos
/// Se sincroniza mediante el sistema de RPCs del DinosaurController
/// </summary>
public class DeadBody : MonoBehaviour
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

    [Header("🆔 Identificación")]
    [Tooltip("ID único del cuerpo para sincronización en red")]
    public string bodyID;

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

        Debug.Log($"🍖 Cuerpo muerto creado con {currentMeat} de carne. ID: {bodyID}");
    }

    void Update()
    {
        despawnTimer += Time.deltaTime;

        // Verificar si se agotó el tiempo o la carne
        if (despawnTimer >= despawnTime || currentMeat <= 0f)
        {
            Debug.Log($"🗑️ Destruyendo cuerpo muerto. Razón: {(currentMeat <= 0f ? "Carne agotada" : "Tiempo expirado")}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Consume carne del cuerpo. Retorna la cantidad consumida.
    /// Este método es llamado localmente, la sincronización se hace desde DinosaurController
    /// </summary>
    public float ConsumeMeat(float amount)
    {
        if (currentMeat <= 0f)
        {
            Debug.Log("❌ No hay más carne en este cuerpo");
            return 0f;
        }

        // Consumir la cantidad solicitada (o lo que quede)
        float consumed = Mathf.Min(amount, currentMeat);
        currentMeat -= consumed;

        Debug.Log($"🍖 Consumido {consumed} de carne. Restante: {currentMeat}");

        // Reproducir sonido de comer
        PlayEatingSound();

        return consumed;
    }

    /// <summary>
    /// Reproduce sonido de comer
    /// </summary>
    public void PlayEatingSound()
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
    /// Actualiza la cantidad de carne (llamado desde RPC para sincronizar)
    /// </summary>
    public void SetMeat(float amount)
    {
        currentMeat = amount;
    }
}