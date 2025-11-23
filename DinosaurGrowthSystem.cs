using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Sistema de crecimiento para dinosaurios con 3 etapas: Juvenil, Sub-adulto, Adulto
/// Sincronizado con Photon de forma optimizada (cada 1 minuto)
/// </summary>
public class DinosaurGrowthSystem : MonoBehaviourPunCallbacks, IPunObservable
{
	// ═══════════════════════════════════════════════════════════
	// 🌱 ETAPAS DE CRECIMIENTO
	// ═══════════════════════════════════════════════════════════

	public enum GrowthStage
	{
		Juvenile = 0,    // Juvenil (0% - 33%)
		SubAdult = 1,    // Sub-adulto (34% - 66%)
		Adult = 2        // Adulto (67% - 100%)
	}

	[Header("📊 Estado de Crecimiento")]
	[Tooltip("Etapa actual de crecimiento")]
	public GrowthStage currentStage = GrowthStage.Juvenile;

	[Tooltip("Progreso de crecimiento (0 = Juvenil, 1 = Adulto completo)")]
	[Range(0f, 1f)]
	public float growthProgress = 0f;

	[Tooltip("Porcentaje de vida actual (0-1, donde 1 = 100%)")]
	[Range(0f, 1f)]
	public float healthPercentage = 1f;

	// ═══════════════════════════════════════════════════════════
	// ⏱️ CONFIGURACIÓN DE TIEMPO
	// ═══════════════════════════════════════════════════════════

	[Header("⏱️ Tiempo de Crecimiento")]
	[Tooltip("Minutos totales para crecer de Juvenil a Adulto")]
	[Range(5f, 60f)]
	public float growthTimeMinutes = 15f;

	[Tooltip("¿Iniciar crecimiento automáticamente?")]
	public bool autoStartGrowth = true;

	[Tooltip("¿Ya completó el crecimiento?")]
	public bool isFullyGrown = false;

	private float growthStartTime;
	private float lastSyncTime = 0f;
	private const float SYNC_INTERVAL = 60f; // Sincronizar cada 60 segundos

	// ═══════════════════════════════════════════════════════════
	// 📈 STATS - VALORES INICIALES Y FINALES
	// ═══════════════════════════════════════════════════════════

	[Header("💪 Stats - Daño")]
	[Tooltip("Daño en etapa Juvenil")]
	public float damageJuvenile = 100f;
	[Tooltip("Daño en etapa Adulta")]
	public float damageAdult = 500f;

	[Header("❤️ Stats - Vida")]
	[Tooltip("Vida máxima en etapa Juvenil")]
	public float healthJuvenile = 50f;
	[Tooltip("Vida máxima en etapa Adulta")]
	public float healthAdult = 1000f;

	[Header("🚶 Stats - Velocidad de Caminar")]
	[Tooltip("Velocidad de caminar en etapa Juvenil")]
	public float walkSpeedJuvenile = 2f;
	[Tooltip("Velocidad de caminar en etapa Adulta")]
	public float walkSpeedAdult = 4f;

	[Header("🏃 Stats - Velocidad de Correr")]
	[Tooltip("Velocidad de correr en etapa Juvenil")]
	public float runSpeedJuvenile = 4f;
	[Tooltip("Velocidad de correr en etapa Adulta")]
	public float runSpeedAdult = 8f;

	[Header("🏊 Stats - Velocidad de Nadar")]
	[Tooltip("Velocidad de nadar en etapa Juvenil")]
	public float swimSpeedJuvenile = 2f;
	[Tooltip("Velocidad de nadar en etapa Adulta")]
	public float swimSpeedAdult = 5f;

	// ═══════════════════════════════════════════════════════════
	// 📏 ESCALA VISUAL
	// ═══════════════════════════════════════════════════════════

	[Header("📏 Escala Visual")]
	[Tooltip("Escala del modelo en etapa Juvenil")]
	public float scaleJuvenile = 0.5f;
	[Tooltip("Escala del modelo en etapa Sub-adulta")]
	public float scaleSubAdult = 0.75f;
	[Tooltip("Escala del modelo en etapa Adulta")]
	public float scaleAdult = 1f;

	// ═══════════════════════════════════════════════════════════
	// 🔊 SONIDOS POR ETAPA
	// ═══════════════════════════════════════════════════════════

	[Header("🔊 Sonidos de Pisadas por Etapa")]
	[Tooltip("Sonidos de caminar para etapa Juvenil")]
	public AudioClip[] walkSoundsJuvenile;
	[Tooltip("Sonidos de caminar para etapa Sub-adulta")]
	public AudioClip[] walkSoundsSubAdult;
	[Tooltip("Sonidos de caminar para etapa Adulta")]
	public AudioClip[] walkSoundsAdult;

	[Tooltip("Sonidos de correr para etapa Juvenil")]
	public AudioClip[] runSoundsJuvenile;
	[Tooltip("Sonidos de correr para etapa Sub-adulta")]
	public AudioClip[] runSoundsSubAdult;
	[Tooltip("Sonidos de correr para etapa Adulta")]
	public AudioClip[] runSoundsAdult;

	// ═══════════════════════════════════════════════════════════
	// 🔗 REFERENCIAS
	// ═══════════════════════════════════════════════════════════

	[Header("🔗 Referencias")]
	[Tooltip("Referencia al SimpleDinosaurController")]
	public SimpleDinosaurController dinosaurController;

	[Tooltip("Referencia al HealthSystem")]
	public HealthSystem healthSystem;

	private PhotonView photonView;

	// ═══════════════════════════════════════════════════════════
	// 🎬 INICIALIZACIÓN
	// ═══════════════════════════════════════════════════════════

	void Start()
	{
		// Obtener componentes
		photonView = GetComponent<PhotonView>();

		if (dinosaurController == null)
			dinosaurController = GetComponent<SimpleDinosaurController>();

		if (healthSystem == null)
			healthSystem = GetComponent<HealthSystem>();

		// Solo el dueño inicia el crecimiento
		if (photonView.IsMine)
		{
			if (autoStartGrowth)
			{
				StartGrowth();
			}
		}

		// Aplicar stats iniciales
		ApplyCurrentStats();
	}

	void Update()
	{
		// Solo el dueño actualiza el crecimiento
		if (!photonView.IsMine || isFullyGrown) return;

		// Actualizar progreso de crecimiento
		UpdateGrowth();

		// Sincronizar cada 60 segundos
		if (Time.time - lastSyncTime >= SYNC_INTERVAL)
		{
			SyncGrowthToNetwork();
			lastSyncTime = Time.time;
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 🌱 SISTEMA DE CRECIMIENTO
	// ═══════════════════════════════════════════════════════════

	public void StartGrowth()
	{
		growthStartTime = Time.time;
		growthProgress = 0f;
		isFullyGrown = false;
		Debug.Log("🌱 Crecimiento iniciado! Tiempo total: " + growthTimeMinutes + " minutos");
	}

	void UpdateGrowth()
	{
		// Calcular tiempo transcurrido en segundos
		float elapsedTime = Time.time - growthStartTime;
		float totalGrowthTime = growthTimeMinutes * 60f; // Convertir a segundos

		// Calcular progreso (0 a 1)
		growthProgress = Mathf.Clamp01(elapsedTime / totalGrowthTime);

		// Actualizar etapa según progreso
		GrowthStage previousStage = currentStage;

		if (growthProgress < 0.33f)
			currentStage = GrowthStage.Juvenile;
		else if (growthProgress < 0.67f)
			currentStage = GrowthStage.SubAdult;
		else
			currentStage = GrowthStage.Adult;

		// Detectar cambio de etapa
		if (previousStage != currentStage)
		{
			OnStageChanged(previousStage, currentStage);
		}

		// Marcar como completamente crecido
		if (growthProgress >= 1f && !isFullyGrown)
		{
			isFullyGrown = true;
			Debug.Log("🦖 ¡Crecimiento completado! Ahora es un adulto.");
		}

		// Aplicar stats actuales
		ApplyCurrentStats();
	}

	void OnStageChanged(GrowthStage oldStage, GrowthStage newStage)
	{
		Debug.Log($"📊 Cambio de etapa: {oldStage} → {newStage}");

		// Actualizar escala visual
		UpdateVisualScale();

		// Actualizar sonidos de pisadas
		UpdateFootstepSounds();

		// Sincronizar inmediatamente al cambiar de etapa
		SyncGrowthToNetwork();
	}

	// ═══════════════════════════════════════════════════════════
	// 📊 APLICAR STATS
	// ═══════════════════════════════════════════════════════════

	void ApplyCurrentStats()
	{
		if (dinosaurController == null) return;

		// Guardar porcentaje de vida actual antes de cambiar stats
		if (healthSystem != null && healthSystem.maxHealth > 0)
		{
			healthPercentage = healthSystem.currentHealth / healthSystem.maxHealth;
		}

		// Interpolar stats basado en growthProgress
		float currentDamage = Mathf.Lerp(damageJuvenile, damageAdult, growthProgress);
		float currentMaxHealth = Mathf.Lerp(healthJuvenile, healthAdult, growthProgress);
		float currentWalkSpeed = Mathf.Lerp(walkSpeedJuvenile, walkSpeedAdult, growthProgress);
		float currentRunSpeed = Mathf.Lerp(runSpeedJuvenile, runSpeedAdult, growthProgress);
		float currentSwimSpeed = Mathf.Lerp(swimSpeedJuvenile, swimSpeedAdult, growthProgress);

		// Aplicar al SimpleDinosaurController
		dinosaurController.attackDamage = currentDamage;
		dinosaurController.walkSpeed = currentWalkSpeed;
		dinosaurController.runSpeed = currentRunSpeed;
		dinosaurController.swimSpeed = currentSwimSpeed;

		// Aplicar al HealthSystem manteniendo el porcentaje de vida
		if (healthSystem != null)
		{
			healthSystem.maxHealth = currentMaxHealth;
			healthSystem.currentHealth = currentMaxHealth * healthPercentage; // Mantener porcentaje
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 📏 ESCALA VISUAL
	// ═══════════════════════════════════════════════════════════

	void UpdateVisualScale()
	{
		float targetScale = scaleJuvenile;

		switch (currentStage)
		{
			case GrowthStage.Juvenile:
				targetScale = scaleJuvenile;
				break;
			case GrowthStage.SubAdult:
				targetScale = scaleSubAdult;
				break;
			case GrowthStage.Adult:
				targetScale = scaleAdult;
				break;
		}

		// Aplicar escala al transform
		transform.localScale = Vector3.one * targetScale;

		Debug.Log($"📏 Escala actualizada a: {targetScale}");
	}

	// ═══════════════════════════════════════════════════════════
	// 🔊 SONIDOS DE PISADAS
	// ═══════════════════════════════════════════════════════════

	void UpdateFootstepSounds()
	{
		if (dinosaurController == null) return;

		AudioClip[] newWalkSounds = null;
		AudioClip[] newRunSounds = null;

		switch (currentStage)
		{
			case GrowthStage.Juvenile:
				newWalkSounds = walkSoundsJuvenile;
				newRunSounds = runSoundsJuvenile;
				break;
			case GrowthStage.SubAdult:
				newWalkSounds = walkSoundsSubAdult;
				newRunSounds = runSoundsSubAdult;
				break;
			case GrowthStage.Adult:
				newWalkSounds = walkSoundsAdult;
				newRunSounds = runSoundsAdult;
				break;
		}

		// Actualizar sonidos en el SimpleDinosaurController
		if (newWalkSounds != null && newWalkSounds.Length > 0)
		{
			dinosaurController.walkSounds = newWalkSounds;
			Debug.Log($"🔊 Sonidos de caminar actualizados para etapa: {currentStage}");
		}

		if (newRunSounds != null && newRunSounds.Length > 0)
		{
			dinosaurController.runSounds = newRunSounds;
			Debug.Log($"🔊 Sonidos de correr actualizados para etapa: {currentStage}");
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 🌐 SINCRONIZACIÓN PHOTON
	// ═══════════════════════════════════════════════════════════

	void SyncGrowthToNetwork()
	{
		if (photonView != null && photonView.IsMine)
		{
			// Los datos se sincronizan automáticamente a través de OnPhotonSerializeView
			Debug.Log($"📡 Sincronizando crecimiento: {growthProgress * 100f:F1}% - Etapa: {currentStage}");
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			// Enviar datos (solo el dueño)
			stream.SendNext(growthProgress);
			stream.SendNext((int)currentStage);
			stream.SendNext(healthPercentage);
			stream.SendNext(isFullyGrown);
		}
		else
		{
			// Recibir datos (clientes remotos)
			float receivedProgress = (float)stream.ReceiveNext();
			GrowthStage receivedStage = (GrowthStage)stream.ReceiveNext();
			healthPercentage = (float)stream.ReceiveNext();
			isFullyGrown = (bool)stream.ReceiveNext();

			// Detectar cambio de etapa
			if (receivedStage != currentStage)
			{
				currentStage = receivedStage;
				UpdateVisualScale();
				UpdateFootstepSounds();
			}

			// Actualizar progreso
			growthProgress = receivedProgress;

			// Aplicar stats
			ApplyCurrentStats();
		}
	}

	// ═══════════════════════════════════════════════════════════
	// 🛠️ UTILIDADES PÚBLICAS
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Forzar crecimiento instantáneo a una etapa específica (para debugging)
	/// </summary>
	public void SetGrowthStage(GrowthStage stage)
	{
		if (!photonView.IsMine) return;

		switch (stage)
		{
			case GrowthStage.Juvenile:
				growthProgress = 0.16f;
				break;
			case GrowthStage.SubAdult:
				growthProgress = 0.5f;
				break;
			case GrowthStage.Adult:
				growthProgress = 1f;
				isFullyGrown = true;
				break;
		}

		UpdateGrowth();
		SyncGrowthToNetwork();
	}

	/// <summary>
	/// Obtener el nombre de la etapa actual
	/// </summary>
	public string GetStageName()
	{
		switch (currentStage)
		{
			case GrowthStage.Juvenile:
				return "Juvenil";
			case GrowthStage.SubAdult:
				return "Sub-adulto";
			case GrowthStage.Adult:
				return "Adulto";
			default:
				return "Desconocido";
		}
	}

	/// <summary>
	/// Obtener tiempo restante de crecimiento en segundos
	/// </summary>
	public float GetRemainingGrowthTime()
	{
		if (isFullyGrown) return 0f;

		float totalGrowthTime = growthTimeMinutes * 60f;
		float elapsedTime = Time.time - growthStartTime;
		return Mathf.Max(0f, totalGrowthTime - elapsedTime);
	}
}
