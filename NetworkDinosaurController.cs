using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Controlador de Dinosaurio para Photon Fusion - ULTRA OPTIMIZADO
/// ✅ Sincronización mínima de datos
/// ✅ Predicción del lado del cliente
/// ✅ Animaciones basadas en estado
/// ✅ Sistema de combate PvP
/// ⚡ OPTIMIZACIÓN: Solo ~20 bytes por tick
/// </summary>
public class NetworkDinosaurController : NetworkBehaviour
{
    [Header("Referencias Locales")]
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
    public Button eatButton;
    public Button drinkButton;

    [Header("📏 Configuración de Tamaño")]
    public SimpleDinosaurController.DinosaurSize dinosaurSize = SimpleDinosaurController.DinosaurSize.Medium;
    public float modelHeight = 2.5f;

    [Header("Velocidades")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float crouchSpeed = 1f;
    public float swimSpeed = 3f;
    public float turnSpeed = 120f;

    [Header("⚔️ Sistema de Ataque")]
    public float attackDamage = 25f;
    public float attackRange = 2f;
    public float attackAngle = 90f;
    public float attackCooldown = 0.5f;
    public float attackDuration = 0.5f;
    public LayerMask enemyLayer;
    public Transform attackPoint;

    [Header("⭐ Salto y Gravedad")]
    public float jumpHeight = 2f;
    public float jumpCooldown = 1f;
    public float gravity = -20f;
    public float terminalVelocity = -50f;

    [Header("🍖 Sistema de Hambre/Sed/Estamina")]
    [Range(0f, 200f)]
    public float maxHunger = 100f;
    [Range(0f, 200f)]
    public float maxThirst = 100f;
    [Range(0f, 200f)]
    public float maxStamina = 100f;

    public float hungerDecayRate = 0.5f;
    public float thirstDecayRate = 0.7f;
    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 5f;
    public float staminaSleepRegenRate = 15f;

    [Header("📊 Barras UI")]
    public Image hungerBar;
    public Image thirstBar;
    public Image staminaBar;

    [Header("Audio")]
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] jumpSounds;

    // ═══════════════════════════════════════════════════════════
    // 🌐 VARIABLES DE RED - ULTRA COMPRIMIDAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estado comprimido del movimiento
    /// ⚡ OPTIMIZACIÓN: byte = 8 bits = 256 estados posibles
    /// 0 = Idle, 1 = Walk, 2 = Run, 3 = Crouch, 4 = Jump, 5 = Falling, 6 = Attacking, 7 = Swimming, 8 = IdleSwim
    /// </summary>
    [Networked]
    public byte MovementState { get; set; }

    /// <summary>
    /// Velocidad normalizada (0-1)
    /// ⚡ OPTIMIZACIÓN: byte (0-255) convertido a 0.0-1.0
    /// </summary>
    [Networked]
    public byte NormalizedSpeed { get; set; }

    /// <summary>
    /// Hambre actual (0-200)
    /// ⚡ OPTIMIZACIÓN: Sincronizada solo cada tick
    /// </summary>
    [Networked]
    public float CurrentHunger { get; set; }

    /// <summary>
    /// Sed actual (0-200)
    /// </summary>
    [Networked]
    public float CurrentThirst { get; set; }

    /// <summary>
    /// Estamina actual (0-200)
    /// </summary>
    [Networked]
    public float CurrentStamina { get; set; }

    /// <summary>
    /// Flags de estado (múltiples bools en un byte)
    /// ⚡ OPTIMIZACIÓN: 8 bools en 1 byte
    /// Bit 0: isRunning
    /// Bit 1: isCrouching
    /// Bit 2: isAttacking
    /// Bit 3: isInWater
    /// Bit 4: isSwimming
    /// Bit 5: isDead
    /// Bit 6: isEating
    /// Bit 7: isDrinking
    /// </summary>
    [Networked]
    public byte StateFlags { get; set; }

    /// <summary>
    /// Tick del último ataque (para cooldown)
    /// </summary>
    [Networked]
    public int LastAttackTick { get; set; }

    // Propiedades helper para acceder a los state flags
    public bool IsRunning
    {
        get => (StateFlags & 0x01) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x01) : (byte)(StateFlags & ~0x01);
    }

    public bool IsCrouching
    {
        get => (StateFlags & 0x02) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x02) : (byte)(StateFlags & ~0x02);
    }

    public bool IsAttacking
    {
        get => (StateFlags & 0x04) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x04) : (byte)(StateFlags & ~0x04);
    }

    public bool IsInWater
    {
        get => (StateFlags & 0x08) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x08) : (byte)(StateFlags & ~0x08);
    }

    public bool IsSwimming
    {
        get => (StateFlags & 0x10) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x10) : (byte)(StateFlags & ~0x10);
    }

    public bool IsDead
    {
        get => (StateFlags & 0x20) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x20) : (byte)(StateFlags & ~0x20);
    }

    public bool IsEating
    {
        get => (StateFlags & 0x40) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x40) : (byte)(StateFlags & ~0x40);
    }

    public bool IsDrinking
    {
        get => (StateFlags & 0x80) != 0;
        set => StateFlags = value ? (byte)(StateFlags | 0x80) : (byte)(StateFlags & ~0x80);
    }

    // Variables locales (no sincronizadas)
    private CharacterController controller;
    private NetworkHealthSystem healthSystem;

    private Vector3 inputVector;
    private Vector3 velocity;
    private float currentSpeed = 0f;

    private float attackTimer = 0f;
    private List<NetworkObject> enemiesHit = new List<NetworkObject>();

    // Variables para comer/beber
    private GameObject nearbyFood = null;
    private GameObject nearbyWater = null;
    public float foodDetectionRange = 3f;
    public float eatingSpeed = 15f;
    public float drinkingSpeed = 20f;

    // Para estados públicos compatibles con otros scripts
    public bool isInWater { get => IsInWater; set => IsInWater = value; }
    public bool isSwimming { get => IsSwimming; set => IsSwimming = value; }
    public bool isAttacking { get => IsAttacking; set => IsAttacking = value; }

    // ═══════════════════════════════════════════════════════════
    // 🎮 INICIALIZACIÓN
    // ═══════════════════════════════════════════════════════════

    public override void Spawned()
    {
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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        healthSystem = GetComponent<NetworkHealthSystem>();

        // Solo inicializar valores para el servidor
        if (Object.HasStateAuthority)
        {
            CurrentHunger = maxHunger;
            CurrentThirst = maxThirst;
            CurrentStamina = maxStamina;
            MovementState = 0; // Idle
            NormalizedSpeed = 0;
            StateFlags = 0;
        }

        // Solo el jugador local necesita configurar botones y cámara
        if (Object.HasInputAuthority)
        {
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    cameraTransform = mainCam.transform;
            }

            SetupButtonListeners();

            // Ocultar botones inicialmente
            if (eatButton != null)
                eatButton.gameObject.SetActive(false);
            if (drinkButton != null)
                drinkButton.gameObject.SetActive(false);
        }
        else
        {
            // Desactivar controles para otros jugadores
            if (movementJoystick != null)
                movementJoystick.gameObject.SetActive(false);
        }

        if (attackPoint == null)
            attackPoint = transform;
    }

    // ═══════════════════════════════════════════════════════════
    // 🎮 INPUT Y LÓGICA DE RED
    // ═══════════════════════════════════════════════════════════

    public override void FixedUpdateNetwork()
    {
        // Solo el servidor actualiza hambre/sed/estamina
        if (Object.HasStateAuthority)
        {
            UpdateHungerThirstStamina();
        }

        // Solo el cliente con input authority procesa input
        if (GetInput(out NetworkInputData input))
        {
            ProcessInput(input);
        }

        // Aplicar movimiento
        ApplyMovement();

        // Actualizar timers
        UpdateTimers();

        // Actualizar estado
        UpdateState();
    }

    public override void Render()
    {
        // Actualizar animaciones localmente (más suave)
        UpdateAnimations();

        // Actualizar UI (solo jugador local)
        if (Object.HasInputAuthority)
        {
            UpdateStatsUI();
            DetectFoodAndWater();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🎯 PROCESAMIENTO DE INPUT
    // ═══════════════════════════════════════════════════════════

    void ProcessInput(NetworkInputData input)
    {
        if (IsDead) return;

        inputVector = input.movementInput;

        // Botones de estado
        if (input.runButton && !IsEating && !IsDrinking)
        {
            IsRunning = !IsRunning;
        }

        if (input.crouchButton && !IsEating && !IsDrinking)
        {
            IsCrouching = !IsCrouching;
        }

        // Salto
        if (input.jumpButton && controller.isGrounded && !IsAttacking && !IsEating && !IsDrinking)
        {
            DoJump();
        }

        // Ataque
        if (input.attackButton && !IsEating && !IsDrinking)
        {
            TryAttack();
        }

        // Comer/Beber
        if (input.eatButton)
        {
            ToggleEating();
        }

        if (input.drinkButton)
        {
            ToggleDrinking();
        }

        // Calcular movimiento
        CalculateMovement();
    }

    // ═══════════════════════════════════════════════════════════
    // 🏃 SISTEMA DE MOVIMIENTO
    // ═══════════════════════════════════════════════════════════

    void CalculateMovement()
    {
        if (IsEating || IsDrinking)
        {
            currentSpeed = 0f;
            return;
        }

        if (inputVector.magnitude > 0.1f)
        {
            // Determinar velocidad objetivo
            float targetSpeed;
            if (IsInWater && IsSwimming)
            {
                targetSpeed = swimSpeed;
            }
            else if (IsCrouching)
            {
                targetSpeed = crouchSpeed;
            }
            else
            {
                targetSpeed = IsRunning ? runSpeed : walkSpeed;
            }

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Runner.DeltaTime * 10f);

            // Calcular dirección de movimiento relativa a la cámara
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : transform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * inputVector.z + cameraRight * inputVector.x).normalized;

            // Rotar hacia la dirección de movimiento
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * turnSpeed / 60f);
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Runner.DeltaTime * 10f);
        }

        // Actualizar velocidad normalizada para sincronización
        NormalizedSpeed = (byte)(Mathf.Clamp01(currentSpeed / runSpeed) * 255);
    }

    void ApplyMovement()
    {
        // Movimiento horizontal
        Vector3 move = transform.forward * currentSpeed;

        // Gravedad
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (IsInWater && IsSwimming)
        {
            // Flotación en agua
            velocity.y = Mathf.Lerp(velocity.y, 0f, Runner.DeltaTime * 5f);
        }
        else
        {
            velocity.y += gravity * Runner.DeltaTime;
            velocity.y = Mathf.Max(velocity.y, terminalVelocity);
        }

        Vector3 finalMove = move + velocity;
        controller.Move(finalMove * Runner.DeltaTime);
    }

    void DoJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        PlayJumpSound();
    }

    // ═══════════════════════════════════════════════════════════
    // ⚔️ SISTEMA DE COMBATE PvP
    // ═══════════════════════════════════════════════════════════

    void TryAttack()
    {
        // Verificar cooldown
        int ticksSinceLastAttack = Runner.Tick - LastAttackTick;
        float timeSinceLastAttack = ticksSinceLastAttack * Runner.DeltaTime;

        if (timeSinceLastAttack < attackCooldown) return;
        if (IsAttacking) return;

        ExecuteAttack();
    }

    void ExecuteAttack()
    {
        IsAttacking = true;
        attackTimer = attackDuration;
        LastAttackTick = Runner.Tick;

        enemiesHit.Clear();

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        PlayAttackSound();

        // Realizar ataque inmediatamente
        PerformAttackDamage();
    }

    void PerformAttackDamage()
    {
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1f;

        // Detectar enemigos en el área de ataque
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, attackRange, enemyLayer);

        foreach (Collider hit in hitColliders)
        {
            // Ignorar a sí mismo
            if (hit.gameObject == gameObject) continue;

            NetworkObject hitNetworkObject = hit.GetComponent<NetworkObject>();
            if (hitNetworkObject == null) continue;

            // Evitar golpear dos veces al mismo enemigo
            if (enemiesHit.Contains(hitNetworkObject)) continue;

            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            if (angleToTarget <= attackAngle / 2f)
            {
                enemiesHit.Add(hitNetworkObject);

                // Aplicar daño al sistema de salud
                NetworkHealthSystem targetHealth = hit.GetComponent<NetworkHealthSystem>();
                if (targetHealth != null && targetHealth.IsAlive())
                {
                    // Enviar daño con referencia al atacante
                    targetHealth.TakeDamage(attackDamage, Object.InputAuthority);

                    Debug.Log($"🗡️ {gameObject.name} atacó a {hit.gameObject.name} por {attackDamage} de daño!");
                }

                PlayHitSound();
            }
        }
    }

    void UpdateTimers()
    {
        if (IsAttacking)
        {
            attackTimer -= Runner.DeltaTime;
            if (attackTimer <= 0f)
            {
                IsAttacking = false;
                enemiesHit.Clear();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🍖 SISTEMA DE HAMBRE/SED/ESTAMINA
    // ═══════════════════════════════════════════════════════════

    void UpdateHungerThirstStamina()
    {
        if (!IsEating && !IsDrinking)
        {
            CurrentHunger -= hungerDecayRate * Runner.DeltaTime;
            CurrentThirst -= thirstDecayRate * Runner.DeltaTime;

            CurrentHunger = Mathf.Clamp(CurrentHunger, 0f, maxHunger);
            CurrentThirst = Mathf.Clamp(CurrentThirst, 0f, maxThirst);
        }

        // Consumo de estamina al correr
        if (IsRunning && currentSpeed > 0.2f && !IsEating && !IsDrinking)
        {
            CurrentStamina -= staminaDrainRate * Runner.DeltaTime;

            if (CurrentStamina <= 0f)
            {
                CurrentStamina = 0f;
                IsRunning = false;
            }
        }
        else if (!IsEating && !IsDrinking)
        {
            CurrentStamina += staminaRegenRate * Runner.DeltaTime;
        }

        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maxStamina);
    }

    void DetectFoodAndWater()
    {
        // Detectar comida cercana
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

        // Detectar agua cercana
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

        // Mostrar/ocultar botones
        if (eatButton != null)
        {
            eatButton.gameObject.SetActive(nearbyFood != null && !IsDrinking);
        }

        if (drinkButton != null)
        {
            drinkButton.gameObject.SetActive(nearbyWater != null && !IsEating);
        }
    }

    void ToggleEating()
    {
        if (IsEating)
        {
            StopEating();
        }
        else
        {
            StartEating();
        }
    }

    void StartEating()
    {
        if (nearbyFood == null || CurrentHunger >= maxHunger) return;

        IsEating = true;

        if (animator != null)
        {
            animator.SetBool("IsEating", true);
            animator.SetTrigger("Eat");
        }

        Debug.Log("🍖 Comenzó a comer");
    }

    void StopEating()
    {
        IsEating = false;

        if (animator != null)
        {
            animator.SetBool("IsEating", false);
        }

        Debug.Log("🍖 Dejó de comer");
    }

    void ToggleDrinking()
    {
        if (IsDrinking)
        {
            StopDrinking();
        }
        else
        {
            StartDrinking();
        }
    }

    void StartDrinking()
    {
        if (nearbyWater == null || CurrentThirst >= maxThirst) return;

        IsDrinking = true;

        if (animator != null)
        {
            animator.SetBool("IsDrinking", true);
            animator.SetTrigger("Drink");
        }

        Debug.Log("💧 Comenzó a beber");
    }

    void StopDrinking()
    {
        IsDrinking = false;

        if (animator != null)
        {
            animator.SetBool("IsDrinking", false);
        }

        Debug.Log("💧 Dejó de beber");
    }

    // ═══════════════════════════════════════════════════════════
    // 🎭 ANIMACIONES (Basadas en estado)
    // ═══════════════════════════════════════════════════════════

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Velocidad normalizada (0-1)
        float speed = NormalizedSpeed / 255f;
        animator.SetFloat("Speed", speed);

        // Estados
        animator.SetBool("IsGrounded", controller.isGrounded && !IsInWater);
        animator.SetBool("IsRunning", IsRunning && !IsInWater);
        animator.SetBool("IsCrouching", IsCrouching && !IsInWater);
        animator.SetBool("IsAttacking", IsAttacking);
        animator.SetBool("IsInWater", IsInWater);
        animator.SetBool("IsSwimming", IsSwimming);
        animator.SetBool("IsEating", IsEating);
        animator.SetBool("IsDrinking", IsDrinking);
        animator.SetFloat("VerticalSpeed", velocity.y);

        // Dirección de movimiento local
        if (inputVector.magnitude > 0.1f)
        {
            Vector3 localMove = transform.InverseTransformDirection(transform.forward);
            animator.SetFloat("MoveX", localMove.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveZ", localMove.z, 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("MoveX", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveZ", 0f, 0.1f, Time.deltaTime);
        }
    }

    void UpdateState()
    {
        if (IsInWater)
        {
            if (IsSwimming && currentSpeed > 0.2f)
            {
                MovementState = 7; // Swimming
            }
            else
            {
                MovementState = 8; // IdleSwim
            }
        }
        else if (IsAttacking)
        {
            MovementState = 6; // Attacking
        }
        else if (!controller.isGrounded)
        {
            MovementState = velocity.y > 0 ? (byte)4 : (byte)5; // Jump : Falling
        }
        else if (currentSpeed > 0.2f)
        {
            if (IsCrouching)
            {
                MovementState = 3; // Crouch
            }
            else
            {
                MovementState = IsRunning ? (byte)2 : (byte)1; // Run : Walk
            }
        }
        else
        {
            MovementState = 0; // Idle
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 📊 UI
    // ═══════════════════════════════════════════════════════════

    void UpdateStatsUI()
    {
        if (hungerBar != null)
        {
            hungerBar.fillAmount = CurrentHunger / maxHunger;
        }

        if (thirstBar != null)
        {
            thirstBar.fillAmount = CurrentThirst / maxThirst;
        }

        if (staminaBar != null)
        {
            staminaBar.fillAmount = CurrentStamina / maxStamina;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🎵 AUDIO
    // ═══════════════════════════════════════════════════════════

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

    void PlayJumpSound()
    {
        if (audioSource != null && jumpSounds.Length > 0)
        {
            AudioClip clip = jumpSounds[Random.Range(0, jumpSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 💀 MUERTE
    // ═══════════════════════════════════════════════════════════

    public void Die()
    {
        if (IsDead) return;

        IsDead = true;

        Debug.Log("💀 Dinosaurio ha muerto!");

        IsEating = false;
        IsDrinking = false;
        IsAttacking = false;
        IsRunning = false;

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Death");
            animator.SetBool("IsEating", false);
            animator.SetBool("IsDrinking", false);
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsRunning", false);
            animator.SetFloat("Speed", 0f);
        }

        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🛠️ SETUP
    // ═══════════════════════════════════════════════════════════

    void SetupButtonListeners()
    {
        if (runButton != null)
        {
            runButton.onClick.RemoveAllListeners();
            runButton.onClick.AddListener(() => {
                // El input se procesará en GetInput
            });
        }

        if (crouchButton != null)
        {
            crouchButton.onClick.RemoveAllListeners();
            crouchButton.onClick.AddListener(() => {
                // El input se procesará en GetInput
            });
        }

        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(() => {
                // El input se procesará en GetInput
            });
        }

        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => {
                // El input se procesará en GetInput
            });
        }

        if (eatButton != null)
        {
            eatButton.onClick.RemoveAllListeners();
            eatButton.onClick.AddListener(() => {
                // El input se procesará en GetInput
            });
        }

        if (drinkButton != null)
        {
            drinkButton.onClick.RemoveAllListeners();
            drinkButton.onClick.AddListener(() => {
                // El input se procesará en GetInput
            });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🔧 UTILIDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════

    public float GetCurrentHunger()
    {
        return CurrentHunger;
    }

    public float GetCurrentThirst()
    {
        return CurrentThirst;
    }

    public bool IsEatingOrDrinking()
    {
        return IsEating || IsDrinking;
    }
}

/// <summary>
/// Estructura de datos de input para Photon Fusion
/// ⚡ OPTIMIZACIÓN: ~12 bytes por tick
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector3 movementInput;  // 12 bytes
    public NetworkBool jumpButton;      // 1 bit
    public NetworkBool attackButton;    // 1 bit
    public NetworkBool runButton;       // 1 bit
    public NetworkBool crouchButton;    // 1 bit
    public NetworkBool eatButton;       // 1 bit
    public NetworkBool drinkButton;     // 1 bit
    // Total: ~13 bytes
}
