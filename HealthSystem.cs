using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Fusion;

// SISTEMA DE SALUD - ADAPTADO PARA PHOTON FUSION
// ✅ La vida es local (no sincronizada) pero se puede recibir daño por RPC
// ✅ Cada jugador ve su propia vida
// ✅ El daño se envía desde SimpleDinosaurController via RPC

public class HealthSystem : NetworkBehaviour
{
    [Header("Configuración de Salud")]
    [Tooltip("Vida máxima (100% = este valor)")]
    public float maxHealth = 200f;
    [Tooltip("Vida actual")]
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
    
    void Start()
    {
        currentHealth = maxHealth;

		// Obtener referencia al controlador del dinosaurio si no está asignada
		if (dinosaurController == null)
			dinosaurController = GetComponent<SimpleDinosaurController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Obtener renderers para efectos de daño
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0 && damageMaterial != null)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }

        mainCamera = Camera.main;

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
    
    void Update()
    {
        // Billboard de la barra de vida
        if (billboardHealthBar && healthBarCanvas != null && mainCamera != null)
        {
            healthBarCanvas.transform.LookAt(healthBarCanvas.transform.position + mainCamera.transform.forward);
        }

		// 🍖 Sistema de pérdida de vida por hambre/sed
		UpdateHungerThirstDamage();

		// 🩸 Actualizar UI de daño visual
		UpdateDamageOverlay();
    }
    
    /// <summary>
    /// Recibir daño
    /// </summary>
    public void TakeDamage(float damage, Vector3 damageSource = default)
    {
        if (isDead || isInvulnerable || damage <= 0) return;
        
        // Aplicar daño
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        Debug.Log($"{gameObject.name} recibió {damage} de daño. Vida restante: {currentHealth}/{maxHealth}");
        
        // Actualizar UI
        UpdateHealthBar();
        
        // Eventos
        onDamageTaken?.Invoke();
        onHealthChanged?.Invoke();
        
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
        
        // Verificar muerte
        if (currentHealth <= 0 && canDie)
        {
            Die();
        }
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

        // Evento de muerte
        onDeath?.Invoke();

		// 💀 Llamar al método Die() del SimpleDinosaurController
		if (dinosaurController != null)
		{
			dinosaurController.Die();
		}

		// 💀 Activar panel de muerte
		if (deathPanel != null)
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

        // Destruir objeto (solo si está activado)
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
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

    void OnGUI()
    {
        if (Application.isEditor && !isDead)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 20), 
                         $"HP: {currentHealth:F0}/{maxHealth:F0}");
            }
        }
    }
}
