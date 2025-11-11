using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// Sistema de Llamados/Rugidos para Photon Fusion - OPTIMIZADO
/// ✅ Sincronización de animaciones de rugido
/// ✅ Audio reproducido en todos los clientes
/// ✅ Bajo tráfico de red (solo RPCs)
/// </summary>
public class NetworkCallSystem : NetworkBehaviour
{
    [Header("📞 Referencias")]
    [Tooltip("Animator del dinosaurio")]
    public Animator animator;

    [Tooltip("AudioSource del dinosaurio")]
    public AudioSource audioSource;

    [Tooltip("Referencia al NetworkDinosaurController")]
    public NetworkDinosaurController controller;

    [Header("🖼️ UI - Panel de Llamados")]
    [Tooltip("Panel que contiene los botones de llamados")]
    public GameObject callPanel;

    [Tooltip("Botón principal para abrir/cerrar el panel")]
    public Button mainCallButton;

    [Header("🔘 Botones de Llamados")]
    public Button call1Button;
    public Button call2Button;
    public Button call3Button;
    public Button call4Button;

    [Header("📞 CALL 1 - BROADCAST")]
    public AudioClip[] call1Sounds;
    public string call1AnimationTrigger = "Call1";
    public float call1Duration = 2.5f;

    [Header("📞 CALL 2 - FRIENDLY")]
    public AudioClip[] call2Sounds;
    public string call2AnimationTrigger = "Call2";
    public float call2Duration = 2.0f;

    [Header("📞 CALL 3 - THREATEN")]
    public AudioClip[] call3Sounds;
    public string call3AnimationTrigger = "Call3";
    public float call3Duration = 3.0f;

    [Header("📞 CALL 4 - SCARED")]
    public AudioClip[] call4Sounds;
    public string call4AnimationTrigger = "Call4";
    public float call4Duration = 1.5f;

    [Header("⚙️ Configuración")]
    public bool autoClosePanel = true;

    // ═══════════════════════════════════════════════════════════
    // 🌐 VARIABLES DE RED
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estado de llamado actual (0 = ninguno, 1-4 = tipo de llamado)
    /// ⚡ OPTIMIZACIÓN: byte = 1 byte
    /// </summary>
    [Networked]
    public byte CurrentCallType { get; set; }

    /// <summary>
    /// Tick cuando empezó el llamado (para calcular duración)
    /// </summary>
    [Networked]
    public int CallStartTick { get; set; }

    // Estado local
    private bool isPanelOpen = false;
    private bool isCalling = false;

    public override void Spawned()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (controller == null)
            controller = GetComponent<NetworkDinosaurController>();

        // Solo el jugador local configura la UI
        if (Object.HasInputAuthority)
        {
            SetupButtonListeners();

            if (callPanel != null)
            {
                callPanel.SetActive(false);
                isPanelOpen = false;
            }
        }
        else
        {
            // Desactivar UI para otros jugadores
            if (callPanel != null)
                callPanel.SetActive(false);
            if (mainCallButton != null)
                mainCallButton.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Verificar si el llamado ha terminado
        if (CurrentCallType > 0)
        {
            int ticksSinceCalling = Runner.Tick - CallStartTick;
            float timeSinceCalling = ticksSinceCalling * Runner.DeltaTime;

            float callDuration = GetCallDuration(CurrentCallType);

            if (timeSinceCalling >= callDuration)
            {
                // Terminar llamado
                if (Object.HasStateAuthority)
                {
                    CurrentCallType = 0;
                }
            }
        }
    }

    public override void Render()
    {
        // Actualizar estado local basado en estado de red
        isCalling = CurrentCallType > 0;

        // Solo el jugador local actualiza la UI
        if (Object.HasInputAuthority)
        {
            UpdateButtonStates();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🎮 UI Y CONTROLES (Solo Jugador Local)
    // ═══════════════════════════════════════════════════════════

    void SetupButtonListeners()
    {
        if (mainCallButton != null)
        {
            mainCallButton.onClick.RemoveAllListeners();
            mainCallButton.onClick.AddListener(ToggleCallPanel);
        }

        if (call1Button != null)
        {
            call1Button.onClick.RemoveAllListeners();
            call1Button.onClick.AddListener(() => ExecuteCall(1));
        }

        if (call2Button != null)
        {
            call2Button.onClick.RemoveAllListeners();
            call2Button.onClick.AddListener(() => ExecuteCall(2));
        }

        if (call3Button != null)
        {
            call3Button.onClick.RemoveAllListeners();
            call3Button.onClick.AddListener(() => ExecuteCall(3));
        }

        if (call4Button != null)
        {
            call4Button.onClick.RemoveAllListeners();
            call4Button.onClick.AddListener(() => ExecuteCall(4));
        }
    }

    public void ToggleCallPanel()
    {
        if (isCalling) return;

        if (controller != null)
        {
            if (controller.IsAttacking || controller.IsEatingOrDrinking())
            {
                return;
            }
        }

        isPanelOpen = !isPanelOpen;

        if (callPanel != null)
        {
            callPanel.SetActive(isPanelOpen);
        }

        Debug.Log(isPanelOpen ? "📞 Panel de llamados abierto" : "📞 Panel de llamados cerrado");
    }

    public void ExecuteCall(int callType)
    {
        // Validar que no esté ya llamando
        if (isCalling)
        {
            Debug.LogWarning("⚠️ Ya está realizando un llamado");
            return;
        }

        // Validar que el controlador no esté ocupado
        if (controller != null)
        {
            if (controller.IsAttacking || controller.IsEatingOrDrinking())
            {
                Debug.LogWarning("⚠️ No se puede llamar mientras ataca, come o bebe");
                return;
            }
        }

        // Cerrar panel automáticamente
        if (autoClosePanel && isPanelOpen && callPanel != null)
        {
            callPanel.SetActive(false);
            isPanelOpen = false;
            Debug.Log("📞 Panel de llamados cerrado automáticamente");
        }

        // Enviar RPC al servidor para ejecutar el llamado en red
        RPC_PerformCall(callType);
    }

    // ═══════════════════════════════════════════════════════════
    // 🌐 RPCs DE RED
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// RPC para iniciar un llamado (enviado al servidor)
    /// ⚡ OPTIMIZACIÓN: Solo 1 byte de data (callType)
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformCall(byte callType)
    {
        // Validar tipo de llamado
        if (callType < 1 || callType > 4) return;

        // Actualizar estado de red
        CurrentCallType = callType;
        CallStartTick = Runner.Tick;

        // Ejecutar en todos los clientes
        RPC_PlayCallAnimation(callType);
    }

    /// <summary>
    /// RPC para reproducir animación y sonido en todos los clientes
    /// ⚡ OPTIMIZACIÓN: Solo 1 byte de data
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCallAnimation(byte callType)
    {
        string animTrigger = "";
        AudioClip[] sounds = null;
        string callName = "";

        switch (callType)
        {
            case 1:
                animTrigger = call1AnimationTrigger;
                sounds = call1Sounds;
                callName = "Broadcast";
                break;
            case 2:
                animTrigger = call2AnimationTrigger;
                sounds = call2Sounds;
                callName = "Friendly";
                break;
            case 3:
                animTrigger = call3AnimationTrigger;
                sounds = call3Sounds;
                callName = "Threaten";
                break;
            case 4:
                animTrigger = call4AnimationTrigger;
                sounds = call4Sounds;
                callName = "Scared";
                break;
        }

        // Reproducir animación
        if (animator != null && !string.IsNullOrEmpty(animTrigger))
        {
            animator.ResetTrigger(animTrigger);
            animator.SetTrigger(animTrigger);
        }

        // Reproducir sonido
        if (audioSource != null && sounds != null && sounds.Length > 0)
        {
            AudioClip clip = sounds[Random.Range(0, sounds.Length)];
            audioSource.PlayOneShot(clip);
        }

        Debug.Log($"📞 Llamado {callName} ejecutado");
    }

    // ═══════════════════════════════════════════════════════════
    // 🛠️ UTILIDADES
    // ═══════════════════════════════════════════════════════════

    void UpdateButtonStates()
    {
        bool canCall = !isCalling;

        if (call1Button != null)
        {
            call1Button.interactable = canCall;
        }

        if (call2Button != null)
        {
            call2Button.interactable = canCall;
        }

        if (call3Button != null)
        {
            call3Button.interactable = canCall;
        }

        if (call4Button != null)
        {
            call4Button.interactable = canCall;
        }

        if (mainCallButton != null)
        {
            ColorBlock colors = mainCallButton.colors;

            if (isCalling)
            {
                colors.normalColor = Color.red;
            }
            else if (isPanelOpen)
            {
                colors.normalColor = Color.green;
            }
            else
            {
                colors.normalColor = Color.white;
            }

            mainCallButton.colors = colors;
        }
    }

    float GetCallDuration(byte callType)
    {
        switch (callType)
        {
            case 1: return call1Duration;
            case 2: return call2Duration;
            case 3: return call3Duration;
            case 4: return call4Duration;
            default: return 2.5f;
        }
    }

    public bool IsCalling()
    {
        return isCalling;
    }

    public int GetCurrentCallType()
    {
        return CurrentCallType;
    }

    public void ClosePanel()
    {
        if (isPanelOpen && Object.HasInputAuthority)
        {
            ToggleCallPanel();
        }
    }
}
