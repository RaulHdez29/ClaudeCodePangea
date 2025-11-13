using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

// VERSIÓN CORREGIDA - FIX PARA PROBLEMA DE JOYSTICK
// ✅ Separación correcta de rotación horizontal y alineación con terreno
// ✅ Sin conflictos de dirección al girar
// ✅ Movimiento suave y predecible
// 🌐 ADAPTADO A PHOTON PUN2 - Multijugador en tiempo real

public class SimpleDinosaurController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("🌐 Photon PUN2")]
    private PhotonView photonView;

    [Header("🌐 Optimización de Red")]
    [Tooltip("Velocidad de interpolación de posición (más alto = más suave pero con lag)")]
    [Range(5f, 30f)]
    public float networkPositionLerp = 15f;

    [Tooltip("Velocidad de interpolación de rotación")]
    [Range(5f, 30f)]
    public float networkRotationLerp = 20f;

    [Tooltip("Distancia mínima para sincronizar posición (metros)")]
    public float positionThreshold = 0.1f;

    [Tooltip("Ángulo mínimo para sincronizar rotación (grados)")]
    public float rotationThreshold = 2f;

    // Variables de red para interpolación
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;
    private float networkSpeed;

    // Última sincronización de parámetros del Animator (para detectar cambios)
    private float lastNetworkSpeed = -1f;
    private bool lastNetworkIsRunning = false;
    private bool lastNetworkIsCrouching = false;
    private bool lastNetworkIsSwimming = false;
    private bool lastNetworkIsAttacking = false;
    private bool lastNetworkIsGrounded = true;
    private bool lastNetworkIsDead = false;
    private int lastNetworkState = -1;

    // Timestamp para predicción
    private double lastReceiveTime;

    [Header("Referencias")]
    public Animator animator;
    public AudioSource audioSource;
    public Transform cameraTransform;
    
    [Header("Controles Táctiles")]
    public VariableJoystick movementJoystick;
    public Button runButton;
    public Button crouchButton;
    public Button callButton;
    public Button jumpButton;
    public Button attackButton;

    [Header("📏 CONFIGURACIÓN DE TAMAÑO DEL DINOSAURIO")]
    [Tooltip("Selecciona el tamaño del dinosaurio para aplicar valores preconfigurados")]
    public DinosaurSize dinosaurSize = DinosaurSize.Medium;
    [Tooltip("Altura real del modelo (usado para cálculos automáticos)")]
    public float modelHeight = 2.5f;
    [Space(10)]
    [Tooltip("Haz click derecho en el script → 'Apply Size Preset' para aplicar configuración automática")]
    public bool autoConfigureOnStart = false;

    public enum DinosaurSize
    {
        Small,      // 1-2m (Compsognathus, Velociraptor joven)
        Medium,     // 3-5m (Velociraptor, Dilophosaurus)
        Large,      // 6-10m (T-Rex, Spinosaurus)
        Custom      // Valores manuales 
    }

    [Header("Velocidades")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float crouchSpeed = 1f;
    public float turnSpeed = 120f;
    
    [Header("🔄 CONFIGURACIÓN DE RADIO DE GIRO - MEJORADO")]
    [Tooltip("Radio mínimo de giro al caminar")]
    public float walkTurnRadius = 1.5f;
    [Tooltip("Radio mínimo de giro al correr")]
    public float runTurnRadius = 3f;
    [Tooltip("Radio mínimo de giro en crouch")]
    public float crouchTurnRadius = 1.2f;
    [Tooltip("Suavizado del giro (más alto = más suave)")]
    [Range(1f, 10f)]
    public float turnSmoothness = 5f;
    [Tooltip("Velocidad máxima de rotación (grados/segundo)")]
    public float maxRotationSpeed = 180f;
    [Tooltip("Factor de suavizado de dirección")]
    [Range(0.1f, 1f)]
    public float directionSmoothFactor = 0.3f;
    [Tooltip("Tolerancia para considerar que el dinosaurio está mirando hacia el objetivo")]
    [Range(5f, 45f)]
    public float lookAtTolerance = 15f;
    [Tooltip("Factor de velocidad durante el giro (reduce velocidad al girar)")]
    [Range(0.3f, 1f)]
    public float turningSpeedFactor = 0.7f;
    
    [Header("⭐ CONFIGURACIÓN DE SALTO")]
    [Tooltip("Altura del salto")]
    public float jumpHeight = 2f;
    [Tooltip("Tiempo de espera entre saltos (después de aterrizar)")]
    public float jumpCooldown = 1f;
	
	[Header("⭐ SALTO - Coyote & Buffer")]
	[Tooltip("Tiempo (s) que permitido saltar después de despegar (coyote time)")]
	public float coyoteTime = 0.12f;
	[Tooltip("Tiempo (s) para guardar input de salto antes de aterrizar (jump buffer)")]
	public float jumpBufferTime = 0.12f;

	private float lastGroundedTime = -10f;  // ✅ Variable unificada
	private float jumpBufferTimerLocal = 0f;
    
    [Header("⭐ CONFIGURACIÓN DE GRAVEDAD")]
    [Tooltip("Fuerza de gravedad (más negativo = cae más rápido)")]
    public float gravity = -20f;
    [Tooltip("Velocidad terminal de caída")]
    public float terminalVelocity = -50f;
    
    [Header("🏔️ CONFIGURACIÓN DE PENDIENTES")]
    [Tooltip("Multiplicador de velocidad en subidas (mayor = sube más rápido)")]
    [Range(0.5f, 2f)]
    public float slopeSpeedMultiplier = 1.2f;
    [Tooltip("Fuerza extra al bajar pendientes")]
    public float slopeForceDown = 10f;
    [Tooltip("⭐ Alinear modelo con el terreno")]
    public bool alignToTerrain = true;
    [Tooltip("Velocidad de alineación con el terreno")]
    [Range(1f, 20f)]
    public float terrainAlignmentSpeed = 8f;
    [Tooltip("Máximo ángulo de inclinación permitido")]
    [Range(0f, 90f)]
    public float maxTerrainTilt = 45f;
    
    [Header("⚔️ SISTEMA DE ATAQUE - MEJORADO")]
    [Tooltip("Daño que hace cada ataque")]
    public float attackDamage = 25f;
    [Tooltip("Rango del ataque (distancia)")]
    public float attackRange = 2f;
    [Tooltip("Ángulo del ataque (cono frontal)")]
    [Range(0f, 180f)]
    public float attackAngle = 90f;
    [Tooltip("⭐ Tiempo de espera entre ataques - REDUCIDO para mejor respuesta")]
    public float attackCooldown = 0.5f;
    [Tooltip("Duración del ataque (animación)")]
    public float attackDuration = 0.5f;
    [Tooltip("Layer de enemigos (objetos que pueden recibir daño)")]
    public LayerMask enemyLayer;
    [Tooltip("Punto desde donde sale el ataque (boca del dinosaurio)")]
    public Transform attackPoint;
    [Tooltip("Mostrar área de ataque en el editor")]
    public bool showAttackGizmos = true;
    
    [Header("⚔️ NUEVAS MEJORAS DE ATAQUE")]
    [Tooltip("⭐ Permitir buffer de input (guarda el ataque para después)")]
    public bool enableAttackBuffer = true;
    [Tooltip("⭐ Tiempo que se guarda el input de ataque")]
    public float attackBufferTime = 0.3f;
    [Tooltip("⭐ Tolerancia al estar 'en el suelo' para atacar")]
    public float groundedTolerance = 0.2f;
    [Tooltip("⭐ Permitir atacar en el aire")]
    public bool canAttackInAir = true;
    
    [Header("⚔️ EFECTOS DE ATAQUE")]
    [Tooltip("Fuerza de empuje al atacar")]
    public float attackKnockback = 5f;
    [Tooltip("Puede moverse mientras ataca")]
    public bool canMoveWhileAttacking = false;
    [Tooltip("Puede rotar mientras ataca")]
    public bool canRotateWhileAttacking = true;
    
    [Header("🎵 AUDIO DE ATAQUE")]
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
	
	[Header("🎤 Call / Roar Settings")]
	[Tooltip("Duración del rugido (segundos) - DEPRECATED: Usa CallSystem en su lugar")]
	public float callDuration = 2.5f; // ajusta según la duración de tu animación
	private bool isCalling = false;
	private float callTimer = 0f;

	[Header("📞 Sistema de Llamados Mejorado")]
	[Tooltip("Referencia al sistema de llamados (CallSystem) - opcional")]
	public CallSystem callSystem;

	[Header("🍖 Sistema de Hambre, Sed y Estamina")]
	[Tooltip("Hambre máxima")]
	[Range(0f, 200f)]
	public float maxHunger = 100f;
	[Tooltip("Hambre actual")]
	public float currentHunger = 100f;
	[Tooltip("Velocidad de degradación de hambre por segundo")]
	public float hungerDecayRate = 0.5f;

	[Tooltip("Sed máxima")]
	[Range(0f, 200f)]
	public float maxThirst = 100f;
	[Tooltip("Sed actual")]
	public float currentThirst = 100f;
	[Tooltip("Velocidad de degradación de sed por segundo")]
	public float thirstDecayRate = 0.7f;

	[Tooltip("Estamina máxima")]
	[Range(0f, 200f)]
	public float maxStamina = 100f;
	[Tooltip("Estamina actual")]
	public float currentStamina = 100f;
	[Tooltip("Velocidad de consumo de estamina al correr")]
	public float staminaDrainRate = 10f;
	[Tooltip("Velocidad de regeneración de estamina normal")]
	public float staminaRegenRate = 5f;
	[Tooltip("Velocidad de regeneración de estamina al dormir")]
	public float staminaSleepRegenRate = 15f;

	[Header("🍗 Sistema de Comer/Beber")]
	[Tooltip("Distancia para detectar comida/agua")]
	public float foodDetectionRange = 3f;
	[Tooltip("Velocidad de aumento de hambre al comer")]
	public float eatingSpeed = 15f;
	[Tooltip("Velocidad de aumento de sed al beber")]
	public float drinkingSpeed = 20f;
	[Tooltip("Duración de animación de comer")]
	public float eatingAnimationDuration = 2f;

	[Header("🍖 UI de Comer/Beber")]
	[Tooltip("Botón para comer (aparece cerca de comida)")]
	public Button eatButton;
	[Tooltip("Botón para beber (aparece cerca de agua)")]
	public Button drinkButton;

	[Header("📊 Barras UI de Estadísticas")]
	[Tooltip("Barra de hambre (Image tipo Filled)")]
	public Image hungerBar;
	[Tooltip("Barra de sed (Image tipo Filled)")]
	public Image thirstBar;
	[Tooltip("Barra de estamina (Image tipo Filled)")]
	public Image staminaBar;

	// Estados de comer/beber
	private bool isEating = false;
	private bool isDrinking = false;
	private GameObject nearbyFood = null;
	private GameObject nearbyWater = null;

	// Referencia cacheada al sistema de sueño
	private DinosaurSleepSystem sleepSystemCache;

    [Header("🔄 CONFIGURACIÓN DE TURN Y LOOK - BASADO EN CÁMARA")]
    [Tooltip("Activar poses estáticas de giro")]
    public bool enableStaticTurn = true;
    [Tooltip("Activar poses de mirada arriba/abajo")]
    public bool enableVerticalLook = true;
    [Tooltip("Umbral para detectar cambio de cámara (grados)")]
    public float turnDetectionThreshold = 30f;
    [Tooltip("Umbral para detectar cambio de cámara vertical (grados)")]
    public float lookDetectionThreshold = 20f;
    [Tooltip("Ángulo para detectar frente/atrás")]
    public float frontBackAngle = 45f;
    [Tooltip("Velocidad de transición entre poses de giro")]
    [Range(1f, 10f)]
    public float turnTransitionSpeed = 4f;
    [Tooltip("Velocidad de transición entre poses de mirada vertical")]
    [Range(1f, 10f)]
    public float lookTransitionSpeed = 4f;
    [Tooltip("⭐ Límite máximo de Turn y Look (evita poses estáticas)")]
    [Range(0.5f, 1f)]
    public float maxTurnLookValue = 0.80f;

    [Header("🎭 SISTEMA DE IDLE VARIATIONS")]
    [Tooltip("Activar cambio automático entre diferentes idles")]
    public bool enableIdleVariations = true;
    [Tooltip("Número de idle variations disponibles (0 = idle normal, 1+ = variations)")]
    [Range(0, 10)]
    public int numberOfIdleVariations = 3;
    [Tooltip("Duraciones manuales para cada idle variation (índice 0 = variation 1, índice 1 = variation 2, etc.)")]
    public float[] idleVariationDurations = new float[] { 4f, 3f, 5f };
    [Tooltip("Nombre del estado del Animator que contiene el Blend Tree de Turn/Look con Idle Variations")]
    public string idleStateNameInAnimator = "Turn Look";
    [Tooltip("Tiempo mínimo en idle antes de cambiar (segundos)")]
    public float minIdleTimeBeforeVariation = 5f;
    [Tooltip("Tiempo máximo en idle antes de cambiar (segundos)")]
    public float maxIdleTimeBeforeVariation = 15f;
    [Tooltip("Probabilidad de activar idle variation (0-100%)")]
    [Range(0f, 100f)]
    public float idleVariationChance = 70f;
    [Tooltip("Permitir idle variations solo cuando completamente quieto")]
    public bool onlyWhenFullyIdle = true;
    [Tooltip("Multiplicador de velocidad de transición al entrar a variation (0.1 = muy lento, 1.0 = normal)")]
    [Range(0.1f, 1f)]
    public float variationEnterSpeedMultiplier = 0.5f;
    [Tooltip("Multiplicador de velocidad de transición al salir de variation (0.1 = muy lento, 1.0 = normal)")]
    [Range(0.1f, 1f)]
    public float variationExitSpeedMultiplier = 0.4f;
    [Tooltip("Mostrar logs de debug en la consola (útil para troubleshooting)")]
    public bool showIdleVariationDebugLogs = false;
    
    [Header("Configuración de Movimiento")]
    public float turnInPlaceThreshold = 0.1f;
    public float movementThreshold = 0.2f;
    [Tooltip("Suavizado del movimiento")]
    [Range(1f, 20f)]
    public float movementSmoothness = 10f;
    [Tooltip("Suavizado de las animaciones de dirección (MoveX/MoveZ)")]
    [Range(0.05f, 0.5f)]
    public float directionAnimationDampTime = 0.1f;
    
    [Header("Audio")]
    public AudioClip[] walkSounds;
    public AudioClip[] runSounds;
    public AudioClip[] crouchWalkSounds;
    public AudioClip[] callSounds;
    public AudioClip[] jumpSounds;
    public AudioClip[] landSounds;

    [Header("🌊 SISTEMA DE NATACIÓN")]
    [Tooltip("Layer del agua (debe tener Trigger activado)")]
    public LayerMask waterLayer;
    [Tooltip("Velocidad de natación")]
    public float swimSpeed = 3f;
    [Tooltip("Fuerza de flotación (empuje hacia arriba)")]
    public float buoyancyForce = 9.8f;
    [Tooltip("Resistencia del agua (frena movimiento)")]
    [Range(0.5f, 0.95f)]
    public float waterDrag = 0.8f;
    [Tooltip("Altura del agua donde empieza a flotar")]
    public float waterSurfaceOffset = 1f;
    [Tooltip("Tolerancia para evitar parpadeo entre estados (0.5 = 50% del offset)")]
    [Range(0.3f, 0.8f)]
    public float waterHysteresis = 0.6f;
    [Tooltip("Permitir salto desde el agua")]
    public bool canJumpFromWater = false;

    // Estados
    public enum MovementState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Crouch = 3,
        Jump = 4,
        Falling = 5,
        Attacking = 6,
        Swimming = 7,
        IdleSwim = 8
    }

    public enum TurnState
    {
        Idle = 0,
        TurnLeft = -1,
        TurnRight = 1
    }
    
    [Header("Estado Actual")]
    public MovementState currentState = MovementState.Idle;
    public TurnState currentTurnState = TurnState.Idle;
    public bool isRunning = false;
    public bool isCrouching = false;
    public bool isOnSlope = false;
    public float currentSlopeAngle = 0f;
    public bool isAttacking = false;
    public int enemiesInRange = 0;
    public bool isInWater = false;
    public bool isSwimming = false;
    public bool isDead = false;

    // Character Controller
    private CharacterController controller;
    
    // Variables de movimiento
    private Vector3 inputVector;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    
    // ⭐ VARIABLES DE RADIO DE GIRO NATURAL
    private Vector3 currentMoveDirection;
    private Vector3 targetMoveDirection;
    private Vector3 smoothedMoveDirection;
    private float currentTurnRadius = 0f;
    
    // ⭐ FIX: Variables separadas para rotación
    private float currentYaw = 0f;  // Rotación horizontal (controlada por joystick)
    private Quaternion terrainRotation = Quaternion.identity;  // Rotación del terreno (pitch/roll)
    
    // Variables de salto
    private bool canJump = true;
    private float jumpCooldownTimer = 0f;
    private bool hasJumped = false;
    
    // Variables de ataque
    private float attackCooldownTimer = 0f;
    private float attackTimer = 0f;
    private bool attackBuffered = false;
    private float attackBufferTimer = 0f;
    private List<GameObject> enemiesHit = new List<GameObject>();
    
    // Variables de detección de pendiente
    private RaycastHit slopeHit;
    private Vector3 slopeNormal;
    
    // ⭐ Variables para alineación con terreno
    private Vector3 smoothNormal = Vector3.up;
    
    // Variables de animación Turn/Look basado en cámara
    private float currentTurn = 0f;
    private float targetTurn = 0f;
    private float currentLook = 0f;
    private float targetLook = 0f;
    private float lastCameraAngle = 0f;
    private float lastCameraVerticalAngle = 0f;

    // 🎭 Variables de Idle Variations
    private float idleTimer = 0f;
    private float nextIdleVariationTime = 0f;
    private float currentIdleVariation = 0f;  // Cambiado a float para compatibilidad con Animator
    private int currentIdleVariationIndex = -1;  // Índice actual (0-based) para acceder al array de duraciones
    private bool isPlayingIdleVariation = false;
    private float idleVariationTimer = 0f;
    private float savedTurnBeforeVariation = 0f;  // Guardar Turn antes de variation
    private float savedLookBeforeVariation = 0f;  // Guardar Look antes de variation
    private bool isRestoringTurnLook = false;     // Flag para indicar que está restaurando

    // 🌊 Variables de natación
    private Collider waterCollider = null;
    private float waterSurfaceY = 0f;
    private bool wasInWater = false;


    void Start()
    {
        // 🌐 Obtener PhotonView
        photonView = GetComponent<PhotonView>();

        // ⚠️ CRÍTICO: Asignar componentes esenciales ANTES de cualquier return
        // Estos componentes son necesarios tanto para jugadores locales como remotos
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = new Vector3(0, 1f, 0);
            controller.radius = 0.5f;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        // 🌐 Solo configurar controles para el jugador local
        if (!photonView.IsMine)
        {
            // Desactivar controles UI para jugadores remotos
            if (movementJoystick != null)
                movementJoystick.gameObject.SetActive(false);
            if (runButton != null)
                runButton.gameObject.SetActive(false);
            if (crouchButton != null)
                crouchButton.gameObject.SetActive(false);
            if (callButton != null)
                callButton.gameObject.SetActive(false);
            if (jumpButton != null)
                jumpButton.gameObject.SetActive(false);
            if (attackButton != null)
                attackButton.gameObject.SetActive(false);
            if (eatButton != null)
                eatButton.gameObject.SetActive(false);
            if (drinkButton != null)
                drinkButton.gameObject.SetActive(false);

            // Desactivar barras de stats para jugadores remotos (hambre/sed/estamina son locales)
            if (hungerBar != null)
                hungerBar.transform.parent.gameObject.SetActive(false);
            if (thirstBar != null && thirstBar.transform.parent != hungerBar.transform.parent)
                thirstBar.transform.parent.gameObject.SetActive(false);
            if (staminaBar != null && staminaBar.transform.parent != hungerBar.transform.parent)
                staminaBar.transform.parent.gameObject.SetActive(false);

            return; // No ejecutar el resto del Start para jugadores remotos
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }

        if (attackPoint == null)
            attackPoint = transform;

        // Inicializar rotación
        currentYaw = transform.eulerAngles.y;

        // 📏 Auto-configurar tamaño si está activado
        if (autoConfigureOnStart && dinosaurSize != DinosaurSize.Custom)
        {
            ApplySizePreset();
        }

		// ⚡ Cachear referencia al sistema de sueño
		sleepSystemCache = GetComponent<DinosaurSleepSystem>();

        // 🎭 Inicializar sistema de idle variations
        ResetIdleVariationTimer();

        SetupButtonListeners();
    }
    
    void SetupButtonListeners()
    {
        if (runButton != null)
        {
            runButton.onClick.RemoveAllListeners();
            runButton.onClick.AddListener(() => {
                isRunning = !isRunning;
                // Si activa correr mientras está agachado, se mantiene agachado
                // hasta que mueva el joystick (lógica en CalculateMovement)
            });
        }

        if (crouchButton != null)
        {
            crouchButton.onClick.RemoveAllListeners();
            crouchButton.onClick.AddListener(() => {
                isCrouching = !isCrouching;
                // Ya no desactivamos isRunning aquí
                // El usuario puede tener run activo y crouch al mismo tiempo
            });
        }

        if (callButton != null)
        {
            callButton.onClick.RemoveAllListeners();
            // Si hay CallSystem configurado, no hacer nada aquí (CallSystem maneja el botón)
            // Si no hay CallSystem, usar la función antigua
            if (callSystem == null)
            {
                callButton.onClick.AddListener(PlayCallSound);
            }
        }

        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(TryJump);
        }

        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(TryAttack);
        }

		if (eatButton != null)
		{
			eatButton.onClick.RemoveAllListeners();
			eatButton.onClick.AddListener(ToggleEating);
			eatButton.gameObject.SetActive(false); // Ocultar inicialmente
		}

		if (drinkButton != null)
		{
			drinkButton.onClick.RemoveAllListeners();
			drinkButton.onClick.AddListener(ToggleDrinking);
			drinkButton.gameObject.SetActive(false); // Ocultar inicialmente
		}
    }

    // 📏 SISTEMA DE CONFIGURACIÓN AUTOMÁTICA POR TAMAÑO
    [ContextMenu("Apply Size Preset")]
    public void ApplySizePreset()
    {
        if (dinosaurSize == DinosaurSize.Custom)
        {
            Debug.LogWarning("⚠️ Tamaño configurado como 'Custom'. No se aplicarán presets automáticos.");
            return;
        }


        switch (dinosaurSize)
        {
            case DinosaurSize.Small:
                ApplySmallPreset();
                break;
            case DinosaurSize.Medium:
                ApplyMediumPreset();
                break;
            case DinosaurSize.Large:
                ApplyLargePreset();
                break;
        }

        // Actualizar Character Controller
        if (controller != null)
        {
            controller.height = modelHeight;
            controller.radius = modelHeight * 0.2f;
            controller.center = new Vector3(0, modelHeight * 0.5f, 0);
        }

    }

    void ApplySmallPreset()
    {
        // Velocidades
        walkSpeed = modelHeight * 1.2f;
        runSpeed = modelHeight * 2.5f;
        crouchSpeed = modelHeight * 0.6f;
        swimSpeed = modelHeight * 1.2f;
        turnSpeed = 180f;

        // Salto
        jumpHeight = modelHeight * 0.8f;
        jumpCooldown = 0.5f;

        // Gravedad
        gravity = -25f;
        terminalVelocity = -40f;

        // Agua
        waterSurfaceOffset = modelHeight * 0.5f;
        buoyancyForce = 15f;
        waterDrag = 0.75f;
        waterHysteresis = 0.6f;

        // Ataque
        attackRange = modelHeight * 0.6f;
        attackDamage = modelHeight * 10f;

        // Giro
        walkTurnRadius = 1.0f;
        runTurnRadius = 2.0f;
        crouchTurnRadius = 0.8f;
        maxRotationSpeed = 200f;
    }

    void ApplyMediumPreset()
    {
        // Velocidades
        walkSpeed = modelHeight * 1.0f;
        runSpeed = modelHeight * 2.2f;
        crouchSpeed = modelHeight * 0.5f;
        swimSpeed = modelHeight * 1.2f;
        turnSpeed = 120f;

        // Salto
        jumpHeight = modelHeight * 0.6f;
        jumpCooldown = 1.0f;

        // Gravedad
        gravity = -20f;
        terminalVelocity = -50f;

        // Agua
        waterSurfaceOffset = modelHeight * 0.45f;
        buoyancyForce = 9.8f;
        waterDrag = 0.80f;
        waterHysteresis = 0.6f;

        // Ataque
        attackRange = modelHeight * 0.5f;
        attackDamage = modelHeight * 10f;

        // Giro
        walkTurnRadius = 1.5f;
        runTurnRadius = 3.0f;
        crouchTurnRadius = 1.2f;
        maxRotationSpeed = 180f;
    }

    void ApplyLargePreset()
    {
        // Velocidades
        walkSpeed = modelHeight * 0.7f;
        runSpeed = modelHeight * 1.6f;
        crouchSpeed = modelHeight * 0.35f;
        swimSpeed = modelHeight * 0.9f;
        turnSpeed = 90f;

        // Salto
        jumpHeight = modelHeight * 0.4f;
        jumpCooldown = 2.0f;

        // Gravedad
        gravity = -15f;
        terminalVelocity = -60f;

        // Agua
        waterSurfaceOffset = modelHeight * 0.4f;
        buoyancyForce = 8f;
        waterDrag = 0.85f;
        waterHysteresis = 0.6f;

        // Ataque
        attackRange = modelHeight * 0.5f;
        attackDamage = modelHeight * 15f;

        // Giro
        walkTurnRadius = 2.5f;
        runTurnRadius = 5.0f;
        crouchTurnRadius = 2.0f;
        maxRotationSpeed = 120f;
    }

    void Update()
    {
        // 🌐 Jugadores remotos: interpolar posición y rotación
        if (!photonView.IsMine)
        {
            // Interpolar posición con predicción de movimiento
            if (networkVelocity != Vector3.zero)
            {
                // Predicción: calcular dónde debería estar basado en velocidad
                float timeSinceLastUpdate = (float)(PhotonNetwork.Time - lastReceiveTime);
                Vector3 predictedPosition = networkPosition + (networkVelocity * timeSinceLastUpdate);

                // Interpolar hacia la posición predicha
                transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * networkPositionLerp);
            }
            else
            {
                // Sin velocidad, solo interpolar a la posición de red
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * networkPositionLerp);
            }

            // Interpolar rotación
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * networkRotationLerp);

            // ⚠️ HACK: Forzar actualización de controller.isGrounded
            // Aplicar un movimiento minúsculo hacia abajo para que Unity actualice las colisiones
            controller.Move(Vector3.down * 0.001f);

            return; // No ejecutar lógica de control para jugadores remotos
        }

		// 💀 Si está muerto, no hacer nada
		if (isDead)
		{
			// Detener todo movimiento
			moveDirection = Vector3.zero;
			velocity.x = 0f;
			velocity.z = 0f;
			currentSpeed = 0f;
			targetSpeed = 0f;
			isRunning = false;
			return;
		}

		// 📞 Verificar si el CallSystem está activo y llamando
		if (callSystem != null && callSystem.IsCalling())
		{
			isCalling = true;
		}

        // Leer input del joystick
        GetInput();

		// 🍖 Actualizar hambre, sed y estamina
		UpdateHungerThirstStamina();

		// 🍗 Detectar comida y agua cercana
		DetectFoodAndWater();

        // Detectar pendientes
        CheckSlope();

        // Calcular movimiento y rotación
        CalculateMovement();

        // ⭐ FIX: Aplicar rotación separada
        ApplySeparatedRotation();

        // Aplicar movimiento
        ApplyMovement();

        // ⭐ Alinear con el terreno (solo afecta pitch/roll, no yaw)
        // 🌊 NO alinear cuando está nadando
        if (alignToTerrain && controller.isGrounded && !isSwimming)
        {
            AlignToTerrainFixed();
        }

        // 🌊 Resetear rotación a horizontal cuando está nadando
        if (isSwimming)
        {
            ResetRotationToHorizontal();
        }

        // Actualizar animaciones
        UpdateAnimations();

        // Actualizar sistema de ataque
        UpdateAttackSystem();

        // Actualizar Turn y Look basado en cámara
        UpdateCameraBasedTurnAndLook();

        // 🎭 Actualizar sistema de Idle Variations
        UpdateIdleVariations();

        // Actualizar timers
        UpdateTimers();

        // Actualizar estado
        UpdateState();

        // Actualizar UI
        UpdateUI();

		// 📊 Actualizar barras de estadísticas
		UpdateStatsUI();
    }
    
    void GetInput()
    {
        if (movementJoystick != null)
        {
            inputVector = new Vector3(movementJoystick.Horizontal, 0, movementJoystick.Vertical);
        }
        else
        {
            inputVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        }

        // Limitar magnitud
        if (inputVector.magnitude > 1f)
            inputVector = inputVector.normalized;
    }
    
    void CheckSlope()
    {
        if (controller.isGrounded)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out slopeHit, controller.height))
            {
                slopeNormal = slopeHit.normal;
                currentSlopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
                isOnSlope = currentSlopeAngle > 1f && currentSlopeAngle < controller.slopeLimit;
            }
            else
            {
                isOnSlope = false;
                currentSlopeAngle = 0f;
                slopeNormal = Vector3.up;
            }
        }
        else
        {
            isOnSlope = false;
            currentSlopeAngle = 0f;
            slopeNormal = Vector3.up;
        }
    }
    
    void CalculateMovement()
    {
		// 🍖 No moverse mientras come o bebe
		if (isEating || isDrinking)
		{
			targetSpeed = 0f;
			moveDirection = Vector3.zero;
			currentSpeed = 0f;
			return;
		}

        // ⭐ FIX: Calcular dirección relativa a la cámara de forma más estable
        if (inputVector.magnitude > turnInPlaceThreshold)
        {
            // 🏃 Si está agachado y tiene run activo, al mover el joystick sale del crouch
            if (isCrouching && isRunning && inputVector.magnitude > movementThreshold)
            {
                isCrouching = false;
            }

            // Obtener vectores de la cámara
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            // Proyectar en plano horizontal
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calcular dirección deseada basada en el input del joystick
            targetMoveDirection = (cameraForward * inputVector.z + cameraRight * inputVector.x).normalized;

            // Si estamos atacando y no podemos movernos, mantener dirección actual
            if (isAttacking && !canMoveWhileAttacking)
            {
                targetMoveDirection = transform.forward;
            }

            // Suavizar la dirección de movimiento
            if (currentMoveDirection == Vector3.zero)
            {
                currentMoveDirection = targetMoveDirection;
            }
            else
            {
                currentMoveDirection = Vector3.Slerp(currentMoveDirection, targetMoveDirection,
                    Time.deltaTime * turnSmoothness);
            }
            
            // 🌊 Calcular velocidad objetivo (diferente en agua)
            if (isInWater && isSwimming)
            {
                targetSpeed = swimSpeed;  // Velocidad de natación
            }
            else
            {
                // Determinar velocidad según el estado: crouch, run o walk
                if (isCrouching)
                {
                    targetSpeed = crouchSpeed;
                }
                else
                {
                    targetSpeed = isRunning ? runSpeed : walkSpeed;
                }
            }

            // Reducir velocidad durante el giro (no aplica en agua)
            if (!isInWater)
            {
                float angleToTarget = Vector3.Angle(transform.forward, currentMoveDirection);
                if (angleToTarget > lookAtTolerance)
                {
                    targetSpeed *= turningSpeedFactor;
                }
            }

            // Aplicar modificador de pendiente (no aplica en agua)
            if (isOnSlope && !isInWater)
            {
                float slopeFactor = Vector3.Dot(currentMoveDirection, slopeNormal);
                if (slopeFactor < 0) // Subiendo
                {
                    targetSpeed *= slopeSpeedMultiplier;
                }
            }
        }
        else
        {
            // Sin input
            targetSpeed = 0f;
            if (currentMoveDirection.magnitude > 0.1f)
            {
                currentMoveDirection = Vector3.Lerp(currentMoveDirection, Vector3.zero, Time.deltaTime * 5f);
            }
        }
        
        // Suavizar velocidad
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * movementSmoothness);
        
        // Calcular dirección de movimiento final
        moveDirection = transform.forward * currentSpeed;

        
        // Si no podemos movernos mientras atacamos
        if (isAttacking && !canMoveWhileAttacking)
        {
            moveDirection = Vector3.zero;
        }
    }
    
void ApplySeparatedRotation()
{
    if (inputVector.magnitude > turnInPlaceThreshold)
    {
        if (!isAttacking || canRotateWhileAttacking)
        {
            if (targetMoveDirection != Vector3.zero)
            {
                // Calcular ángulo objetivo (dirección deseada)
                float targetYaw = Mathf.Atan2(targetMoveDirection.x, targetMoveDirection.z) * Mathf.Rad2Deg;
                float angleDifference = Mathf.DeltaAngle(currentYaw, targetYaw);

                // Radio dinámico de giro (según velocidad y estado)
                if (isCrouching)
                {
                    currentTurnRadius = crouchTurnRadius;
                }
                else
                {
                    currentTurnRadius = isRunning ? runTurnRadius : walkTurnRadius;
                }

                // Calcular velocidad de rotación
                float rotationStep = Mathf.Clamp(angleDifference, -maxRotationSpeed * Time.deltaTime, maxRotationSpeed * Time.deltaTime);
                currentYaw += rotationStep;

                // Aplicar rotación suave al transform
                Vector3 eulerRotation = transform.eulerAngles;
                eulerRotation.y = currentYaw;
                transform.eulerAngles = eulerRotation;

                // ⭐ Actualizar dirección de movimiento para que siga la curva
                Quaternion turnRotation = Quaternion.Euler(0f, rotationStep * (1f / currentTurnRadius), 0f);
                currentMoveDirection = turnRotation * currentMoveDirection;

                // Moverse siguiendo el giro
                moveDirection = transform.forward * currentSpeed;
            }
        }
    }
    else
    {
        // Sin input, mantener orientación actual
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.y = currentYaw;
        transform.eulerAngles = eulerRotation;
    }
}


    
void ApplyMovement()
{
    // ✅ ACTUALIZAR TIEMPO EN SUELO PRIMERO
    bool wasGrounded = controller.isGrounded;

    // 🌊 SISTEMA DE NATACIÓN
    if (isInWater)
    {
        // ✅ Calcular profundidad real del agua (desde el centro del personaje)
        float waterDepth = waterSurfaceY - transform.position.y;
        bool isDeepEnoughToSwim = waterDepth >= waterSurfaceOffset;

        if (isDeepEnoughToSwim)
        {
            // 🏊 AGUA PROFUNDA - ACTIVAR NATACIÓN

            // ✅ FLOTACIÓN - aplicar fuerza hacia arriba
            velocity.y += buoyancyForce * Time.deltaTime;

            // Limitar velocidad vertical en agua
            velocity.y = Mathf.Clamp(velocity.y, -2f, 2f);

            // Aplicar resistencia del agua (drag)
            velocity *= waterDrag;

            // Determinar si está nadando (moviéndose)
            isSwimming = inputVector.magnitude > 0.1f;

            // No resetear hasJumped en agua (a menos que se permita saltar desde agua)
            if (canJumpFromWater)
            {
                canJump = true;
                hasJumped = false;
            }
        }
        else
        {
            // 🚶 AGUA POCO PROFUNDA - CAMINAR NORMALMENTE

            isSwimming = false;

            // Aplicar física normal como en tierra
            if (controller.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            // Aplicar gravedad normal
            velocity.y += gravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, terminalVelocity);

            // Aplicar fuerza adicional si está en pendiente
            if (isOnSlope && controller.isGrounded)
            {
                velocity += Vector3.down * slopeForceDown * Time.deltaTime;
            }
        }
    }
    else
    {
        // 🏃 MOVIMIENTO NORMAL (FUERA DEL AGUA)

        // Mantener pegado al suelo (evita saltos falsos cuando ya está en tierra)
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // Aplicar gravedad
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, terminalVelocity);

        // Aplicar fuerza adicional si está en pendiente
        if (isOnSlope && controller.isGrounded)
        {
            velocity += Vector3.down * slopeForceDown * Time.deltaTime;
        }

        isSwimming = false;
    }

    // Combinar movimiento horizontal y vertical
    Vector3 finalMove = moveDirection + velocity;

    // Aplicar movimiento
    controller.Move(finalMove * Time.deltaTime);

    // ✅ ACTUALIZAR ESTADO DE SUELO DESPUÉS DEL MOVIMIENTO
    if (controller.isGrounded && !isSwimming)
    {
        lastGroundedTime = Time.time;

        // 🔹 Resetear hasJumped cuando aterriza
        if (hasJumped)
        {
            hasJumped = false;
        }
    }

}


    
    void AlignToTerrainFixed()
    {
        // ⭐ FIX: Nueva implementación que no afecta la rotación Y
        
        // Detectar el terreno debajo
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        RaycastHit hit;
        
        Vector3 targetNormal = Vector3.up;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 5f))
        {
            targetNormal = hit.normal;
            
            // Limitar el ángulo máximo de inclinación
            float angleFromUp = Vector3.Angle(Vector3.up, targetNormal);
            if (angleFromUp > maxTerrainTilt)
            {
                targetNormal = Vector3.Lerp(targetNormal, Vector3.up, 
                    (angleFromUp - maxTerrainTilt) / angleFromUp);
            }
        }
        
        // Suavizar la normal
        smoothNormal = Vector3.Slerp(smoothNormal, targetNormal, Time.deltaTime * terrainAlignmentSpeed);
        
        // ⭐ FIX: Calcular rotación del terreno sin afectar Y
        // Crear una rotación que alinee con el terreno pero mantenga el yaw actual
        Vector3 forward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, smoothNormal).normalized;
        
        if (projectedForward.magnitude > 0.1f)
        {
            // Crear quaternion que mira hacia adelante con la normal del terreno
            Quaternion targetTerrainRotation = Quaternion.LookRotation(projectedForward, smoothNormal);
            
            // ⭐ FIX: Extraer solo pitch y roll, manteniendo el yaw actual
            Vector3 terrainEuler = targetTerrainRotation.eulerAngles;
            terrainEuler.y = currentYaw; // Forzar el yaw a mantenerse
            
            // Aplicar la rotación final
            Quaternion finalRotation = Quaternion.Euler(terrainEuler);
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, 
                Time.deltaTime * terrainAlignmentSpeed);
        }
    }

    // 🌊 Resetear rotación a horizontal al nadar
    void ResetRotationToHorizontal()
    {
        // Crear rotación horizontal (pitch=0, roll=0, mantener yaw)
        Vector3 targetEuler = transform.eulerAngles;
        targetEuler.x = 0f;  // Sin pitch (inclinación adelante/atrás)
        targetEuler.z = 0f;  // Sin roll (inclinación lateral)

        // Suavizar la transición a horizontal
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
            Time.deltaTime * terrainAlignmentSpeed);

        // También resetear smoothNormal para que cuando salga del agua no tenga valores residuales
        smoothNormal = Vector3.Lerp(smoothNormal, Vector3.up, Time.deltaTime * terrainAlignmentSpeed);
    }

void UpdateAnimations()
{
    if (animator == null) return;

    // 🌊 PARÁMETROS DE NATACIÓN
    animator.SetBool("IsInWater", isInWater);
    animator.SetBool("IsSwimming", isSwimming);

    // 🔹 1. Velocidad normalizada (0 = quieto, 1 = corriendo/nadando)
    // 🎭 Si está reproduciendo Idle Variation, forzar Speed a 0 para velocidad normal
    float normalizedSpeed;
    if (isPlayingIdleVariation)
    {
        normalizedSpeed = 0f;  // Forzar a 0 durante idle variation
    }
    else
    {
        normalizedSpeed = isInWater ? (currentSpeed / swimSpeed) : (currentSpeed / runSpeed);
    }
    animator.SetFloat("Speed", normalizedSpeed);

    // 🔹 2. Estados principales
    animator.SetBool("IsGrounded", controller.isGrounded && !isInWater);
    animator.SetBool("IsRunning", isRunning && !isInWater);
    animator.SetBool("IsCrouching", isCrouching && !isInWater);
    animator.SetBool("IsAttacking", isAttacking);
    animator.SetFloat("VerticalSpeed", velocity.y);

    // 🔹 3. Parámetros de dirección (si los usas en tu blend tree)
    // ✅ Usar dampTime para suavizar las transiciones
    // 🎭 Si está reproduciendo Idle Variation, forzar MoveX/MoveZ a 0
    if (isPlayingIdleVariation)
    {
        animator.SetFloat("MoveX", 0f, directionAnimationDampTime, Time.deltaTime);
        animator.SetFloat("MoveZ", 0f, directionAnimationDampTime, Time.deltaTime);
    }
    else if (inputVector.magnitude > 0.1f)
    {
        Vector3 localMove = transform.InverseTransformDirection(currentMoveDirection);
        animator.SetFloat("MoveX", localMove.x, directionAnimationDampTime, Time.deltaTime);
        animator.SetFloat("MoveZ", localMove.z, directionAnimationDampTime, Time.deltaTime);
    }
    else
    {
        // Suavizar la transición a 0 también
        animator.SetFloat("MoveX", 0f, directionAnimationDampTime, Time.deltaTime);
        animator.SetFloat("MoveZ", 0f, directionAnimationDampTime, Time.deltaTime);
    }

    // 🔹 4. Parámetros de cámara / mirada
    animator.SetFloat("Turn", currentTurn);
    animator.SetFloat("Look", currentLook);

    // 🎭 5. Parámetro de Idle Variation (Float)
    animator.SetFloat("IdleVariation", currentIdleVariation);

}

    
    void UpdateCameraBasedTurnAndLook()
    {
        // 🎭 PRIORIDAD 1: Si está reproduciendo Idle Variation, desactivar Turn/Look
        if (isPlayingIdleVariation)
        {
            // Forzar Turn y Look a 0 suavemente (más lento para transición suave)
            targetTurn = 0f;
            targetLook = 0f;
            currentTurnState = TurnState.Idle;

            // Transición SUAVE usando multiplicador configurable
            float smoothSpeed = turnTransitionSpeed * variationEnterSpeedMultiplier;
            currentTurn = Mathf.Lerp(currentTurn, 0f, Time.deltaTime * smoothSpeed);
            currentLook = Mathf.Lerp(currentLook, 0f, Time.deltaTime * smoothSpeed);
            return;
        }

        // 🎭 PRIORIDAD 2: Si está restaurando Turn/Look después de variation
        if (isRestoringTurnLook)
        {
            // Restaurar suavemente a los valores guardados
            float smoothSpeed = turnTransitionSpeed * variationExitSpeedMultiplier;
            currentTurn = Mathf.Lerp(currentTurn, savedTurnBeforeVariation, Time.deltaTime * smoothSpeed);
            currentLook = Mathf.Lerp(currentLook, savedLookBeforeVariation, Time.deltaTime * smoothSpeed);

            // Cuando llegue cerca de los valores guardados, terminar restauración
            if (Mathf.Abs(currentTurn - savedTurnBeforeVariation) < 0.05f &&
                Mathf.Abs(currentLook - savedLookBeforeVariation) < 0.05f)
            {
                currentTurn = savedTurnBeforeVariation;
                currentLook = savedLookBeforeVariation;
                isRestoringTurnLook = false;
            }
            return;
        }

        if (!enableStaticTurn || cameraTransform == null)
        {
            currentTurn = Mathf.Lerp(currentTurn, 0f, Time.deltaTime * turnTransitionSpeed);
            currentLook = Mathf.Lerp(currentLook, 0f, Time.deltaTime * lookTransitionSpeed);
            return;
        }

        // Sistema de Turn basado en la diferencia entre cámara y dinosaurio
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 dinoForward = transform.forward;
        dinoForward.y = 0;
        dinoForward.Normalize();

        float angleToCamera = Vector3.SignedAngle(dinoForward, cameraForward, Vector3.up);

        // Determinar el estado de Turn basado en el ángulo
        if (Mathf.Abs(angleToCamera) > turnDetectionThreshold)
        {
            if (angleToCamera > 0)
            {
                targetTurn = Mathf.Min(angleToCamera / 90f, maxTurnLookValue); // Girar a la derecha
                currentTurnState = TurnState.TurnRight;
            }
            else
            {
                targetTurn = Mathf.Max(angleToCamera / 90f, -maxTurnLookValue); // Girar a la izquierda
                currentTurnState = TurnState.TurnLeft;
            }
        }
        else
        {
            targetTurn = 0f;
            currentTurnState = TurnState.Idle;
        }

        // Sistema de Look (vertical)
        if (enableVerticalLook)
        {
            float cameraPitch = cameraTransform.eulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;

            if (Mathf.Abs(cameraPitch) > lookDetectionThreshold)
            {
                if (cameraPitch < 0) // Mirando hacia arriba
                {
                    targetLook = Mathf.Min(-cameraPitch / 45f, maxTurnLookValue);
                }
                else // Mirando hacia abajo
                {
                    targetLook = Mathf.Max(-cameraPitch / 45f, -maxTurnLookValue);
                }
            }
            else
            {
                targetLook = 0f;
            }
        }
        else
        {
            targetLook = 0f;
        }

        // Suavizar las transiciones
        currentTurn = Mathf.Lerp(currentTurn, targetTurn, Time.deltaTime * turnTransitionSpeed);
        currentLook = Mathf.Lerp(currentLook, targetLook, Time.deltaTime * lookTransitionSpeed);

        // Limitar valores máximos
        currentTurn = Mathf.Clamp(currentTurn, -maxTurnLookValue, maxTurnLookValue);
        currentLook = Mathf.Clamp(currentLook, -maxTurnLookValue, maxTurnLookValue);
    }
    
    // ═══════════════════════════════════════════════════════════
    // 🎭 SISTEMA DE IDLE VARIATIONS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Actualiza el sistema de idle variations (rascarse, mirar alrededor, etc.)
    /// </summary>
    void UpdateIdleVariations()
    {
        if (!enableIdleVariations || animator == null || numberOfIdleVariations <= 0)
        {
            // Si está desactivado, asegurar que esté en idle normal (0)
            if (currentIdleVariation != 0f)
            {
                currentIdleVariation = 0f;
                currentIdleVariationIndex = -1;
                isPlayingIdleVariation = false;
            }
            return;
        }


        // Verificar si está en estado Idle (completamente quieto)
        bool isInIdleState = currentState == MovementState.Idle &&
                            !isAttacking &&
                            !isCalling &&
                            !isEating &&
                            !isDrinking &&
                            controller.isGrounded &&
                            !isInWater;

        // Si requiere estar completamente quieto
        if (onlyWhenFullyIdle)
        {
            isInIdleState = isInIdleState &&
                           currentSpeed <= 0.01f &&
                           inputVector.magnitude <= 0.01f;
        }

        // ✅ REMOVIDO: Ya NO verificamos isTurnLookNeutral
        // Ahora puede activarse aunque esté mirando hacia un lado
        // Se guardará la posición y se restaurará después

        // Si NO está en idle, resetear sistema
        if (!isInIdleState)
        {
            if (isPlayingIdleVariation || currentIdleVariation != 0f)
            {
                currentIdleVariation = 0f;
                currentIdleVariationIndex = -1;
                isPlayingIdleVariation = false;
            }
            idleTimer = 0f;
            return;
        }

        // ✅ Está en Idle - Actualizar lógica

        if (isPlayingIdleVariation)
        {
            // 🎬 Está reproduciendo una idle variation
            idleVariationTimer += Time.deltaTime;

            // 🎯 Obtener la duración de la animación actual desde el array
            float currentAnimationDuration = 3f; // Default fallback

            if (currentIdleVariationIndex >= 0 && currentIdleVariationIndex < idleVariationDurations.Length)
            {
                currentAnimationDuration = idleVariationDurations[currentIdleVariationIndex];
            }
            else
            {
                Debug.LogWarning($"🎭 ⚠️ WARNING: Índice de idle variation fuera de rango ({currentIdleVariationIndex}). Array tiene {idleVariationDurations.Length} elementos. Usando duración por defecto: 3s");
            }

            // Verificar si es tiempo de terminar la variation
            if (idleVariationTimer >= currentAnimationDuration)
            {
                currentIdleVariation = 0f;
                currentIdleVariationIndex = -1;
                isPlayingIdleVariation = false;

                // 🔄 Activar restauración de Turn/Look (si había valores guardados)
                if (Mathf.Abs(savedTurnBeforeVariation) > 0.01f || Mathf.Abs(savedLookBeforeVariation) > 0.01f)
                {
                    isRestoringTurnLook = true;
                }

                ResetIdleVariationTimer();
            }
        }
        else
        {
            // ⏱️ Está en idle normal - contar tiempo
            idleTimer += Time.deltaTime;

            // Cuando llega al tiempo objetivo, decidir si activar variation
            if (idleTimer >= nextIdleVariationTime)
            {
                // Tirar dado para ver si activa variation
                float randomChance = Random.Range(0f, 100f);

                if (randomChance <= idleVariationChance)
                {
                    // 💾 Guardar valores actuales de Turn/Look antes de activar la variation
                    savedTurnBeforeVariation = currentTurn;
                    savedLookBeforeVariation = currentLook;

                    // ✅ Seleccionar idle variation aleatoria (1 a numberOfIdleVariations)
                    int randomVariationNumber = Random.Range(1, numberOfIdleVariations + 1);
                    currentIdleVariation = (float)randomVariationNumber;
                    currentIdleVariationIndex = randomVariationNumber - 1; // Convertir a índice 0-based para el array
                    isPlayingIdleVariation = true;
                    idleVariationTimer = 0f;

                    // 🎬 CRUCIAL: Forzar al Animator a reiniciar el estado desde frame 0
                    // Esto previene que la animación empiece desde la mitad
                    if (!string.IsNullOrEmpty(idleStateNameInAnimator))
                    {
                        // Play el estado actual con normalizedTime = 0 (frame 0)
                        animator.Play(idleStateNameInAnimator, 0, 0f);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ idleStateNameInAnimator está vacío. La animación puede empezar desde la mitad. Configura el nombre del estado en el Inspector.");
                    }
                }
                else
                {
                    // ❌ No activar, resetear timer para intentar de nuevo
                    ResetIdleVariationTimer();
                }
            }
        }
    }

    /// <summary>
    /// Resetea el timer de idle variations con un tiempo aleatorio
    /// </summary>
    void ResetIdleVariationTimer()
    {
        idleTimer = 0f;
        nextIdleVariationTime = Random.Range(minIdleTimeBeforeVariation, maxIdleTimeBeforeVariation);
    }

    // ═══════════════════════════════════════════════════════════
    // ⚔️ SISTEMA DE ATAQUE
    // ═══════════════════════════════════════════════════════════

    void UpdateAttackSystem()
    {
        // Detectar enemigos en rango
        DetectEnemiesInRange();
        
        // Procesar buffer de ataque
        if (enableAttackBuffer && attackBuffered)
        {
            attackBufferTimer -= Time.deltaTime;
            if (attackBufferTimer <= 0f)
            {
                attackBuffered = false;
            }
            
            // Intentar ejecutar el ataque buffereado
            if (attackCooldownTimer <= 0f && !isAttacking)
            {
                ExecuteAttack();
                attackBuffered = false;
            }
        }
        
        // Timer del ataque actual
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                EndAttack();
            }
        }
		
		if (isCalling)
	{
		callTimer -= Time.deltaTime;

		// Evitar ataque mientras ruge
		isAttacking = false;

		// Cuando termina el rugido
		if (callTimer <= 0f)
		{
			isCalling = false;
		}
	}
        
        // Cooldown
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }
    
    void DetectEnemiesInRange()
    {
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1f;
        Collider[] nearbyEnemies = Physics.OverlapSphere(attackPosition, attackRange * 1.5f, enemyLayer);
        
        enemiesInRange = 0;
        foreach (Collider enemy in nearbyEnemies)
        {
            Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);
            
            if (angleToEnemy <= attackAngle / 2f)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy <= attackRange)
                {
                    enemiesInRange++;
                }
            }
        }
    }
    
    public void TryAttack()
    {
		if (isCalling) return;

		// 🍖 No atacar mientras come o bebe
		if (isEating || isDrinking) return;

        // ✅ Usar variable unificada lastGroundedTime
        bool canAttackNow = (controller.isGrounded || Time.time - lastGroundedTime < groundedTolerance || canAttackInAir)
                           && attackCooldownTimer <= 0f
                           && !isAttacking;

        if (canAttackNow)
        {
            ExecuteAttack();
        }
        else if (enableAttackBuffer)
        {
            attackBuffered = true;
            attackBufferTimer = attackBufferTime;
        }
    }
    
    void ExecuteAttack()
    {
        // 🌐 Llamar al RPC para sincronizar el ataque en todos los clientes
        photonView.RPC("RPC_ExecuteAttack", RpcTarget.All);
    }

    [PunRPC]
    void RPC_ExecuteAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;
        attackCooldownTimer = attackCooldown;
        currentState = MovementState.Attacking;

        // Limpiar lista de enemigos golpeados
        enemiesHit.Clear();

        // 🌐 ANIMACIÓN - Se ejecuta en TODOS los clientes para que todos vean el ataque
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsAttacking", true);
        }

        // Sonido (se ejecuta en todos los clientes)
        PlayAttackSound();

        // 🌐 Hacer daño SOLO en el cliente que atacó (el dueño)
        if (photonView.IsMine)
        {
            PerformAttackDamage();
        }
    }
    
    void PerformAttackDamage()
    {
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1f;

        // 🌐 Detectar enemigos en el área de ataque (incluyendo jugadores)
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, attackRange);

        foreach (Collider hit in hitColliders)
        {
            // No atacarse a sí mismo
            if (hit.gameObject == gameObject)
                continue;

            if (enemiesHit.Contains(hit.gameObject))
                continue;

            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            if (angleToTarget <= attackAngle / 2f)
            {
                enemiesHit.Add(hit.gameObject);

                // 🌐 Verificar si es un jugador (tiene PhotonView)
                PhotonView targetPhotonView = hit.GetComponent<PhotonView>();
                if (targetPhotonView != null)
                {
                    // Atacar a otro jugador a través de RPC
                    targetPhotonView.RPC("RPC_TakeDamage", RpcTarget.All, attackDamage, photonView.ViewID);
                }
                else
                {
                    // Atacar a NPC/enemigo normal
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(attackDamage);
                    }

                    // Para NPCs sin IDamageable, intentar HealthSystem directamente
                    HealthSystem healthSystem = hit.GetComponent<HealthSystem>();
                    if (healthSystem != null)
                    {
                        healthSystem.TakeDamage(attackDamage);
                    }
                }

                // Aplicar knockback
                if (attackKnockback > 0f)
                {
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 knockbackDirection = directionToTarget;
                        knockbackDirection.y = 0.3f;
                        rb.AddForce(knockbackDirection.normalized * attackKnockback, ForceMode.Impulse);
                    }
                }

                // Sonido de impacto
                PlayHitSound();
            }
        }
    }

    // 🌐 RPC para recibir daño de otros jugadores
    [PunRPC]
    void RPC_TakeDamage(float damage, int attackerViewID)
    {
        // Solo el dueño del jugador procesa el daño en su cliente
        if (photonView.IsMine)
        {
            HealthSystem healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null && !healthSystem.isDead)
            {
                healthSystem.TakeDamage(damage);
            }
        }
    }
    
    void EndAttack()
    {
        isAttacking = false;
        enemiesHit.Clear();
        
        if (controller.isGrounded)
        {
            currentState = inputVector.magnitude > movementThreshold ? 
                (isRunning ? MovementState.Run : MovementState.Walk) : MovementState.Idle;
        }
    }
    
	public void TryJump()
	{
		// 🍖 No saltar mientras come o bebe
		if (isEating || isDrinking) return;

		// ✅ FIX: Usar lastGroundedTime con tolerancia para evitar timing issues
		// El botón ya está deshabilitado correctamente en la UI
		bool isGroundedRecently = (Time.time - lastGroundedTime) <= 0.2f;

		if (!isGroundedRecently && !controller.isGrounded)
		{
			return;
		}
		if (!canJump) return;
		if (isAttacking) return;

		// Ejecutar salto
		DoJump();
	}
	
private void DoJump()
{
    // 🌐 Sincronizar salto en todos los clientes
    if (photonView != null)
    {
        photonView.RPC("RPC_DoJump", RpcTarget.All);
    }
    else
    {
        RPC_DoJump();
    }
}

[PunRPC]
void RPC_DoJump()
{
    velocity.y = Mathf.Sqrt(Mathf.Max(0.0001f, jumpHeight) * -2f * gravity);
    canJump = false;
    hasJumped = true;
    jumpCooldownTimer = jumpCooldown;

    // 🌐 ANIMACIÓN - Se ejecuta en TODOS los clientes
    if (animator != null)
    {
        // ⚠️ CRÍTICO: En jugadores remotos, forzar actualización de IsGrounded ANTES del trigger
        if (!photonView.IsMine)
        {
            animator.SetBool("IsGrounded", controller.isGrounded);
        }

        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");
        animator.SetFloat("VerticalSpeed", velocity.y);
    }

    // 🌐 SONIDO - Se ejecuta en todos los clientes
    PlayJumpSound();
}

void UpdateTimers()
{
    // ✅ Control del cooldown de salto - SIMPLIFICADO
    if (jumpCooldownTimer > 0f)
    {
        jumpCooldownTimer -= Time.deltaTime;

        // Cuando el cooldown termina, permitir saltar de nuevo
        if (jumpCooldownTimer <= 0f)
        {
            jumpCooldownTimer = 0f;
            canJump = true;  // ✅ Resetear aquí cuando el cooldown termine
        }
    }

    // Control del cooldown del ataque
    if (attackCooldownTimer > 0f)
    {
        attackCooldownTimer -= Time.deltaTime;
    }

    // Control del buffer de ataque (si lo tienes activo)
    if (enableAttackBuffer && attackBuffered)
    {
        attackBufferTimer -= Time.deltaTime;
        if (attackBufferTimer <= 0f)
        {
            attackBuffered = false;
        }
    }
}

    
    void UpdateState()
    {
        // 🌊 Estados de natación tienen prioridad
        if (isInWater)
        {
            if (isSwimming && currentSpeed > movementThreshold)
            {
                currentState = MovementState.Swimming;
            }
            else
            {
                currentState = MovementState.IdleSwim;
            }
        }
        else if (isAttacking)
        {
            currentState = MovementState.Attacking;
        }
        else if (!controller.isGrounded)
        {
            currentState = velocity.y > 0 ? MovementState.Jump : MovementState.Falling;
        }
        else if (currentSpeed > movementThreshold)
        {
            // Determinar el estado según crouch, run o walk
            if (isCrouching)
            {
                currentState = MovementState.Crouch;
            }
            else
            {
                currentState = isRunning ? MovementState.Run : MovementState.Walk;
            }
        }
        else
        {
            currentState = MovementState.Idle;
        }
    }
    
    void UpdateUI()
    {
        if (runButton != null)
        {
            ColorBlock colors = runButton.colors;
            colors.normalColor = isRunning ? Color.green : Color.white;
            runButton.colors = colors;
            // Ya no deshabilitamos el botón cuando está agachado
            // El usuario puede activar run mientras está agachado
        }

        if (crouchButton != null)
        {
            ColorBlock colors = crouchButton.colors;
            colors.normalColor = isCrouching ? Color.yellow : Color.white;
            crouchButton.colors = colors;
        }

        // ✅ DESHABILITAR BOTÓN DE SALTO cuando no se puede usar
        if (jumpButton != null)
        {
            bool canJumpNow = controller.isGrounded && canJump && !isAttacking;
            jumpButton.interactable = canJumpNow;

            // Cambiar color visual para feedback
            ColorBlock colors = jumpButton.colors;
            colors.normalColor = canJumpNow ? Color.white : Color.gray;
            jumpButton.colors = colors;
        }

        if (attackButton != null)
        {
            ColorBlock colors = attackButton.colors;
            if (isAttacking)
                colors.normalColor = Color.red;
            else if (attackCooldownTimer > 0)
                colors.normalColor = Color.gray;
            else if (enemiesInRange > 0)
                colors.normalColor = Color.yellow;
            else
                colors.normalColor = Color.white;
            attackButton.colors = colors;
        }
    }
    
    // Métodos de audio
	void PlayCallSound()
	{
		// Si ya está rugiendo o atacando, no permitir otro rugido
		if (isCalling || isAttacking) return;

		// 🍖 No rugir mientras come o bebe
		if (isEating || isDrinking) return;

		// 🌐 Sincronizar llamado en todos los clientes
		if (photonView != null)
		{
			photonView.RPC("RPC_PlayCall", RpcTarget.All);
		}
		else
		{
			RPC_PlayCall();
		}
	}

	[PunRPC]
	void RPC_PlayCall()
	{
		// Activar rugido
		isCalling = true;
		callTimer = callDuration;

		// 🌐 SONIDO - Se ejecuta en todos los clientes
		if (audioSource != null && callSounds.Length > 0)
		{
			AudioClip clip = callSounds[Random.Range(0, callSounds.Length)];
			audioSource.PlayOneShot(clip);
		}

		// 🌐 ANIMACIÓN - Se ejecuta en todos los clientes
		if (animator != null)
		{
			animator.ResetTrigger("Attack");
			animator.SetTrigger("Call");
			animator.SetBool("IsAttacking", false);
		}
	}

    
    void PlayJumpSound()
    {
        if (audioSource != null && jumpSounds.Length > 0)
        {
            AudioClip clip = jumpSounds[Random.Range(0, jumpSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    void PlayAttackSound()
    {
        if (audioSource != null && attackSounds.Length > 0)
        {
            AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    void PlayHitSound()
    {
        if (audioSource != null && hitSounds.Length > 0)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Métodos de pasos (llamados por eventos de animación)
    public void PlayFootstep()
    {
        if (audioSource != null)
        {
            AudioClip[] clips = isRunning ? runSounds : walkSounds;
            if (clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                audioSource.PlayOneShot(clip, 0.7f);
            }
        }
    }

    // 🔽 Método para pasos en crouch (llamado por eventos de animación de crouch)
    public void PlayCrouchFootstep()
    {
        if (audioSource != null)
        {
            if (crouchWalkSounds.Length > 0)
            {
                AudioClip clip = crouchWalkSounds[Random.Range(0, crouchWalkSounds.Length)];
                audioSource.PlayOneShot(clip, 0.6f); // Volumen un poco más bajo que walk
            }
            else
            {
                // Fallback: si no hay sonidos de crouch, usar walk sounds
                if (walkSounds.Length > 0)
                {
                    AudioClip clip = walkSounds[Random.Range(0, walkSounds.Length)];
                    audioSource.PlayOneShot(clip, 0.5f);
                }
            }
        }
    }

    public void PlayLandSound()
    {
        if (audioSource != null && landSounds.Length > 0)
        {
            AudioClip clip = landSounds[Random.Range(0, landSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showAttackGizmos) return;
        
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1f;
        
        // Gizmos de ataque
        Gizmos.color = isAttacking ? Color.red : (enemiesInRange > 0 ? Color.yellow : Color.cyan);
        Gizmos.DrawWireSphere(attackPosition, attackRange);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Vector3 forward = transform.forward;
            Vector3 right = Quaternion.Euler(0, attackAngle / 2f, 0) * forward;
            Vector3 left = Quaternion.Euler(0, -attackAngle / 2f, 0) * forward;
            
            Gizmos.DrawLine(transform.position, transform.position + right * attackRange);
            Gizmos.DrawLine(transform.position, transform.position + left * attackRange);
        }
        
        // Gizmos de ground detection
        if (Application.isPlaying)
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            Gizmos.color = controller != null && controller.isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 2f);
            
            if (isOnSlope)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(slopeHit.point, slopeHit.point + slopeHit.normal * 2f);
            }
            
            // Gizmos de alineación con terreno
            if (alignToTerrain)
            {
                // Mostrar normal suavizada del terreno
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, transform.position + smoothNormal * 3f);
                
                // Mostrar up vector del transform
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + transform.up * 2.5f);
            }
        }
        
        // Gizmos de dirección de movimiento
        if (Application.isPlaying && currentMoveDirection != Vector3.zero)
        {
            // Dirección actual
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, 
                           transform.position + Vector3.up * 0.5f + currentMoveDirection * 2f);
            
            // Dirección objetivo
            if (targetMoveDirection != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.7f, 
                               transform.position + Vector3.up * 0.7f + targetMoveDirection * 2f);
            }
        }

        // 🌊 Gizmos de agua
        if (Application.isPlaying && isInWater && waterCollider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 1f);

            // Línea de superficie del agua
            Gizmos.color = Color.blue;
            Vector3 surfacePos = new Vector3(transform.position.x, waterSurfaceY, transform.position.z);
            Gizmos.DrawLine(surfacePos + Vector3.left * 2f, surfacePos + Vector3.right * 2f);
            Gizmos.DrawLine(surfacePos + Vector3.forward * 2f, surfacePos + Vector3.back * 2f);
        }
    }

    // 🌊 SISTEMA DE DETECCIÓN DE AGUA
    void OnTriggerEnter(Collider other)
    {
        // Verificar si el collider está en el layer de agua
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            // Guardar referencia al collider del agua
            waterCollider = other;
            UpdateWaterSurface(other);

            // ✅ NO activar isInWater inmediatamente, esperar a verificar profundidad
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Verificar si el collider está en el layer de agua
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            // Actualizar altura de superficie del agua
            UpdateWaterSurface(other);

            // ✅ Calcular profundidad del agua
            float waterDepth = waterSurfaceY - transform.position.y;

            // 🔄 HISTÉRESIS: Usar diferentes umbrales para entrar/salir
            // Esto evita el parpadeo cuando el dinosaurio flota
            float enterThreshold = waterSurfaceOffset;
            float exitThreshold = waterSurfaceOffset * waterHysteresis;

            // ✅ Solo activar isInWater si está lo suficientemente profundo
            if (!isInWater && waterDepth >= enterThreshold)
            {
                // Entró a agua profunda
                isInWater = true;
                wasInWater = true;
            }
            // ✅ Solo desactivar si la profundidad baja significativamente (histéresis)
            else if (isInWater && waterDepth < exitThreshold)
            {
                // Salió a agua poco profunda (con tolerancia)
                isInWater = false;
                isSwimming = false;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Verificar si el collider está en el layer de agua
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            ExitWater();
        }
    }

    void UpdateWaterSurface(Collider water)
    {
        // Calcular la superficie del agua (parte superior del collider)
        Bounds bounds = water.bounds;
        waterSurfaceY = bounds.max.y;
    }

    void ExitWater()
    {
        isInWater = false;
        isSwimming = false;
        waterCollider = null;

        Debug.Log("🏖️ Dinosaurio salió del agua!");
    }

	// ═══════════════════════════════════════════════════════════
	// 🍖 SISTEMA DE HAMBRE, SED Y ESTAMINA
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Actualiza hambre, sed y estamina
	/// </summary>
	void UpdateHungerThirstStamina()
	{
		// 🍖 No actualizar si está comiendo o bebiendo (se maneja en sus métodos)
		if (!isEating && !isDrinking)
		{
			// Degradar hambre y sed constantemente
			currentHunger -= hungerDecayRate * Time.deltaTime;
			currentThirst -= thirstDecayRate * Time.deltaTime;

			// Limitar valores
			currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
			currentThirst = Mathf.Clamp(currentThirst, 0f, maxThirst);
		}

		// ⚡ Estamina - Consumo al correr (basado en velocidad real, no en el botón)
		if (isRunning && currentSpeed > movementThreshold && !isEating && !isDrinking)
		{
			currentStamina -= staminaDrainRate * Time.deltaTime;

			// Si se queda sin estamina, dejar de correr automáticamente
			if (currentStamina <= 0f)
			{
				currentStamina = 0f;
				isRunning = false;
			}
		}
		// ⚡ Estamina - Regeneración (cuando NO está corriendo activamente)
		// ✅ NUEVO: Regenera al caminar Y cuando está quieto (solo NO regenera al correr)
		else if (!isEating && !isDrinking)
		{
			// Verificar si está durmiendo para regenerar más rápido (usando referencia cacheada)
			// NOTA: Cuando está durmiendo, el DinosaurSleepSystem también maneja la regeneración
			// porque este script se desactiva durante el sueño
			float regenRate = (sleepSystemCache != null && sleepSystemCache.IsSleeping) ? staminaSleepRegenRate : staminaRegenRate;

			currentStamina += regenRate * Time.deltaTime;
		}

		// Limitar estamina
		currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
	}

	/// <summary>
	/// Detecta comida y agua cercana
	/// </summary>
	void DetectFoodAndWater()
	{
		// 🍗 Detectar comida cercana
		Collider[] foodColliders = Physics.OverlapSphere(transform.position, foodDetectionRange);
		nearbyFood = null;

		foreach (Collider col in foodColliders)
		{
			if (col.CompareTag("Food"))
			{
				nearbyFood = col.gameObject;
				break;
			}
		}

		// 💧 Detectar agua cercana
		Collider[] waterColliders = Physics.OverlapSphere(transform.position, foodDetectionRange);
		nearbyWater = null;

		foreach (Collider col in waterColliders)
		{
			if (col.CompareTag("Water"))
			{
				nearbyWater = col.gameObject;
				break;
			}
		}

		// 🖼️ Mostrar/ocultar botones según proximidad
		if (eatButton != null)
		{
			eatButton.gameObject.SetActive(nearbyFood != null && !isDrinking);
		}

		if (drinkButton != null)
		{
			drinkButton.gameObject.SetActive(nearbyWater != null && !isEating);
		}
	}

	/// <summary>
	/// Alternar comer
	/// </summary>
	public void ToggleEating()
	{
		if (isEating)
		{
			// Dejar de comer
			StopEating();
		}
		else
		{
			// Empezar a comer
			StartEating();
		}
	}

	/// <summary>
	/// Empezar a comer
	/// </summary>
	void StartEating()
	{
		if (nearbyFood == null || currentHunger >= maxHunger) return;

		// 🌐 Sincronizar animación de comer en todos los clientes
		if (photonView != null)
		{
			photonView.RPC("RPC_StartEating", RpcTarget.All);
		}
		else
		{
			RPC_StartEating();
		}
	}

	[PunRPC]
	void RPC_StartEating()
	{
		isEating = true;

		// 🌐 ANIMACIÓN - Se ejecuta en todos los clientes
		if (animator != null)
		{
			animator.SetBool("IsEating", true);
			animator.SetTrigger("Eat");
		}

		Debug.Log("🍖 Dinosaurio comenzó a comer");

		// Iniciar corrutina de comer (solo en el dueño)
		if (photonView == null || photonView.IsMine)
		{
			StartCoroutine(EatingCoroutine());
		}
	}

	/// <summary>
	/// Corrutina de comer
	/// </summary>
	System.Collections.IEnumerator EatingCoroutine()
	{
		while (isEating && currentHunger < maxHunger && nearbyFood != null)
		{
			// Aumentar hambre gradualmente
			currentHunger += eatingSpeed * Time.deltaTime;
			currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

			// Si se llenó, dejar de comer automáticamente
			if (currentHunger >= maxHunger)
			{
				Debug.Log("🍖 Dinosaurio está lleno!");
				StopEating();
				yield break;
			}

			yield return null;
		}

		// Si se alejó de la comida, dejar de comer
		if (nearbyFood == null)
		{
			StopEating();
		}
	}

	/// <summary>
	/// Dejar de comer
	/// </summary>
	void StopEating()
	{
		// 🌐 Sincronizar detener comer en todos los clientes
		if (photonView != null)
		{
			photonView.RPC("RPC_StopEating", RpcTarget.All);
		}
		else
		{
			RPC_StopEating();
		}
	}

	[PunRPC]
	void RPC_StopEating()
	{
		isEating = false;

		// 🌐 ANIMACIÓN - Se ejecuta en todos los clientes
		if (animator != null)
		{
			animator.SetBool("IsEating", false);
		}

		// Solo el dueño detiene la corrutina
		if (photonView == null || photonView.IsMine)
		{
			StopCoroutine(EatingCoroutine());
		}

		Debug.Log("🍖 Dinosaurio dejó de comer");
	}

	/// <summary>
	/// Alternar beber
	/// </summary>
	public void ToggleDrinking()
	{
		if (isDrinking)
		{
			// Dejar de beber
			StopDrinking();
		}
		else
		{
			// Empezar a beber
			StartDrinking();
		}
	}

	/// <summary>
	/// Empezar a beber
	/// </summary>
	void StartDrinking()
	{
		if (nearbyWater == null || currentThirst >= maxThirst) return;

		// 🌐 Sincronizar animación de beber en todos los clientes
		if (photonView != null)
		{
			photonView.RPC("RPC_StartDrinking", RpcTarget.All);
		}
		else
		{
			RPC_StartDrinking();
		}
	}

	[PunRPC]
	void RPC_StartDrinking()
	{
		isDrinking = true;

		// 🌐 ANIMACIÓN - Se ejecuta en todos los clientes
		if (animator != null)
		{
			animator.SetBool("IsDrinking", true);
			animator.SetTrigger("Drink");
		}

		Debug.Log("💧 Dinosaurio comenzó a beber");

		// Iniciar corrutina de beber (solo en el dueño)
		if (photonView == null || photonView.IsMine)
		{
			StartCoroutine(DrinkingCoroutine());
		}
	}

	/// <summary>
	/// Corrutina de beber
	/// </summary>
	System.Collections.IEnumerator DrinkingCoroutine()
	{
		while (isDrinking && currentThirst < maxThirst && nearbyWater != null)
		{
			// Aumentar sed gradualmente
			currentThirst += drinkingSpeed * Time.deltaTime;
			currentThirst = Mathf.Clamp(currentThirst, 0f, maxThirst);

			// Si se llenó, dejar de beber automáticamente
			if (currentThirst >= maxThirst)
			{
				Debug.Log("💧 Dinosaurio ya no tiene sed!");
				StopDrinking();
				yield break;
			}

			yield return null;
		}

		// Si se alejó del agua, dejar de beber
		if (nearbyWater == null)
		{
			StopDrinking();
		}
	}

	/// <summary>
	/// Dejar de beber
	/// </summary>
	void StopDrinking()
	{
		// 🌐 Sincronizar detener beber en todos los clientes
		if (photonView != null)
		{
			photonView.RPC("RPC_StopDrinking", RpcTarget.All);
		}
		else
		{
			RPC_StopDrinking();
		}
	}

	[PunRPC]
	void RPC_StopDrinking()
	{
		isDrinking = false;

		// 🌐 ANIMACIÓN - Se ejecuta en todos los clientes
		if (animator != null)
		{
			animator.SetBool("IsDrinking", false);
		}

		// Solo el dueño detiene la corrutina
		if (photonView == null || photonView.IsMine)
		{
			StopCoroutine(DrinkingCoroutine());
		}

		Debug.Log("💧 Dinosaurio dejó de beber");
	}

	/// <summary>
	/// Método público para obtener hambre actual (usado por HealthSystem)
	/// </summary>
	public float GetCurrentHunger()
	{
		return currentHunger;
	}

	/// <summary>
	/// Método público para obtener sed actual (usado por HealthSystem)
	/// </summary>
	public float GetCurrentThirst()
	{
		return currentThirst;
	}

	/// <summary>
	/// Método público para verificar si está comiendo o bebiendo
	/// </summary>
	public bool IsEatingOrDrinking()
	{
		return isEating || isDrinking;
	}

	// ═══════════════════════════════════════════════════════════
	// 📊 SISTEMA DE BARRAS UI
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Actualiza las barras UI de hambre, sed y estamina
	/// </summary>
	void UpdateStatsUI()
	{
		// Actualizar barra de hambre
		if (hungerBar != null)
		{
			hungerBar.fillAmount = currentHunger / maxHunger;
		}

		// Actualizar barra de sed
		if (thirstBar != null)
		{
			thirstBar.fillAmount = currentThirst / maxThirst;
		}

		// Actualizar barra de estamina
		if (staminaBar != null)
		{
			staminaBar.fillAmount = currentStamina / maxStamina;
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 💀 SISTEMA DE MUERTE
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Método público para matar al dinosaurio (llamado por HealthSystem)
	/// </summary>
	public void Die()
	{
		if (isDead) return;

		// 🌐 Sincronizar muerte en todos los clientes
		photonView.RPC("RPC_Die", RpcTarget.All);
	}

	[PunRPC]
	void RPC_Die()
	{
		if (isDead) return;

		isDead = true;

		Debug.Log("💀 Dinosaurio ha muerto!");

		// Detener todas las corrutinas activas
		StopAllCoroutines();

		// Detener estados
		isEating = false;
		isDrinking = false;
		isAttacking = false;
		isRunning = false;
		isCalling = false;

		// Activar animación de muerte (visible para todos)
		if (animator != null)
		{
			animator.SetBool("IsDead", true);
			animator.SetTrigger("Death");

			// Desactivar otros parámetros
			animator.SetBool("IsEating", false);
			animator.SetBool("IsDrinking", false);
			animator.SetBool("IsAttacking", false);
			animator.SetBool("IsRunning", false);
			animator.SetFloat("Speed", 0f);
			animator.SetFloat("MoveX", 0f);
			animator.SetFloat("MoveZ", 0f);
		}

		// Desactivar el controlador de movimiento
		if (controller != null)
		{
			controller.enabled = false;
		}

		// Desactivar este script
		this.enabled = false;
	}

	// 🌐 PHOTON: Sincronización OPTIMIZADA de datos personalizados
	// SOLO envía datos que han CAMBIADO para minimizar tráfico de red
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			// ========================================
			// ENVIAMOS DATOS (somos el dueño)
			// ========================================

			// 1. POSICIÓN Y ROTACIÓN (siempre necesarias para interpolación)
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation);

			// 2. VELOCIDAD (para predicción de movimiento)
			stream.SendNext(controller.velocity);

			// 3. VELOCIDAD DE MOVIMIENTO (para animaciones)
			stream.SendNext(currentSpeed);

			// 4. FLAGS DE BITS (comprimir booleanos en un solo byte)
			// Usar bits para reducir 8 booleanos a 1 byte
			byte flags = 0;
			if (isRunning) flags |= 1 << 0;           // Bit 0
			if (isCrouching) flags |= 1 << 1;         // Bit 1
			if (isSwimming) flags |= 1 << 2;          // Bit 2
			if (isInWater) flags |= 1 << 3;           // Bit 3
			if (isAttacking) flags |= 1 << 4;         // Bit 4
			if (controller.isGrounded) flags |= 1 << 5; // Bit 5
			if (isDead) flags |= 1 << 6;              // Bit 6
			if (isCalling) flags |= 1 << 7;           // Bit 7

			stream.SendNext(flags);

			// Segundo byte de flags para estados adicionales
			byte flags2 = 0;
			if (isEating) flags2 |= 1 << 0;           // Bit 0
			if (isDrinking) flags2 |= 1 << 1;         // Bit 1
			stream.SendNext(flags2);

			// 5. ESTADO ACTUAL (necesario para animaciones)
			stream.SendNext((byte)currentState);

			// 6. PARÁMETROS DEL ANIMATOR - ENVIAR VARIABLES DIRECTAS (no leer del Animator)
			// ⚠️ CRÍTICO: Enviar las variables originales, NO leer del Animator
			// porque el Animator puede tener valores desactualizados o interpolados

			// Speed normalizado (igual que en UpdateAnimations)
			float normalizedSpeed;
			if (isPlayingIdleVariation)
			{
				normalizedSpeed = 0f;
			}
			else
			{
				normalizedSpeed = isInWater ? (currentSpeed / swimSpeed) : (currentSpeed / runSpeed);
			}
			stream.SendNext(normalizedSpeed);

			// MoveX y MoveZ (igual que en UpdateAnimations)
			Vector3 localMove;
			if (isPlayingIdleVariation || inputVector.magnitude <= 0.1f)
			{
				localMove = Vector3.zero;
			}
			else
			{
				localMove = transform.InverseTransformDirection(currentMoveDirection);
			}
			stream.SendNext(localMove.x); // MoveX
			stream.SendNext(localMove.z); // MoveZ

			// VerticalSpeed (velocidad Y)
			stream.SendNext(velocity.y);

			// Turn y Look (valores directos de las variables)
			stream.SendNext(currentTurn);
			stream.SendNext(currentLook);

			// IdleVariation
			stream.SendNext(currentIdleVariation);
		}
		else
		{
			// ========================================
			// RECIBIMOS DATOS (jugador remoto)
			// ========================================
			try
			{
				// 1. POSICIÓN Y ROTACIÓN
				networkPosition = (Vector3)stream.ReceiveNext();
				networkRotation = (Quaternion)stream.ReceiveNext();

				// 2. VELOCIDAD (para predicción)
				networkVelocity = (Vector3)stream.ReceiveNext();

				// 3. VELOCIDAD DE MOVIMIENTO
				networkSpeed = (float)stream.ReceiveNext();

				// 4. FLAGS DE BITS (descomprimir)
				byte flags = (byte)stream.ReceiveNext();
				isRunning = (flags & (1 << 0)) != 0;
				isCrouching = (flags & (1 << 1)) != 0;
				isSwimming = (flags & (1 << 2)) != 0;
				isInWater = (flags & (1 << 3)) != 0;
				isAttacking = (flags & (1 << 4)) != 0;
				bool isGrounded = (flags & (1 << 5)) != 0;
				isDead = (flags & (1 << 6)) != 0;
				isCalling = (flags & (1 << 7)) != 0;

				// Segundo byte de flags
				byte flags2 = (byte)stream.ReceiveNext();
				isEating = (flags2 & (1 << 0)) != 0;
				isDrinking = (flags2 & (1 << 1)) != 0;

				// 5. ESTADO ACTUAL (convertir con cast explícito)
				byte stateByte = (byte)stream.ReceiveNext();
				currentState = (MovementState)stateByte;

				// 6. PARÁMETROS DEL ANIMATOR
				float animSpeed = (float)stream.ReceiveNext();
				float moveX = (float)stream.ReceiveNext();
				float moveZ = (float)stream.ReceiveNext();
				float verticalSpeed = (float)stream.ReceiveNext();
				float turn = (float)stream.ReceiveNext();
				float look = (float)stream.ReceiveNext();
				float idleVariation = (float)stream.ReceiveNext();

				// Actualizar valor local de idle variation
				currentIdleVariation = idleVariation;
				isPlayingIdleVariation = idleVariation > 0.1f;

				// 7. ACTUALIZAR ANIMATOR (CRÍTICO para ver animaciones)
				if (animator != null)
				{
					// Actualizar SIEMPRE para que los blend trees funcionen correctamente
					animator.SetFloat("Speed", animSpeed);
					animator.SetFloat("MoveX", moveX);
					animator.SetFloat("MoveZ", moveZ);
					animator.SetFloat("VerticalSpeed", verticalSpeed);
					animator.SetFloat("Turn", turn);
					animator.SetFloat("Look", look);
					animator.SetFloat("IdleVariation", idleVariation);

					// Booleanos (actualizar siempre)
					animator.SetBool("IsRunning", isRunning);
					animator.SetBool("IsCrouching", isCrouching);
					animator.SetBool("IsInWater", isInWater);  // ⚠️ CRÍTICO para natación
					animator.SetBool("IsSwimming", isSwimming);
					animator.SetBool("IsGrounded", isGrounded);
					animator.SetBool("IsAttacking", isAttacking);
					animator.SetBool("IsDead", isDead);
					animator.SetBool("IsEating", isEating);
					animator.SetBool("IsDrinking", isDrinking);
				}

				// 8. GUARDAR TIMESTAMP para predicción
				lastReceiveTime = info.SentServerTime;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"ERROR en OnPhotonSerializeView (recepción): {e.Message}");
			}
		}
	}
}

// Interface para objetos que pueden recibir daño
public interface IDamageable
{
    void TakeDamage(float damage);
}
