using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de sueño para el dinosaurio - VERSIÓN FINAL COMPLETA
/// ✅ Solo permite dormir cuando está COMPLETAMENTE DETENIDO
/// ✅ NO permite dormir mientras nada
/// ✅ Cooldown de 5 segundos después de presionar el botón
/// ✅ Feedback visual y advertencias claras
/// </summary>
public class DinosaurSleepSystem : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al Animator del dinosaurio")]
    public Animator animator;
    
    [Tooltip("Referencia al script del controlador del dinosaurio")]
    public SimpleDinosaurController dinosaurController;
    
    [Tooltip("Referencia al CharacterController (para verificar velocidad)")]
    public CharacterController characterController;
    
    [Header("UI - Botón de Sueño")]
    [Tooltip("Botón para activar/desactivar el sueño")]
    public Button sleepButton;
    
    [Tooltip("Texto del botón (opcional, para cambiar entre 'Dormir' y 'Despertar')")]
    public Text sleepButtonText;
    
    [Header("UI - Botones a Desactivar Durante el Sueño")]
    [Tooltip("Se desactivarán automáticamente al dormir y reactivarán al despertar")]
    public Button[] buttonsToDisable;
    
    [Header("🚫 Validación de Movimiento")]
    [Tooltip("Velocidad mínima para considerar que está detenido")]
    [Range(0f, 0.5f)]
    public float stoppedSpeedThreshold = 0.1f;
    
    [Tooltip("Input mínimo del joystick para considerar que no hay input")]
    [Range(0f, 0.3f)]
    public float joystickDeadZone = 0.05f;
    
    [Tooltip("Mostrar advertencia en consola si intenta dormir en movimiento")]
    public bool showMovementWarnings = true;
    
    [Tooltip("Desactivar botón visualmente cuando está en movimiento")]
    public bool disableButtonWhileMoving = true;
    
    [Header("🌊 Validación de Natación")]
    [Tooltip("NO permitir dormir mientras nada")]
    public bool preventSleepWhileSwimming = true;
    
    [Header("⏱️ Cooldown del Botón")]
    [Tooltip("Tiempo de espera después de presionar el botón (segundos)")]
    [Range(0f, 10f)]
    public float buttonCooldown = 5f;
    
    [Tooltip("Mostrar cooldown en el texto del botón")]
    public bool showCooldownInText = true;
    
    [Header("⏰ Configuración de Tiempos")]
    [Tooltip("Duración de la animación de entrar a dormir")]
    public float sleepEnterDuration = 2f;
    
    [Tooltip("Duración de la animación de despertar")]
    public float sleepExitDuration = 2f;
    
    [Tooltip("Tiempo de espera antes de empezar a dormir (para que termine animaciones actuales)")]
    [Range(0f, 1f)]
    public float transitionDelay = 0.2f;
    
    [Header("🎵 Audio (Opcional)")]
    [Tooltip("Sonido al empezar a dormir")]
    public AudioClip sleepSound;
    
    [Tooltip("Sonido al despertar")]
    public AudioClip wakeSound;
    
    [Tooltip("Sonido cuando intenta dormir pero no puede")]
    public AudioClip cannotSleepSound;
    
    private AudioSource audioSource;
    
    [Header("📊 Estado Actual")]
    [Tooltip("¿Está durmiendo actualmente?")]
    public bool IsSleeping = false;
    
    [Tooltip("Estado actual del sueño:\n0 = Despierto\n1 = Entrando a dormir\n2 = Durmiendo\n3 = Despertando")]
    public int SleepState = 0;
    
    [Header("🔍 Debug Info (Solo Lectura)")]
    [Tooltip("¿Puede dormir ahora? (debug)")]
    public bool canSleepNow = true;
    
    [Tooltip("¿Está en el agua? (debug)")]
    public bool isInWater = false;
    
    [Tooltip("¿Está nadando? (debug)")]
    public bool isSwimming = false;
    
    [Tooltip("Velocidad actual del dinosaurio (debug)")]
    public float currentSpeed = 0f;
    
    [Tooltip("Input actual del joystick (debug)")]
    public float currentJoystickInput = 0f;
    
    [Tooltip("Tiempo restante de cooldown (debug)")]
    public float cooldownTimeRemaining = 0f;
    
    // Constantes de estados
    private const int STATE_AWAKE = 0;
    private const int STATE_ENTERING_SLEEP = 1;
    private const int STATE_SLEEPING = 2;
    private const int STATE_WAKING = 3;
    
    // Nombres de los parámetros del Animator
    private const string ANIM_SLEEP_ENTER = "SleepEnter";
    private const string ANIM_SLEEP_EXIT = "SleepExit";
    private const string ANIM_IS_SLEEPING = "IsSleeping";
    private const string ANIM_SLEEP_STATE = "SleepState";
    
    // Parámetros del Animator que deben resetearse
    private static readonly string[] ANIMATOR_FLOAT_PARAMS = { "Speed", "MoveX", "MoveZ", "TurnSpeed", "VerticalSpeed", "Turn", "Look" };
    private static readonly string[] ANIMATOR_BOOL_PARAMS = { "IsGrounded", "IsRunning", "IsWalking", "IsJumping", "IsAttacking", "IsCalling", "IsSwimming" };
    
    // Control de estado de botones
    private Dictionary<Button, bool> originalButtonStates = new Dictionary<Button, bool>();
    
    // Color original del botón de sueño
    private ColorBlock originalSleepButtonColors;
    private bool hasOriginalColors = false;
    
    // Control de cooldown
    private float lastButtonPressTime = -999f;
    private bool isInCooldown = false;
    
    void Start()
    {
        // Obtener componentes automáticamente si no están asignados
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (dinosaurController == null)
            dinosaurController = GetComponent<SimpleDinosaurController>();
            
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
            
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Guardar colores originales del botón de sueño
        if (sleepButton != null)
        {
            originalSleepButtonColors = sleepButton.colors;
            hasOriginalColors = true;
            sleepButton.onClick.AddListener(ToggleSleep);
        }
        
        // Auto-detectar botones si no están asignados
        if (buttonsToDisable == null || buttonsToDisable.Length == 0)
        {
            AutoDetectButtons();
        }
        
        // Guardar estados originales de botones
        SaveOriginalButtonStates();
        
        // Verificar referencias críticas
        ValidateReferences();
        
        // Inicializar estado
        UpdateAnimatorParameters();
        UpdateButtonText();
    }
    
    void Update()
    {
        // Actualizar cooldown
        UpdateCooldown();
        
        // Actualizar validación de movimiento
        if (SleepState == STATE_AWAKE)
        {
            UpdateMovementValidation();
            UpdateSwimmingStatus();
            
            // Actualizar estado del botón de dormir en tiempo real
            if (disableButtonWhileMoving && sleepButton != null)
            {
                UpdateSleepButtonState();
            }
        }
        else if (SleepState == STATE_SLEEPING)
        {
            // Actualizar botón también cuando está durmiendo (para cooldown)
            if (sleepButton != null)
            {
                UpdateSleepButtonState();
            }
        }
        
        // Actualizar texto del botón con cooldown (en cualquier estado)
        if (showCooldownInText && isInCooldown)
        {
            UpdateButtonText();
        }
        
        #if UNITY_EDITOR
        // Tecla ESC para despertar forzado (solo en editor)
        if (Input.GetKeyDown(KeyCode.Escape) && IsSleeping)
        {
            Debug.Log("🔧 [DEBUG] Despertar forzado con ESC");
            ForceWakeUp();
        }
        #endif
    }
    
    /// <summary>
    /// Actualiza el estado de cooldown del botón
    /// </summary>
    void UpdateCooldown()
    {
        if (buttonCooldown <= 0) 
        {
            isInCooldown = false;
            cooldownTimeRemaining = 0f;
            return;
        }
        
        float timeSinceLastPress = Time.time - lastButtonPressTime;
        
        if (timeSinceLastPress < buttonCooldown)
        {
            isInCooldown = true;
            cooldownTimeRemaining = buttonCooldown - timeSinceLastPress;
        }
        else
        {
            isInCooldown = false;
            cooldownTimeRemaining = 0f;
        }
    }
    
    /// <summary>
    /// Actualiza el estado de agua/natación del dinosaurio
    /// </summary>
    void UpdateSwimmingStatus()
    {
        isInWater = false;
        isSwimming = false;
        
        if (dinosaurController != null)
        {
            // ═══════════════════════════════════════════════════════════
            // 🌊 DETECCIÓN 1: IsInWater (está en contacto con agua)
            // ═══════════════════════════════════════════════════════════
            var isInWaterField = typeof(SimpleDinosaurController).GetField("isInWater");
            if (isInWaterField != null)
            {
                isInWater = (bool)isInWaterField.GetValue(dinosaurController);
            }
            
            // ═══════════════════════════════════════════════════════════
            // 🌊 DETECCIÓN 2: IsSwimming (está nadando activamente)
            // ═══════════════════════════════════════════════════════════
            var isSwimmingField = typeof(SimpleDinosaurController).GetField("isSwimming");
            if (isSwimmingField != null)
            {
                isSwimming = (bool)isSwimmingField.GetValue(dinosaurController);
            }
            
            // ═══════════════════════════════════════════════════════════
            // 🌊 VERIFICACIÓN ADICIONAL: Parámetros del Animator
            // ═══════════════════════════════════════════════════════════
            if (animator != null)
            {
                // Verificar IsInWater en el Animator
                if (HasParameter("IsInWater", AnimatorControllerParameterType.Bool))
                {
                    bool animatorInWater = animator.GetBool("IsInWater");
                    isInWater = isInWater || animatorInWater;
                }
                
                // Verificar IsSwimming en el Animator
                if (HasParameter("IsSwimming", AnimatorControllerParameterType.Bool))
                {
                    bool animatorSwimming = animator.GetBool("IsSwimming");
                    isSwimming = isSwimming || animatorSwimming;
                }
            }
        }
    }
    
    /// <summary>
    /// Actualiza la validación de si puede dormir (verifica movimiento)
    /// </summary>
    void UpdateMovementValidation()
    {
        // Verificar velocidad del CharacterController
        currentSpeed = 0f;
        if (characterController != null)
        {
            currentSpeed = characterController.velocity.magnitude;
        }
        
        // Verificar input del joystick
        currentJoystickInput = 0f;
        if (dinosaurController != null && dinosaurController.movementJoystick != null)
        {
            Vector2 joystickDir = dinosaurController.movementJoystick.Direction;
            currentJoystickInput = joystickDir.magnitude;
        }
        
        // Verificar parámetro Speed del Animator
        float animatorSpeed = 0f;
        if (animator != null && HasParameter("Speed", AnimatorControllerParameterType.Float))
        {
            animatorSpeed = animator.GetFloat("Speed");
        }
        
        // El dinosaurio está detenido si TODAS estas condiciones se cumplen:
        bool isSpeedZero = currentSpeed <= stoppedSpeedThreshold;
        bool isJoystickZero = currentJoystickInput <= joystickDeadZone;
        bool isAnimatorSpeedZero = animatorSpeed <= stoppedSpeedThreshold;
        
        // ═══════════════════════════════════════════════════════════
        // 🌊 VALIDACIÓN DE AGUA: NO puede dormir si está en agua O nadando
        // ═══════════════════════════════════════════════════════════
        bool notInWater = !preventSleepWhileSwimming || (!isInWater && !isSwimming);
        
        bool notInCooldown = !isInCooldown;
        
        canSleepNow = isSpeedZero && isJoystickZero && isAnimatorSpeedZero && notInWater && notInCooldown;
    }
    
    /// <summary>
    /// Actualiza el estado visual del botón de dormir
    /// </summary>
    void UpdateSleepButtonState()
    {
        if (sleepButton == null || !hasOriginalColors) return;
        
        if (SleepState == STATE_AWAKE)
        {
            // ═══════════════════════════════════════════════════════════
            // 🛡️ PRIORIDAD 1: COOLDOWN (siempre se respeta)
            // ═══════════════════════════════════════════════════════════
            if (isInCooldown)
            {
                sleepButton.interactable = false;
                ColorBlock colors = sleepButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gris
                sleepButton.colors = colors;
                return; // ⚠️ En cooldown, no verificar otras condiciones
            }
            
            // ═══════════════════════════════════════════════════════════
            // PRIORIDAD 2: Otras validaciones (si NO hay cooldown)
            // ═══════════════════════════════════════════════════════════
            if (!canSleepNow)
            {
                sleepButton.interactable = false;
                
                ColorBlock colors = sleepButton.colors;
                
                // 🌊 Diferentes colores según el estado del agua
                if (isSwimming)
                {
                    colors.disabledColor = new Color(0f, 0.4f, 1f, 0.6f); // Azul oscuro (nadando)
                }
                else if (isInWater)
                {
                    colors.disabledColor = new Color(0.3f, 0.7f, 1f, 0.5f); // Azul claro (en agua)
                }
                else
                {
                    colors.disabledColor = new Color(1f, 1f, 0f, 0.5f); // Amarillo (movimiento)
                }
                
                sleepButton.colors = colors;
            }
            else
            {
                // ✅ TODO OK: Puede usar el botón
                sleepButton.interactable = true;
                sleepButton.colors = originalSleepButtonColors;
            }
        }
        else if (SleepState == STATE_SLEEPING)
        {
            // ═══════════════════════════════════════════════════════════
            // 🛡️ CUANDO ESTÁ DURMIENDO: Solo puede despertar si no hay cooldown
            // ═══════════════════════════════════════════════════════════
            if (isInCooldown)
            {
                sleepButton.interactable = false;
                ColorBlock colors = sleepButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gris
                sleepButton.colors = colors;
            }
            else
            {
                // ✅ Cooldown terminado: Puede despertar
                sleepButton.interactable = true;
                sleepButton.colors = originalSleepButtonColors;
            }
        }
        else
        {
            // Durante transiciones (ENTERING_SLEEP, WAKING)
            // Mantener el botón desactivado
            sleepButton.interactable = false;
        }
    }
    
    /// <summary>
    /// Auto-detecta botones del DinosaurController
    /// </summary>
    void AutoDetectButtons()
    {
        if (dinosaurController == null) return;
        
        List<Button> detectedButtons = new List<Button>();
        
        var fields = typeof(SimpleDinosaurController).GetFields();
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(Button))
            {
                Button btn = field.GetValue(dinosaurController) as Button;
                if (btn != null && btn != sleepButton)
                {
                    detectedButtons.Add(btn);
                }
            }
        }
        
        buttonsToDisable = detectedButtons.ToArray();
        
        if (buttonsToDisable.Length > 0)
        {
            Debug.Log($"✅ SleepSystem: Auto-detectados {buttonsToDisable.Length} botones para desactivar durante el sueño");
        }
    }
    
    /// <summary>
    /// Guarda los estados originales de los botones
    /// </summary>
    void SaveOriginalButtonStates()
    {
        originalButtonStates.Clear();
        
        if (buttonsToDisable != null)
        {
            foreach (Button btn in buttonsToDisable)
            {
                if (btn != null)
                {
                    originalButtonStates[btn] = btn.interactable;
                }
            }
        }
    }
    
    /// <summary>
    /// Valida que las referencias críticas estén asignadas
    /// </summary>
    void ValidateReferences()
    {
        if (animator == null)
            Debug.LogError("⚠️ DinosaurSleepSystem: ¡Falta asignar el Animator!");
            
        if (dinosaurController == null)
            Debug.LogWarning("⚠️ DinosaurSleepSystem: No se encontró DinosaurController");
            
        if (characterController == null)
            Debug.LogWarning("⚠️ DinosaurSleepSystem: No se encontró CharacterController");
            
        if (sleepButton == null)
            Debug.LogWarning("⚠️ DinosaurSleepSystem: No hay botón de sueño asignado");
    }
    
    /// <summary>
    /// Alterna entre dormir y despertar (llamado por el botón UI)
    /// </summary>
    public void ToggleSleep()
    {
        // ═══════════════════════════════════════════════════════════
        // 🛡️ PROTECCIÓN ANTI-SPAM: Verificar cooldown PRIMERO
        // ═══════════════════════════════════════════════════════════
        if (isInCooldown)
        {
            if (showMovementWarnings)
            {
                Debug.LogWarning($"⏱️ Cooldown activo: Espera {cooldownTimeRemaining:F1}s antes de presionar de nuevo");
            }
            PlaySound(cannotSleepSound);
            StartCoroutine(FlashButton(Color.grey));
            return; // ❌ BLOQUEADO: En cooldown
        }
        
        // ═══════════════════════════════════════════════════════════
        // 🛡️ PROTECCIÓN: No permitir múltiples presiones durante transiciones
        // ═══════════════════════════════════════════════════════════
        if (SleepState == STATE_ENTERING_SLEEP || SleepState == STATE_WAKING)
        {
            if (showMovementWarnings)
            {
                Debug.LogWarning("⏱️ Espera a que termine la transición actual");
            }
            PlaySound(cannotSleepSound);
            return; // ❌ BLOQUEADO: Ya está en transición
        }
        
        // ═══════════════════════════════════════════════════════════
        // ✅ REGISTRAR TIEMPO DE PRESIÓN (ACTIVAR COOLDOWN)
        // ═══════════════════════════════════════════════════════════
        lastButtonPressTime = Time.time;
        
        // Desactivar el botón INMEDIATAMENTE para prevenir doble click
        if (sleepButton != null)
        {
            sleepButton.interactable = false;
        }
        
        // ═══════════════════════════════════════════════════════════
        // ✅ EJECUTAR ACCIÓN
        // ═══════════════════════════════════════════════════════════
        if (IsSleeping || SleepState == STATE_SLEEPING)
        {
            WakeUp();
        }
        else if (SleepState == STATE_AWAKE)
        {
            GoToSleep();
        }
    }
    
    /// <summary>
    /// Inicia el proceso de dormir (SOLO si cumple todas las validaciones)
    /// </summary>
    public void GoToSleep()
    {
        if (SleepState != STATE_AWAKE)
        {
            Debug.LogWarning("⚠️ Ya está en proceso de dormir/despertar");
            return;
        }
        
        // ═══════════════════════════════════════════════════════════
        // ✨ VALIDACIONES CRÍTICAS (el cooldown ya se verificó en ToggleSleep)
        // ═══════════════════════════════════════════════════════════
        
        UpdateMovementValidation();
        UpdateSwimmingStatus();
        
        // 1. Verificar agua/natación
        if (preventSleepWhileSwimming && (isInWater || isSwimming))
        {
            if (showMovementWarnings)
            {
                if (isSwimming)
                {
                    Debug.LogWarning("🏊 ¡No puede dormir mientras NADA!");
                    Debug.LogWarning("💡 Deja de nadar y sal del agua primero");
                }
                else if (isInWater)
                {
                    Debug.LogWarning("🌊 ¡No puede dormir mientras está EN EL AGUA!");
                    Debug.LogWarning("💡 Sal completamente del agua a tierra seca");
                }
            }
            
            PlaySound(cannotSleepSound);
            StartCoroutine(FlashButton(Color.cyan));
            
            // Reactivar el botón después del flash (para que pueda intentar de nuevo)
            StartCoroutine(ReenableButtonAfterDelay(0.6f));
            return;
        }
        
        // 2. Verificar movimiento
        if (currentSpeed > stoppedSpeedThreshold || currentJoystickInput > joystickDeadZone)
        {
            if (showMovementWarnings)
            {
                string reason = "";
                
                if (currentSpeed > stoppedSpeedThreshold)
                    reason += $" Velocidad: {currentSpeed:F2} m/s";
                    
                if (currentJoystickInput > joystickDeadZone)
                    reason += $" Joystick: {currentJoystickInput:F2}";
                
                Debug.LogWarning($"⚠️ No puede dormir mientras está en MOVIMIENTO!{reason}");
                Debug.LogWarning("💡 Suelta el joystick y espera a que se detenga completamente");
            }
            
            PlaySound(cannotSleepSound);
            StartCoroutine(FlashButton(Color.red));
            
            // Reactivar el botón después del flash (para que pueda intentar de nuevo)
            StartCoroutine(ReenableButtonAfterDelay(0.6f));
            return;
        }
        
        // ═══════════════════════════════════════════════════════════
        // ✅ TODAS LAS VALIDACIONES PASADAS: Puede dormir
        // ═══════════════════════════════════════════════════════════
        
        Debug.Log("😴 Dinosaurio va a dormir...");
        
        SleepState = STATE_ENTERING_SLEEP;
        
        if (sleepButton != null && hasOriginalColors)
        {
            sleepButton.colors = originalSleepButtonColors;
        }
        
        StartCoroutine(GoToSleepCoroutine());
    }
    
    /// <summary>
    /// Reactiva el botón después de un delay (usado cuando falla una validación)
    /// </summary>
    private IEnumerator ReenableButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Solo reactivar si está en estado AWAKE y no hay cooldown
        if (SleepState == STATE_AWAKE && !isInCooldown)
        {
            if (sleepButton != null)
            {
                sleepButton.interactable = true;
            }
        }
    }
    
    /// <summary>
    /// Efecto de parpadeo cuando intenta dormir pero no puede
    /// </summary>
    IEnumerator FlashButton(Color flashColor)
    {
        if (sleepButton == null) yield break;
        
        ColorBlock originalColors = sleepButton.colors;
        ColorBlock flashColors = sleepButton.colors;
        flashColors.normalColor = flashColor;
        flashColors.disabledColor = flashColor;
        
        for (int i = 0; i < 3; i++)
        {
            sleepButton.colors = flashColors;
            yield return new WaitForSeconds(0.1f);
            sleepButton.colors = originalColors;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// Corrutina para manejar la transición a dormir
    /// </summary>
    private IEnumerator GoToSleepCoroutine()
    {
        DisableAllButtons();
        
        if (dinosaurController != null)
        {
            dinosaurController.enabled = false;
        }
        
        yield return null;
        
        ResetAnimatorToIdle();
        
        if (transitionDelay > 0)
        {
            yield return new WaitForSeconds(transitionDelay);
        }
        
        UpdateAnimatorParameters();
        
        if (animator != null)
        {
            animator.SetTrigger(ANIM_SLEEP_ENTER);
        }
        
        PlaySound(sleepSound);
        UpdateButtonText();
        
        yield return new WaitForSeconds(sleepEnterDuration);
        
        SleepState = STATE_SLEEPING;
        IsSleeping = true;
        UpdateAnimatorParameters();
        UpdateButtonText();
        
        // ═══════════════════════════════════════════════════════════
        // 🛡️ DESPUÉS DE DORMIR: Mantener cooldown activo
        // ═══════════════════════════════════════════════════════════
        // El botón permanecerá desactivado hasta que el cooldown termine
        // UpdateSleepButtonState() lo manejará automáticamente
        
        Debug.Log("💤 Dinosaurio está durmiendo profundamente");
        Debug.Log($"⏱️ Cooldown activo: {buttonCooldown}s hasta poder despertar");
    }
    
    /// <summary>
    /// Inicia el proceso de despertar
    /// </summary>
    public void WakeUp()
    {
        if (SleepState != STATE_SLEEPING && SleepState != STATE_ENTERING_SLEEP)
        {
            Debug.LogWarning("⚠️ El dinosaurio no está durmiendo");
            return;
        }
        
        Debug.Log("🌅 Dinosaurio despertando...");
        
        StopAllCoroutines();
        StartCoroutine(WakeUpCoroutine());
    }
    
    /// <summary>
    /// Corrutina para manejar la transición a despertar
    /// </summary>
    private IEnumerator WakeUpCoroutine()
    {
        SleepState = STATE_WAKING;
        IsSleeping = false;
        UpdateAnimatorParameters();
        
        if (animator != null)
        {
            animator.SetTrigger(ANIM_SLEEP_EXIT);
        }
        
        PlaySound(wakeSound);
        UpdateButtonText();
        
        yield return new WaitForSeconds(sleepExitDuration);
        
        ResetAnimatorToIdle();
        
        SleepState = STATE_AWAKE;
        IsSleeping = false;
        UpdateAnimatorParameters();
        
        yield return null;
        
        if (dinosaurController != null)
            dinosaurController.enabled = true;
            
        EnableAllButtons();
        UpdateButtonText();
        
        // ═══════════════════════════════════════════════════════════
        // 🛡️ DESPUÉS DE DESPERTAR: El cooldown sigue activo
        // ═══════════════════════════════════════════════════════════
        // El botón permanecerá desactivado hasta que el cooldown termine
        // Update() y UpdateSleepButtonState() lo manejarán automáticamente
        
        Debug.Log("✅ Dinosaurio despierto y listo para la acción!");
        
        if (isInCooldown)
        {
            Debug.Log($"⏱️ Cooldown activo: {cooldownTimeRemaining:F1}s hasta poder dormir de nuevo");
        }
    }
    
    /// <summary>
    /// Resetea completamente el Animator al estado Idle
    /// </summary>
    private void ResetAnimatorToIdle()
    {
        if (animator == null) return;
        
        foreach (string paramName in ANIMATOR_FLOAT_PARAMS)
        {
            if (HasParameter(paramName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(paramName, 0f);
            }
        }
        
        foreach (string paramName in ANIMATOR_BOOL_PARAMS)
        {
            if (HasParameter(paramName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(paramName, false);
            }
        }
        
        string[] commonTriggers = { "Jump", "Attack", "Call", "Hit", "Death" };
        foreach (string triggerName in commonTriggers)
        {
            if (HasParameter(triggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger(triggerName);
            }
        }
    }
    
    /// <summary>
    /// Desactiva todos los botones excepto el de sueño
    /// </summary>
    private void DisableAllButtons()
    {
        if (buttonsToDisable == null) return;
        
        int disabledCount = 0;
        foreach (Button btn in buttonsToDisable)
        {
            if (btn != null && btn != sleepButton)
            {
                btn.interactable = false;
                disabledCount++;
            }
        }
        
        if (disabledCount > 0)
        {
            Debug.Log($"🔒 Desactivados {disabledCount} botones durante el sueño");
        }
    }
    
    /// <summary>
    /// Reactiva todos los botones a su estado original
    /// </summary>
    private void EnableAllButtons()
    {
        if (buttonsToDisable == null) return;
        
        int enabledCount = 0;
        foreach (Button btn in buttonsToDisable)
        {
            if (btn != null && btn != sleepButton)
            {
                if (originalButtonStates.ContainsKey(btn))
                {
                    btn.interactable = originalButtonStates[btn];
                }
                else
                {
                    btn.interactable = true;
                }
                enabledCount++;
            }
        }
        
        if (enabledCount > 0)
        {
            Debug.Log($"🔓 Reactivados {enabledCount} botones");
        }
    }
    
    /// <summary>
    /// Verifica si el Animator tiene un parámetro específico
    /// </summary>
    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Actualiza los parámetros del Animator relacionados con el sueño
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;
        
        if (HasParameter(ANIM_IS_SLEEPING, AnimatorControllerParameterType.Bool))
            animator.SetBool(ANIM_IS_SLEEPING, IsSleeping);
            
        if (HasParameter(ANIM_SLEEP_STATE, AnimatorControllerParameterType.Int))
            animator.SetInteger(ANIM_SLEEP_STATE, SleepState);
    }
    
    /// <summary>
    /// Actualiza el texto del botón según el estado
    /// </summary>
    private void UpdateButtonText()
    {
        if (sleepButtonText == null) return;
        
        switch (SleepState)
        {
            case STATE_AWAKE:
                if (isInCooldown && showCooldownInText)
                {
                    sleepButtonText.text = $"⏱️ {Mathf.CeilToInt(cooldownTimeRemaining)}s";
                }
                else if (isSwimming)
                {
                    sleepButtonText.text = "🏊 Nadando";
                }
                else if (isInWater)
                {
                    sleepButtonText.text = "🌊 En Agua";
                }
                else if (!canSleepNow)
                {
                    sleepButtonText.text = "🚫 Detente";
                }
                else
                {
                    sleepButtonText.text = "😴 Dormir";
                }
                break;
                
            case STATE_ENTERING_SLEEP:
                sleepButtonText.text = "💤 Durmiendo...";
                break;
                
            case STATE_SLEEPING:
                // 🛡️ Mostrar cooldown también cuando está durmiendo
                if (isInCooldown && showCooldownInText)
                {
                    sleepButtonText.text = $"⏱️ {Mathf.CeilToInt(cooldownTimeRemaining)}s";
                }
                else
                {
                    sleepButtonText.text = "🌅 Despertar";
                }
                break;
                
            case STATE_WAKING:
                sleepButtonText.text = "⏰ Despertando...";
                break;
        }
    }
    
    /// <summary>
    /// Reproduce un sonido (si existe)
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Método público para forzar el despertar
    /// </summary>
    public void ForceWakeUp()
    {
        if (IsSleeping || SleepState != STATE_AWAKE)
        {
            StopAllCoroutines();
            StartCoroutine(ForceWakeUpCoroutine());
        }
    }
    
    /// <summary>
    /// Despertar forzado instantáneo
    /// </summary>
    private IEnumerator ForceWakeUpCoroutine()
    {
        Debug.Log("⚡ DESPERTAR FORZADO!");
        
        SleepState = STATE_AWAKE;
        IsSleeping = false;
        
        ResetAnimatorToIdle();
        UpdateAnimatorParameters();
        
        yield return null;
        
        if (dinosaurController != null)
            dinosaurController.enabled = true;
            
        EnableAllButtons();
        UpdateButtonText();
    }
    
    /// <summary>
    /// Método público para verificar si puede realizar acciones
    /// </summary>
    public bool CanPerformActions()
    {
        return SleepState == STATE_AWAKE;
    }
    
    /// <summary>
    /// Método público para verificar si está en proceso de transición
    /// </summary>
    public bool IsTransitioning()
    {
        return SleepState == STATE_ENTERING_SLEEP || SleepState == STATE_WAKING;
    }
    
    /// <summary>
    /// Método público para verificar si puede dormir ahora
    /// </summary>
    public bool CanSleepNow()
    {
        UpdateMovementValidation();
        UpdateSwimmingStatus();
        return canSleepNow && SleepState == STATE_AWAKE;
    }
    
    void OnValidate()
    {
        if (sleepEnterDuration < 0) sleepEnterDuration = 0;
        if (sleepExitDuration < 0) sleepExitDuration = 0;
        if (transitionDelay < 0) transitionDelay = 0;
        if (buttonCooldown < 0) buttonCooldown = 0;
    }
}