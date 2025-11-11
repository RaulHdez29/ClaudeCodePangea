using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections;

/// <summary>
/// Sistema de Sueño para Photon Fusion - OPTIMIZADO
/// ✅ Sincronización de estado de sueño
/// ✅ Animaciones visibles para todos los jugadores
/// ✅ Bajo tráfico de red
/// </summary>
public class NetworkSleepSystem : NetworkBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public NetworkDinosaurController dinosaurController;
    public CharacterController characterController;
    public NetworkHealthSystem healthSystem;

    [Header("UI - Botón de Sueño (Solo Local)")]
    public Button sleepButton;
    public Text sleepButtonText;

    [Header("UI - Botones a Desactivar Durante el Sueño")]
    public Button[] buttonsToDisable;

    [Header("⏰ Configuración de Tiempos")]
    public float sleepEnterDuration = 2f;
    public float sleepExitDuration = 2f;

    [Header("❤️ Configuración de Regeneración")]
    public bool regenerateHealthWhileSleeping = true;
    public float healthRegenRate = 5f;
    public bool requireHungerForHealthRegen = true;
    public bool requireThirstForHealthRegen = true;

    [Header("🎵 Audio")]
    public AudioClip sleepSound;
    public AudioClip wakeSound;

    private AudioSource audioSource;

    // ═══════════════════════════════════════════════════════════
    // 🌐 VARIABLES DE RED
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estado de sueño sincronizado
    /// 0 = Despierto, 1 = Entrando a dormir, 2 = Durmiendo, 3 = Despertando
    /// ⚡ OPTIMIZACIÓN: byte = 1 byte
    /// </summary>
    [Networked(OnChanged = nameof(OnSleepStateChanged))]
    public byte SleepState { get; set; }

    /// <summary>
    /// Tick cuando cambió el estado de sueño
    /// </summary>
    [Networked]
    public int SleepStateChangeTick { get; set; }

    // Constantes
    private const byte STATE_AWAKE = 0;
    private const byte STATE_ENTERING_SLEEP = 1;
    private const byte STATE_SLEEPING = 2;
    private const byte STATE_WAKING = 3;

    // Estado local
    private bool isPanelSetup = false;

    public bool IsSleeping => SleepState == STATE_SLEEPING;

    public override void Spawned()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (dinosaurController == null)
            dinosaurController = GetComponent<NetworkDinosaurController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (healthSystem == null)
            healthSystem = GetComponent<NetworkHealthSystem>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Inicializar estado
        if (Object.HasStateAuthority)
        {
            SleepState = STATE_AWAKE;
        }

        // Solo el jugador local configura UI
        if (Object.HasInputAuthority)
        {
            SetupUI();
        }
        else
        {
            // Desactivar UI para otros jugadores
            if (sleepButton != null)
                sleepButton.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Solo el servidor maneja regeneración
        if (Object.HasStateAuthority && SleepState == STATE_SLEEPING)
        {
            RegenerateWhileSleeping();
        }

        // Verificar transiciones automáticas
        if (Object.HasStateAuthority)
        {
            CheckStateTransitions();
        }
    }

    public override void Render()
    {
        // Solo el jugador local actualiza UI
        if (Object.HasInputAuthority)
        {
            UpdateButtonText();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🎮 UI Y CONTROLES (Solo Jugador Local)
    // ═══════════════════════════════════════════════════════════

    void SetupUI()
    {
        if (isPanelSetup) return;

        if (sleepButton != null)
        {
            sleepButton.onClick.RemoveAllListeners();
            sleepButton.onClick.AddListener(ToggleSleep);
        }

        UpdateButtonText();
        isPanelSetup = true;
    }

    public void ToggleSleep()
    {
        // Solo el jugador local puede controlar su sueño
        if (!Object.HasInputAuthority) return;

        if (SleepState == STATE_ENTERING_SLEEP || SleepState == STATE_WAKING)
        {
            Debug.LogWarning("⏱️ Espera a que termine la transición actual");
            return;
        }

        if (SleepState == STATE_SLEEPING || SleepState == STATE_ENTERING_SLEEP)
        {
            // Despertar
            RPC_WakeUp();
        }
        else if (SleepState == STATE_AWAKE)
        {
            // Dormir (validaciones en el servidor)
            RPC_GoToSleep();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🌐 RPCs DE RED
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// RPC para iniciar el sueño (con validaciones en servidor)
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_GoToSleep()
    {
        if (SleepState != STATE_AWAKE) return;

        // Validaciones del servidor
        if (dinosaurController != null)
        {
            if (dinosaurController.IsEatingOrDrinking())
            {
                Debug.LogWarning("🍖 No puede dormir mientras come o bebe");
                return;
            }

            if (dinosaurController.IsInWater || dinosaurController.IsSwimming)
            {
                Debug.LogWarning("🌊 No puede dormir en el agua");
                return;
            }
        }

        // Iniciar transición
        SleepState = STATE_ENTERING_SLEEP;
        SleepStateChangeTick = Runner.Tick;

        Debug.Log("😴 Iniciando sueño...");

        // Notificar a todos los clientes
        RPC_OnEnterSleep();
    }

    /// <summary>
    /// RPC para despertar
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_WakeUp()
    {
        if (SleepState != STATE_SLEEPING && SleepState != STATE_ENTERING_SLEEP) return;

        SleepState = STATE_WAKING;
        SleepStateChangeTick = Runner.Tick;

        Debug.Log("🌅 Despertando...");

        // Notificar a todos los clientes
        RPC_OnWakeUp();
    }

    /// <summary>
    /// RPC ejecutado en todos los clientes al entrar a dormir
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnEnterSleep()
    {
        // Animación
        if (animator != null)
        {
            // Resetear parámetros críticos
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveZ", 0f);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsAttacking", false);

            animator.SetTrigger("SleepEnter");
            animator.SetBool("IsSleeping", true);
        }

        // Sonido
        PlaySound(sleepSound);

        // Desactivar botones (solo jugador local)
        if (Object.HasInputAuthority)
        {
            DisableAllButtons();
        }

        // Desactivar movimiento (solo jugador local)
        if (Object.HasInputAuthority && dinosaurController != null)
        {
            dinosaurController.enabled = false;
        }
    }

    /// <summary>
    /// RPC ejecutado en todos los clientes al despertar
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnWakeUp()
    {
        // Animación
        if (animator != null)
        {
            animator.SetTrigger("SleepExit");
            animator.SetBool("IsSleeping", false);
        }

        // Sonido
        PlaySound(wakeSound);

        // Reactivar botones (solo jugador local)
        if (Object.HasInputAuthority)
        {
            EnableAllButtons();
        }

        // Reactivar movimiento (solo jugador local)
        if (Object.HasInputAuthority && dinosaurController != null)
        {
            dinosaurController.enabled = true;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ⚙️ LÓGICA DE ESTADO (Solo Servidor)
    // ═══════════════════════════════════════════════════════════

    void CheckStateTransitions()
    {
        int ticksSinceChange = Runner.Tick - SleepStateChangeTick;
        float timeSinceChange = ticksSinceChange * Runner.DeltaTime;

        switch (SleepState)
        {
            case STATE_ENTERING_SLEEP:
                if (timeSinceChange >= sleepEnterDuration)
                {
                    SleepState = STATE_SLEEPING;
                    SleepStateChangeTick = Runner.Tick;
                    Debug.Log("💤 Durmiendo profundamente");
                }
                break;

            case STATE_WAKING:
                if (timeSinceChange >= sleepExitDuration)
                {
                    SleepState = STATE_AWAKE;
                    SleepStateChangeTick = Runner.Tick;
                    Debug.Log("✅ Despierto!");
                }
                break;
        }
    }

    void RegenerateWhileSleeping()
    {
        if (dinosaurController == null) return;

        // Regenerar estamina
        dinosaurController.CurrentStamina += dinosaurController.staminaSleepRegenRate * Runner.DeltaTime;
        dinosaurController.CurrentStamina = Mathf.Clamp(dinosaurController.CurrentStamina, 0f, dinosaurController.maxStamina);

        // Regenerar vida (si cumple requisitos)
        if (regenerateHealthWhileSleeping && healthSystem != null && !healthSystem.IsDead)
        {
            bool canRegenerateHealth = true;

            if (requireHungerForHealthRegen && dinosaurController.CurrentHunger <= 0f)
            {
                canRegenerateHealth = false;
            }

            if (requireThirstForHealthRegen && dinosaurController.CurrentThirst <= 0f)
            {
                canRegenerateHealth = false;
            }

            if (canRegenerateHealth)
            {
                healthSystem.Heal(healthRegenRate * Runner.DeltaTime);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 📊 CALLBACKS DE CAMBIO DE ESTADO
    // ═══════════════════════════════════════════════════════════

    public static void OnSleepStateChanged(Changed<NetworkSleepSystem> changed)
    {
        // Actualizar UI cuando cambia el estado
        if (changed.Behaviour.Object.HasInputAuthority)
        {
            changed.Behaviour.UpdateButtonText();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🛠️ UTILIDADES
    // ═══════════════════════════════════════════════════════════

    void DisableAllButtons()
    {
        if (buttonsToDisable == null) return;

        foreach (Button btn in buttonsToDisable)
        {
            if (btn != null && btn != sleepButton)
            {
                btn.interactable = false;
            }
        }
    }

    void EnableAllButtons()
    {
        if (buttonsToDisable == null) return;

        foreach (Button btn in buttonsToDisable)
        {
            if (btn != null && btn != sleepButton)
            {
                btn.interactable = true;
            }
        }
    }

    void UpdateButtonText()
    {
        if (sleepButtonText == null) return;

        switch (SleepState)
        {
            case STATE_AWAKE:
                sleepButtonText.text = "😴 Dormir";
                break;
            case STATE_ENTERING_SLEEP:
                sleepButtonText.text = "💤 Durmiendo...";
                break;
            case STATE_SLEEPING:
                sleepButtonText.text = "🌅 Despertar";
                break;
            case STATE_WAKING:
                sleepButtonText.text = "⏰ Despertando...";
                break;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public bool CanPerformActions()
    {
        return SleepState == STATE_AWAKE;
    }

    public bool IsTransitioning()
    {
        return SleepState == STATE_ENTERING_SLEEP || SleepState == STATE_WAKING;
    }
}
