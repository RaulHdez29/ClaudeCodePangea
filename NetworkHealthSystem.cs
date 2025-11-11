using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Fusion;

/// <summary>
/// Sistema de Salud para Photon Fusion - OPTIMIZADO
/// ✅ Sincronización de vida en red
/// ✅ Sistema de combate PvP
/// ✅ Bajo tráfico de red
/// </summary>
public class NetworkHealthSystem : NetworkBehaviour
{
    [Header("Configuración de Salud")]
    [Tooltip("Vida máxima (100% = este valor)")]
    public float maxHealth = 200f;

    [Tooltip("Puede morir")]
    public bool canDie = true;

    [Tooltip("Destruir objeto al morir")]
    public bool destroyOnDeath = false; // En red, mejor no destruir automáticamente

    [Tooltip("Tiempo antes de destruir (segundos)")]
    public float destroyDelay = 5f;

    [Header("🍖 Sistema de Hambre/Sed")]
    [Tooltip("Referencia al controlador del dinosaurio")]
    public NetworkDinosaurController dinosaurController;

    [Tooltip("Daño por segundo cuando hambre = 0")]
    public float hungerDamageRate = 2f;

    [Tooltip("Daño por segundo cuando sed = 0")]
    public float thirstDamageRate = 3f;

    [Tooltip("Multiplicador de daño cuando ambos están en 0")]
    public float combinedDamageMultiplier = 2.5f;

    [Header("UI de Salud")]
    [Tooltip("Barra de vida (Image)")]
    public Image healthBar;

    [Tooltip("Canvas de la barra de vida")]
    public Canvas healthBarCanvas;

    [Tooltip("Siempre mirar a la cámara")]
    public bool billboardHealthBar = true;

    [Header("Efectos Visuales")]
    [Tooltip("Efecto de partículas al recibir daño")]
    public GameObject damageEffect;

    [Tooltip("Efecto de partículas al morir")]
    public GameObject deathEffect;

    [Header("🩸 UI de Daño Visual (Solo Local Player)")]
    [Tooltip("Imagen UI para mostrar daño visual (sangre)")]
    public Image damageOverlayImage;

    [Tooltip("Sprites de daño por nivel (índice 0 = 90%, 1 = 80%, ..., 8 = 10%)")]
    public Sprite[] damageSprites = new Sprite[9];

    private int currentDamageLevel = -1;

    [Header("💀 UI de Muerte (Solo Local Player)")]
    [Tooltip("Panel que se muestra cuando el dinosaurio muere")]
    public GameObject deathPanel;

    [Header("Audio")]
    public AudioClip[] hurtSounds;
    public AudioClip[] deathSounds;
    public AudioSource audioSource;

    [Header("Eventos")]
    public UnityEvent onDamageTaken;
    public UnityEvent onDeath;
    public UnityEvent onHealthChanged;

    // ═══════════════════════════════════════════════════════════
    // 🌐 VARIABLES DE RED - OPTIMIZADAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Vida actual sincronizada (0-200)
    /// ⚡ OPTIMIZACIÓN: Usamos OnChanged para solo actualizar UI cuando cambia
    /// </summary>
    [Networked(OnChanged = nameof(OnHealthChanged))]
    public float CurrentHealth { get; set; }

    /// <summary>
    /// Estado de muerte sincronizado
    /// ⚡ OPTIMIZACIÓN: Bool solo ocupa 1 bit
    /// </summary>
    [Networked(OnChanged = nameof(OnDeathStateChanged))]
    public NetworkBool IsDead { get; set; }

    /// <summary>
    /// Estado de invulnerabilidad
    /// </summary>
    [Networked]
    public NetworkBool IsInvulnerable { get; set; }

    // Variables locales
    private Camera mainCamera;
    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        // Inicializar solo una vez al spawn
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
            IsInvulnerable = false;
        }

        // Obtener referencia al controlador del dinosaurio si no está asignada
        if (dinosaurController == null)
            dinosaurController = GetComponent<NetworkDinosaurController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        mainCamera = Camera.main;

        // Solo ocultar UI para el jugador local
        if (Object.HasInputAuthority)
        {
            // Ocultar imagen de daño al inicio
            if (damageOverlayImage != null)
            {
                damageOverlayImage.gameObject.SetActive(false);
            }

            // Ocultar panel de muerte al inicio
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }
        }

        UpdateHealthBar();

        // Inicializar change detector
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void FixedUpdateNetwork()
    {
        // Solo el servidor procesa daño por hambre/sed
        if (Object.HasStateAuthority && !IsDead)
        {
            UpdateHungerThirstDamage();
        }
    }

    public override void Render()
    {
        // Billboard de la barra de vida (todos los clientes)
        if (billboardHealthBar && healthBarCanvas != null && mainCamera != null)
        {
            healthBarCanvas.transform.LookAt(healthBarCanvas.transform.position + mainCamera.transform.forward);
        }

        // Actualizar UI de daño visual (solo para el jugador local)
        if (Object.HasInputAuthority)
        {
            UpdateDamageOverlay();
        }

        // Detectar cambios en propiedades Networked
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):
                    UpdateHealthBar();
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🗡️ SISTEMA DE DAÑO EN RED - PvP
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Recibir daño (llamado localmente, ejecutado en servidor via RPC)
    /// </summary>
    public void TakeDamage(float damage, PlayerRef attacker = default)
    {
        if (IsDead || IsInvulnerable || damage <= 0) return;

        // Enviar al servidor para procesar
        RPC_TakeDamage(damage, attacker);
    }

    /// <summary>
    /// RPC para aplicar daño en el servidor
    /// ⚡ OPTIMIZACIÓN: Solo el servidor tiene autoridad sobre la vida
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TakeDamage(float damage, PlayerRef attacker)
    {
        if (IsDead || IsInvulnerable || damage <= 0) return;

        // Aplicar daño (esto se sincroniza automáticamente vía [Networked])
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        Debug.Log($"{gameObject.name} recibió {damage} de daño de {attacker}. Vida restante: {CurrentHealth}/{maxHealth}");

        // Notificar a todos los clientes para efectos visuales/sonoros
        RPC_OnDamageTaken(damage, attacker);

        // Verificar muerte
        if (CurrentHealth <= 0 && canDie && !IsDead)
        {
            IsDead = true;
            RPC_OnDeath();
        }
    }

    /// <summary>
    /// RPC para efectos visuales/sonoros de daño (todos los clientes)
    /// ⚡ OPTIMIZACIÓN: Efectos locales, no necesitan autoridad
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDamageTaken(float damage, PlayerRef attacker)
    {
        // Eventos locales
        onDamageTaken?.Invoke();
        onHealthChanged?.Invoke();

        // Efectos visuales
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        // Sonido
        if (hurtSounds != null && hurtSounds.Length > 0 && audioSource != null)
        {
            AudioClip hurtClip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(hurtClip);
        }
    }

    /// <summary>
    /// Curar vida (solo servidor)
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;

        if (Object.HasStateAuthority)
        {
            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            Debug.Log($"{gameObject.name} curado por {amount}. Vida: {CurrentHealth}/{maxHealth}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 💀 SISTEMA DE MUERTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// RPC de muerte (ejecutado en todos los clientes)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"{gameObject.name} ha muerto!");

        // Evento de muerte
        onDeath?.Invoke();

        // Llamar al método Die() del NetworkDinosaurController
        if (dinosaurController != null)
        {
            dinosaurController.Die();
        }

        // Activar panel de muerte (solo para el jugador local)
        if (Object.HasInputAuthority && deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        // Efecto de muerte
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Sonido de muerte
        if (deathSounds != null && deathSounds.Length > 0 && audioSource != null)
        {
            AudioClip deathClip = deathSounds[Random.Range(0, deathSounds.Length)];
            audioSource.PlayOneShot(deathClip);
        }

        // Ocultar barra de vida
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }

        // Destruir objeto (solo en el servidor)
        if (destroyOnDeath && Object.HasStateAuthority)
        {
            Runner.Despawn(Object, destroyDelay);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 📊 CALLBACKS DE CAMBIO DE ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Callback cuando cambia la vida (sincronización automática)
    /// </summary>
    public static void OnHealthChanged(Changed<NetworkHealthSystem> changed)
    {
        changed.Behaviour.UpdateHealthBar();
        changed.Behaviour.onHealthChanged?.Invoke();
    }

    /// <summary>
    /// Callback cuando cambia el estado de muerte
    /// </summary>
    public static void OnDeathStateChanged(Changed<NetworkHealthSystem> changed)
    {
        if (changed.Behaviour.IsDead)
        {
            // Efectos adicionales si es necesario
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🍖 SISTEMA DE HAMBRE/SED (Solo Servidor)
    // ═══════════════════════════════════════════════════════════

    void UpdateHungerThirstDamage()
    {
        if (IsDead || dinosaurController == null) return;

        float hunger = dinosaurController.CurrentHunger;
        float thirst = dinosaurController.CurrentThirst;

        bool isHungry = hunger <= 0f;
        bool isThirsty = thirst <= 0f;

        // Si ambos están en 0, aplicar daño acelerado
        if (isHungry && isThirsty)
        {
            float combinedDamage = (hungerDamageRate + thirstDamageRate) * combinedDamageMultiplier * Runner.DeltaTime;
            CurrentHealth -= combinedDamage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        }
        // Si solo hambre está en 0
        else if (isHungry)
        {
            float damage = hungerDamageRate * Runner.DeltaTime;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        }
        // Si solo sed está en 0
        else if (isThirsty)
        {
            float damage = thirstDamageRate * Runner.DeltaTime;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        }

        // Verificar muerte
        if (CurrentHealth <= 0 && canDie && !IsDead)
        {
            IsDead = true;
            RPC_OnDeath();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🎨 UI Y VISUALES
    // ═══════════════════════════════════════════════════════════

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = CurrentHealth / maxHealth;
        }
    }

    void UpdateDamageOverlay()
    {
        if (damageOverlayImage == null || damageSprites == null || damageSprites.Length != 9)
            return;

        // Calcular porcentaje de vida
        float healthPercentage = (CurrentHealth / maxHealth) * 100f;

        int newDamageLevel = -1;

        // Determinar el nivel de daño
        if (healthPercentage <= 90f && healthPercentage > 80f)
            newDamageLevel = 0;
        else if (healthPercentage <= 80f && healthPercentage > 70f)
            newDamageLevel = 1;
        else if (healthPercentage <= 70f && healthPercentage > 60f)
            newDamageLevel = 2;
        else if (healthPercentage <= 60f && healthPercentage > 50f)
            newDamageLevel = 3;
        else if (healthPercentage <= 50f && healthPercentage > 40f)
            newDamageLevel = 4;
        else if (healthPercentage <= 40f && healthPercentage > 30f)
            newDamageLevel = 5;
        else if (healthPercentage <= 30f && healthPercentage > 20f)
            newDamageLevel = 6;
        else if (healthPercentage <= 20f && healthPercentage > 10f)
            newDamageLevel = 7;
        else if (healthPercentage <= 10f && healthPercentage > 0f)
            newDamageLevel = 8;

        // Solo actualizar si cambió el nivel
        if (newDamageLevel != currentDamageLevel)
        {
            currentDamageLevel = newDamageLevel;

            if (newDamageLevel == -1)
            {
                damageOverlayImage.gameObject.SetActive(false);
            }
            else
            {
                damageOverlayImage.gameObject.SetActive(true);
                if (damageSprites[newDamageLevel] != null)
                {
                    damageOverlayImage.sprite = damageSprites[newDamageLevel];
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🛡️ UTILIDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════

    public void MakeInvulnerable(float duration)
    {
        if (Object.HasStateAuthority)
        {
            IsInvulnerable = true;
            Runner.StartCoroutine(InvulnerabilityCoroutine(duration));
        }
    }

    private System.Collections.IEnumerator InvulnerabilityCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (Object != null && Object.HasStateAuthority)
        {
            IsInvulnerable = false;
        }
    }

    public float GetHealthPercentage()
    {
        return CurrentHealth / maxHealth;
    }

    public bool IsAlive()
    {
        return !IsDead && CurrentHealth > 0;
    }

    public bool IsFullHealth()
    {
        return CurrentHealth >= maxHealth;
    }
}
