using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Sistema de llamados/rugidos con panel UI
/// Permite 4 tipos de llamados configurables: Broadcast, Friendly, Threaten, Scared
/// </summary>
public class CallSystem : MonoBehaviour
{
    [Header("📞 Referencias")]
    [Tooltip("Animator del dinosaurio")]
    public Animator animator;
    [Tooltip("AudioSource del dinosaurio")]
    public AudioSource audioSource;
    [Tooltip("Referencia al SimpleDinosaurController")]
    public SimpleDinosaurController controller;

    [Header("🖼️ UI - Panel de Llamados")]
    [Tooltip("Panel que contiene los botones de llamados (se muestra/oculta al presionar el botón principal)")]
    public GameObject callPanel;
    [Tooltip("Botón principal para abrir/cerrar el panel de llamados")]
    public Button mainCallButton;

    [Header("🔘 Botones de Llamados")]
    [Tooltip("Botón para Call 1 - Broadcast")]
    public Button call1Button;
    [Tooltip("Botón para Call 2 - Friendly")]
    public Button call2Button;
    [Tooltip("Botón para Call 3 - Threaten")]
    public Button call3Button;
    [Tooltip("Botón para Call 4 - Scared")]
    public Button call4Button;

    [Header("📞 CALL 1 - BROADCAST")]
    [Tooltip("Sonidos para el llamado de broadcast (se elige uno aleatorio)")]
    public AudioClip[] call1Sounds;
    [Tooltip("Nombre del trigger de animación para Call 1")]
    public string call1AnimationTrigger = "Call1";
    [Tooltip("Duración de la animación de Call 1")]
    public float call1Duration = 2.5f;

    [Header("📞 CALL 2 - FRIENDLY")]
    [Tooltip("Sonidos para el llamado amistoso (se elige uno aleatorio)")]
    public AudioClip[] call2Sounds;
    [Tooltip("Nombre del trigger de animación para Call 2")]
    public string call2AnimationTrigger = "Call2";
    [Tooltip("Duración de la animación de Call 2")]
    public float call2Duration = 2.0f;

    [Header("📞 CALL 3 - THREATEN")]
    [Tooltip("Sonidos para el llamado amenazante (se elige uno aleatorio)")]
    public AudioClip[] call3Sounds;
    [Tooltip("Nombre del trigger de animación para Call 3")]
    public string call3AnimationTrigger = "Call3";
    [Tooltip("Duración de la animación de Call 3")]
    public float call3Duration = 3.0f;

    [Header("📞 CALL 4 - SCARED")]
    [Tooltip("Sonidos para el llamado de miedo (se elige uno aleatorio)")]
    public AudioClip[] call4Sounds;
    [Tooltip("Nombre del trigger de animación para Call 4")]
    public string call4AnimationTrigger = "Call4";
    [Tooltip("Duración de la animación de Call 4")]
    public float call4Duration = 1.5f;

    [Header("⚙️ Configuración")]
    [Tooltip("Cerrar el panel automáticamente después de seleccionar un llamado")]
    public bool autoClosePanel = true;

    // Estado
    private bool isPanelOpen = false;
    private bool isCalling = false;
    private float callTimer = 0f;
    private int currentCallType = 0; // 0 = ninguno, 1-4 = tipo de llamado activo

    void Start()
    {
        SetupButtonListeners();

        // Ocultar panel al inicio
        if (callPanel != null)
        {
            callPanel.SetActive(false);
            isPanelOpen = false;
        }
    }

    void SetupButtonListeners()
    {
        // Botón principal para abrir/cerrar panel
        if (mainCallButton != null)
        {
            mainCallButton.onClick.RemoveAllListeners();
            mainCallButton.onClick.AddListener(ToggleCallPanel);
        }

        // Botones de llamados
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

    void Update()
    {
        // Actualizar timer del llamado
        if (isCalling)
        {
            callTimer -= Time.deltaTime;

            if (callTimer <= 0f)
            {
                EndCall();
            }
        }

        // Actualizar estado de los botones
        UpdateButtonStates();
    }

    /// <summary>
    /// Abre o cierra el panel de llamados
    /// </summary>
    public void ToggleCallPanel()
    {
        // No abrir panel si está llamando
        if (isCalling) return;

        // No abrir panel si el dinosaurio está comiendo, bebiendo o atacando
        if (controller != null)
        {
            if (controller.isAttacking || controller.IsEatingOrDrinking())
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

    /// <summary>
    /// Ejecuta un llamado específico
    /// </summary>
    /// <param name="callType">Tipo de llamado (1-4)</param>
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
            if (controller.isAttacking || controller.IsEatingOrDrinking())
            {
                Debug.LogWarning("⚠️ No se puede llamar mientras ataca, come o bebe");
                return;
            }
        }

        // Ejecutar el llamado según el tipo
        switch (callType)
        {
            case 1:
                PerformCall(call1Sounds, call1AnimationTrigger, call1Duration, "Broadcast");
                break;
            case 2:
                PerformCall(call2Sounds, call2AnimationTrigger, call2Duration, "Friendly");
                break;
            case 3:
                PerformCall(call3Sounds, call3AnimationTrigger, call3Duration, "Threaten");
                break;
            case 4:
                PerformCall(call4Sounds, call4AnimationTrigger, call4Duration, "Scared");
                break;
            default:
                Debug.LogWarning($"⚠️ Tipo de llamado inválido: {callType}");
                return;
        }

        currentCallType = callType;

        // Cerrar panel automáticamente si está configurado
        if (autoClosePanel && isPanelOpen)
        {
            ToggleCallPanel();
        }
    }

    /// <summary>
    /// Realiza el llamado (sonido + animación)
    /// </summary>
    void PerformCall(AudioClip[] sounds, string animTrigger, float duration, string callName)
    {
        isCalling = true;
        callTimer = duration;

        // Reproducir sonido
        if (audioSource != null && sounds != null && sounds.Length > 0)
        {
            AudioClip clip = sounds[UnityEngine.Random.Range(0, sounds.Length)];
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"⚠️ No hay sonidos configurados para {callName}");
        }

        // Activar animación
        if (animator != null && !string.IsNullOrEmpty(animTrigger))
        {
            animator.ResetTrigger(animTrigger);
            animator.SetTrigger(animTrigger);
        }
        else
        {
            Debug.LogWarning($"⚠️ No hay animación configurada para {callName}");
        }

        Debug.Log($"📞 Llamado {callName} ejecutado");
    }

    /// <summary>
    /// Finaliza el llamado actual
    /// </summary>
    void EndCall()
    {
        isCalling = false;
        currentCallType = 0;
        callTimer = 0f;
    }

    /// <summary>
    /// Actualiza el estado visual de los botones
    /// </summary>
    void UpdateButtonStates()
    {
        // Deshabilitar todos los botones de llamado mientras está llamando
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

        // Actualizar color del botón principal
        if (mainCallButton != null)
        {
            ColorBlock colors = mainCallButton.colors;

            if (isCalling)
            {
                colors.normalColor = Color.red; // Rojo si está llamando
            }
            else if (isPanelOpen)
            {
                colors.normalColor = Color.green; // Verde si el panel está abierto
            }
            else
            {
                colors.normalColor = Color.white; // Blanco normal
            }

            mainCallButton.colors = colors;
        }
    }

    /// <summary>
    /// Método público para verificar si está llamando
    /// </summary>
    public bool IsCalling()
    {
        return isCalling;
    }

    /// <summary>
    /// Método público para obtener el tipo de llamado actual
    /// </summary>
    public int GetCurrentCallType()
    {
        return currentCallType;
    }

    /// <summary>
    /// Método público para cerrar el panel desde el exterior
    /// </summary>
    public void ClosePanel()
    {
        if (isPanelOpen)
        {
            ToggleCallPanel();
        }
    }
}