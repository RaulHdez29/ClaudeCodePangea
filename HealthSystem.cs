using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Photon.Pun;
using Photon.Realtime;

// SISTEMA DE SALUD
// Agregar a cualquier objeto que pueda recibir daño
// 🌐 ADAPTADO A PHOTON PUN2 - La vida es LOCAL, pero la muerte se sincroniza

public class HealthSystem : MonoBehaviourPunCallbacks
{
    [Header("🌐 Photon PUN2")]
    private PhotonView photonView;

    [Header("Configuración de Salud")]
    [Tooltip("Vida máxima (100% = este valor)")]
    public float maxHealth = 200f;
    [Tooltip("Vida actual (SOLO LOCAL - cada jugador ve solo su vida)")]
    public float currentHealth = 200f;
    [Tooltip("Puede morir")]
    public bool canDie = true;
    [Tooltip("Destruir objeto al morir")]
    public bool destroyOnDeath = true;
    [Tooltip("Tiempo antes de destruir (segundos)")]
    public float destroyDelay = 2f;

	[Header("🍖 Sistema de Hambre/Sed")]
	[Tooltip("Referencia al controlador del dinosaurio")]
	public SimpleDinosaurController dinosaurController;
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
    [Tooltip("Material de daño (opcional)")]
    public Material damageMaterial;
    [Tooltip("Duración del efecto de daño")]
    public float damageMaterialDuration = 0.2f;

	[Header("🩸 UI de Daño Visual")]
	[Tooltip("Imagen UI para mostrar daño visual (sangre)")]
	public Image damageOverlayImage;
	[Tooltip("Sprites de daño por nivel (índice 0 = 90%, 1 = 80%, ..., 8 = 10%)")]
	public Sprite[] damageSprites = new Sprite[9];
	private int currentDamageLevel = -1;

	[Header("💀 UI de Muerte")]
	[Tooltip("Panel que se muestra cuando el dinosaurio muere")]
	public GameObject deathPanel;

	[Header("🩸 Sistema de Daño Visual en Shader")]
	[Tooltip("Activar sistema de daño visual en el shader")]
	public bool enableShaderDamage = true;
	[Tooltip("Nombre del parámetro del shader para el daño (por defecto: _DamageAmount)")]
	public string shaderDamageParameter = "_DamageAmount";
	[Tooltip("Porcentaje de vida donde empieza a mostrarse daño visual (0-100%)")]
	[Range(0f, 100f)]
	public float damageVisualThreshold = 90f;
	[Tooltip("Intensidad máxima del daño visual (0-1)")]
	[Range(0f, 1f)]
	public float maxDamageIntensity = 1f;
	[Tooltip("Curva de progresión del daño (más pronunciado = daño aparece más rápido)")]
	[Range(1f, 3f)]
	public float damageCurve = 1.5f;

    [Header("Audio")]
    public AudioClip[] hurtSounds;
    public AudioClip[] deathSounds;
    public AudioSource audioSource;
    
    [Header("Estado")]
    public bool isDead = false;
    public bool isInvulnerable = false;
    
    [Header("Eventos")]
    public UnityEvent onDamageTaken;
    public UnityEvent onDeath;
    public UnityEvent onHealthChanged;
    
    // Variables internas
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Camera mainCamera;

    // 🩸 Sistema de daño visual en shader
    private Material[] runtimeMaterials; // Materiales instanciados en runtime
    private bool shaderDamageInitialized = false;
    
    void Start()
    {
        // 🌐 Obtener PhotonView
        photonView = GetComponent<PhotonView>();

        currentHealth = maxHealth;

		// Obtener referencia al controlador del dinosaurio si no está asignada
		if (dinosaurController == null)
			dinosaurController = GetComponent<SimpleDinosaurController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Obtener renderers para efectos de daño
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }

            // 🩸 Inicializar sistema de daño visual en shader
            // SIEMPRE inicializar para crear instancias únicas de materiales
            // Esto evita que múltiples jugadores compartan el mismo material
            if (enableShaderDamage)
            {
                InitializeShaderDamageSystem();
            }
        }

        mainCamera = Camera.main;

        // 🌐 Solo mostrar UI de vida para el jugador local
        if (photonView != null && !photonView.IsMine)
        {
            // Ocultar barra de vida para jugadores remotos (la vida es local)
            if (healthBarCanvas != null)
                healthBarCanvas.gameObject.SetActive(false);

            // Ocultar imagen de daño para jugadores remotos
            if (damageOverlayImage != null)
                damageOverlayImage.gameObject.SetActive(false);

            // Ocultar panel de muerte para jugadores remotos
            if (deathPanel != null)
                deathPanel.SetActive(false);
        }
        else
        {
            // Solo para el jugador local
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

            UpdateHealthBar();
        }
    }
    
    void Update()
    {
        // 🌐 Solo el jugador local ejecuta la lógica de UI y sistema de hambre/sed
        if (photonView != null && !photonView.IsMine)
            return;

        // Billboard de la barra de vida
        if (billboardHealthBar && healthBarCanvas != null && mainCamera != null)
        {
            healthBarCanvas.transform.LookAt(healthBarCanvas.transform.position + mainCamera.transform.forward);
        }

		// 🍖 Sistema de pérdida de vida por hambre/sed (SOLO LOCAL)
		UpdateHungerThirstDamage();

		// 🩸 Actualizar UI de daño visual (SOLO LOCAL)
		UpdateDamageOverlay();
    }
    
    /// <summary>
    /// Recibir daño (SOLO LOCAL - cada jugador gestiona su propia vida)
    /// </summary>
    public void TakeDamage(float damage, Vector3 damageSource = default)
    {
        if (isDead || isInvulnerable || damage <= 0) return;

        // 🌐 Solo el jugador local procesa el daño real en su vida
        if (photonView != null && !photonView.IsMine)
            return;

        // Aplicar daño (SOLO LOCAL)
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{gameObject.name} recibió {damage} de daño. Vida restante: {currentHealth}/{maxHealth}");

        // Actualizar UI (SOLO LOCAL)
        UpdateHealthBar();

        // Eventos (SOLO LOCAL)
        onDamageTaken?.Invoke();
        onHealthChanged?.Invoke();

        // 🌐 Sincronizar efectos visuales en todos los clientes
        if (photonView != null)
        {
            photonView.RPC("RPC_ShowDamageEffects", RpcTarget.All);
        }
        else
        {
            // Sin Photon (modo single player o NPC)
            ShowDamageEffectsLocal();
        }

        // Verificar muerte (SOLO LOCAL)
        if (currentHealth <= 0 && canDie)
        {
            Die();
        }
    }

    /// <summary>
    /// Muestra efectos de daño localmente (sin red)
    /// </summary>
    void ShowDamageEffectsLocal()
    {
        // Efectos visuales
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        // Efecto de material
        if (damageMaterial != null && renderers.Length > 0)
        {
            StartCoroutine(DamageMaterialEffect());
        }

        // Sonido
        if (hurtSounds != null && hurtSounds.Length > 0 && audioSource != null)
        {
            AudioClip hurtClip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(hurtClip);
        }
    }

    /// <summary>
    /// 🌐 RPC para sincronizar efectos de daño en todos los clientes
    /// </summary>
    [PunRPC]
    void RPC_ShowDamageEffects()
    {
        ShowDamageEffectsLocal();
    }
    
    /// <summary>
    /// Curar vida
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateHealthBar();
        onHealthChanged?.Invoke();
        
        Debug.Log($"{gameObject.name} curado por {amount}. Vida: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Establecer vida
    /// </summary>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
        onHealthChanged?.Invoke();
    }
    
    /// <summary>
    /// Muerte
    /// </summary>
    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0;

        Debug.Log($"{gameObject.name} ha muerto!");

        // Evento de muerte (SOLO LOCAL)
        onDeath?.Invoke();

		// 💀 Llamar al método Die() del SimpleDinosaurController (sincroniza animación de muerte)
		if (dinosaurController != null)
		{
			dinosaurController.Die(); // Esto llama a RPC_Die internamente
		}

		// 💀 Activar panel de muerte (SOLO JUGADOR LOCAL)
		if (photonView != null && photonView.IsMine && deathPanel != null)
		{
			deathPanel.SetActive(true);
		}

        // 🌐 Sincronizar efectos visuales de muerte en todos los clientes
        if (photonView != null)
        {
            photonView.RPC("RPC_ShowDeathEffects", RpcTarget.All);
        }
        else
        {
            // Sin Photon (modo single player o NPC)
            ShowDeathEffectsLocal();
        }

        // Ocultar barra de vida (SOLO LOCAL)
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }

        // Destruir objeto (solo si está activado) - NO EN MULTIJUGADOR
        if (destroyOnDeath && photonView == null)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    /// <summary>
    /// Muestra efectos de muerte localmente
    /// </summary>
    void ShowDeathEffectsLocal()
    {
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
    }

    /// <summary>
    /// 🌐 RPC para sincronizar efectos de muerte en todos los clientes
    /// </summary>
    [PunRPC]
    void RPC_ShowDeathEffects()
    {
        ShowDeathEffectsLocal();
    }
    
    /// <summary>
    /// Actualizar barra de vida
    /// </summary>
    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }

        // 🩸 Actualizar daño visual en shader
        // SOLO el jugador local calcula y sincroniza el daño visual
        if (enableShaderDamage && shaderDamageInitialized)
        {
            if (photonView != null && photonView.IsMine)
            {
                // Calcular el valor de daño visual localmente
                float damageValue = CalculateDamageValue();

                // Sincronizar a todos los clientes (incluyendo el local)
                photonView.RPC("RPC_UpdateShaderDamage", RpcTarget.All, damageValue);
            }
            else if (photonView == null)
            {
                // Sin Photon (modo single player)
                UpdateShaderDamageLocal(CalculateDamageValue());
            }
        }
    }
    
    /// <summary>
    /// Efecto visual de daño
    /// </summary>
    System.Collections.IEnumerator DamageMaterialEffect()
    {
        // Cambiar a material de daño
        foreach (Renderer rend in renderers)
        {
            rend.material = damageMaterial;
        }
        
        yield return new WaitForSeconds(damageMaterialDuration);
        
        // Restaurar materiales originales
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
    }
    
    /// <summary>
    /// Hacer invulnerable temporalmente
    /// </summary>
    public void MakeInvulnerable(float duration)
    {
        StartCoroutine(InvulnerabilityCoroutine(duration));
    }
    
    System.Collections.IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        Debug.Log($"{gameObject.name} es invulnerable por {duration} segundos");
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
        Debug.Log($"{gameObject.name} ya no es invulnerable");
    }
    
    /// <summary>
    /// Obtener porcentaje de vida
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    /// <summary>
    /// Está vivo
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }
    
    /// <summary>
    /// Vida completa
    /// </summary>
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

	// ═══════════════════════════════════════════════════════════
	// 🍖 SISTEMA DE HAMBRE/SED
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Actualiza daño por hambre y sed
	/// </summary>
	void UpdateHungerThirstDamage()
	{
		if (isDead || dinosaurController == null) return;

		float hunger = dinosaurController.GetCurrentHunger();
		float thirst = dinosaurController.GetCurrentThirst();

		bool isHungry = hunger <= 0f;
		bool isThirsty = thirst <= 0f;

		// Si ambos están en 0, aplicar daño acelerado
		if (isHungry && isThirsty)
		{
			float combinedDamage = (hungerDamageRate + thirstDamageRate) * combinedDamageMultiplier * Time.deltaTime;
			currentHealth -= combinedDamage;
			currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

			// Debug.Log($"⚠️ Hambre y sed en 0! Daño acelerado: {combinedDamage:F2}");
		}
		// Si solo hambre está en 0
		else if (isHungry)
		{
			float damage = hungerDamageRate * Time.deltaTime;
			currentHealth -= damage;
			currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

			// Debug.Log($"⚠️ Hambre en 0! Daño: {damage:F2}");
		}
		// Si solo sed está en 0
		else if (isThirsty)
		{
			float damage = thirstDamageRate * Time.deltaTime;
			currentHealth -= damage;
			currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

			// Debug.Log($"⚠️ Sed en 0! Daño: {damage:F2}");
		}

		// Actualizar UI
		UpdateHealthBar();

		// Verificar muerte
		if (currentHealth <= 0 && canDie)
		{
			Die();
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 🩸 SISTEMA UI DE DAÑO VISUAL
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Actualiza la imagen de daño según el nivel de vida
	/// </summary>
	void UpdateDamageOverlay()
	{
		if (damageOverlayImage == null || damageSprites == null || damageSprites.Length != 9)
			return;

		// Calcular porcentaje de vida
		float healthPercentage = (currentHealth / maxHealth) * 100f;

		int newDamageLevel = -1;

		// Determinar el nivel de daño (0 = 90%, 1 = 80%, ..., 8 = 10%)
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
				// Vida > 90%, ocultar imagen
				damageOverlayImage.gameObject.SetActive(false);
			}
			else
			{
				// Mostrar imagen con el sprite correspondiente
				damageOverlayImage.gameObject.SetActive(true);

				if (damageSprites[newDamageLevel] != null)
				{
					damageOverlayImage.sprite = damageSprites[newDamageLevel];
				}
			}
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 🩸 SISTEMA DE DAÑO VISUAL EN SHADER
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Inicializa el sistema de daño visual en los shaders
	/// Crea materiales instanciados en runtime para CADA jugador independientemente
	/// </summary>
	void InitializeShaderDamageSystem()
	{
		if (renderers == null || renderers.Length == 0)
		{
			Debug.LogWarning("⚠️ No hay renderers para aplicar daño visual en shader");
			return;
		}

		runtimeMaterials = new Material[renderers.Length];

		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] != null)
			{
				// 🔥 CRÍTICO: Crear una instancia ÚNICA del material para este GameObject
				// Esto evita que múltiples jugadores compartan el mismo material
				Material instanceMaterial = new Material(renderers[i].sharedMaterial);
				renderers[i].material = instanceMaterial;
				runtimeMaterials[i] = instanceMaterial;

				// Verificar si el material tiene el parámetro de daño
				if (instanceMaterial.HasProperty(shaderDamageParameter))
				{
					// Inicializar en 0 (sin daño visible)
					instanceMaterial.SetFloat(shaderDamageParameter, 0f);

					string playerType = (photonView != null && photonView.IsMine) ? "LOCAL" : "REMOTO";
					Debug.Log($"✅ Shader damage inicializado en {renderers[i].name} ({playerType})");
				}
				else
				{
					Debug.LogWarning($"⚠️ El material '{instanceMaterial.name}' no tiene el parámetro '{shaderDamageParameter}'");
				}
			}
		}

		shaderDamageInitialized = true;
		string ownerInfo = photonView != null ? (photonView.IsMine ? "JUGADOR LOCAL" : $"JUGADOR REMOTO (ViewID: {photonView.ViewID})") : "SINGLE PLAYER";
		Debug.Log($"🩸 Sistema de daño visual en shader inicializado para {renderers.Length} renderers - {ownerInfo}");
	}

	/// <summary>
	/// Calcula el valor de daño visual según el porcentaje de vida
	/// SOLO lo ejecuta el jugador local (photonView.IsMine)
	/// </summary>
	float CalculateDamageValue()
	{
		// Calcular porcentaje de vida (0-100)
		float healthPercentage = (currentHealth / maxHealth) * 100f;

		// Calcular el valor de daño visual
		float damageValue = 0f;

		if (healthPercentage < damageVisualThreshold)
		{
			// Calcular cuánto ha bajado la vida desde el threshold
			// Si threshold = 90% y vida = 50%, entonces progress = (90-50) / 90 = 0.44
			float progress = (damageVisualThreshold - healthPercentage) / damageVisualThreshold;

			// Aplicar curva para hacer el daño más pronunciado
			progress = Mathf.Pow(progress, damageCurve);

			// Escalar por la intensidad máxima
			damageValue = progress * maxDamageIntensity;

			// Limitar entre 0 y maxDamageIntensity
			damageValue = Mathf.Clamp01(damageValue);
		}

		return damageValue;
	}

	/// <summary>
	/// 🌐 RPC: Actualiza el shader de daño en TODOS los clientes (sincronizado)
	/// </summary>
	[PunRPC]
	void RPC_UpdateShaderDamage(float damageValue)
	{
		UpdateShaderDamageLocal(damageValue);
	}

	/// <summary>
	/// Actualiza el valor de daño visual en los shaders localmente
	/// Ejecutado por todos los clientes para ver el mismo efecto
	/// </summary>
	void UpdateShaderDamageLocal(float damageValue)
	{
		if (runtimeMaterials == null || runtimeMaterials.Length == 0)
			return;

		// Aplicar el valor a todos los materiales de ESTE jugador específico
		foreach (Material mat in runtimeMaterials)
		{
			if (mat != null && mat.HasProperty(shaderDamageParameter))
			{
				mat.SetFloat(shaderDamageParameter, damageValue);
			}
		}

		// Log ocasional para debug (solo cada 5% de cambio aproximadamente)
		if (Mathf.Abs(damageValue * 100f - Mathf.Round(damageValue * 100f / 5f) * 5f) < 1f)
		{
			string playerType = (photonView != null && photonView.IsMine) ? "LOCAL" : "REMOTO";
			float healthPercentage = (currentHealth / maxHealth) * 100f;
			//Debug.Log($"🩸 Daño visual actualizado ({playerType}): {damageValue:F2} (Vida: {healthPercentage:F1}%)");
		}
	}

    void OnGUI()
    {
        if (Application.isEditor && !isDead)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
               // GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 20),
                 //        $"HP: {currentHealth:F0}/{maxHealth:F0}");
            }
        }
    }
}