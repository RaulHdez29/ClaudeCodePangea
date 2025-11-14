using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using System.IO;
using System.Linq;

public class Creature : MonoBehaviourPun, IPunObservable, Photon.Pun.IOnPhotonViewOwnerChange
{
	#region VARIABLES
	
	[Space(10)]
	[Header("LOCAL PLAYER SETTINGS")]
	[Tooltip("Marcar para controlar esta criatura como jugador local, independiente del Manager")]
	public bool isLocalPlayer = false;
	
	[Header("JOYSTICK CONTROL")]
	public Joystick movementJoystick;
	
	[Space(10)]
	[Header("UI BUTTONS")]
	public Button attackButton;
	public Button jumpButton;
	public Button roarButton;
	public Button sleepButton;
	public Button runButton;
	public Button eatButton;
	public Button drinkButton;
	public Button crouchButton;
	public Button preciseMovementButton;
	
	[Space(10)]
	[Header("UI STATUS BARS")]
	[Tooltip("Imagen filled para mostrar la vida")]
	public Image healthBar;
	[Tooltip("Imagen filled para mostrar el hambre")]
	public Image foodBar;
	[Tooltip("Imagen filled para mostrar la sed")]
	public Image waterBar;
	[Tooltip("Imagen filled para mostrar la estamina")]
	public Image staminaBar;

	[HideInInspector] public bool isRunToggled = false;
	[HideInInspector] public bool isPreciseMovementToggled = false;
	[HideInInspector] public bool isAttackPressed = false;
	[HideInInspector] public bool isJumpPressed = false;
	[HideInInspector] public bool isRoarPressed = false;
	[HideInInspector] public bool isSleepPressed = false;
	[HideInInspector] public bool isEatPressed = false;
	[HideInInspector] public bool isDrinkPressed = false;
	[HideInInspector] public bool isCrouchToggled = false;
	[HideInInspector] public float dinosaurForwardDirection = 0f;
	[HideInInspector] public bool isCrouchPressed = false;
	

	[Space(10)]
	[Header("STANDALONE SETTINGS")]
	[Tooltip("Use Inverse Kinematics - Accurate feet placement on ground")]
	public bool useIK = true;
	[Tooltip("Creatures will be active even if they are no longer visible")]
	public bool realtimeGame = false;
	[Tooltip("Countdown to destroy the creature after death. Put 0 to cancel countdown")]
	public int timeAfterDead = 10000;
	[Tooltip("Allow creatures to walk on all kind of collider (more expensive) vs Terrain only (faster)")]
	public bool useRaycast = true;
	[Tooltip("Layer used for water")]
	public int waterLayer = 4;
	[Tooltip("Unity terrain tree layer")]
	public int treeLayer = 0;
	[Tooltip("Maximum walkable slope before creature starts slipping")]
	[Range(0.1f, 1.0f)] public float MaxSlope = 0.75f;
	[Tooltip("Water plane altitude")]
	public float waterAlt = 55;
	[Tooltip("Blood particle for creatures")]
	public ParticleSystem bloodParticle;

	[Space(10)]
	[Header("ARTIFICIAL INTELLIGENCE")]
	public bool useAI=false;
	const string TIP1 =
	"Use gameobjects as waypoints to define a path for this creature by \n"+
	"taking into account the priority between autonomous AI and its path.";
	const string TIP2 =
	"Place your waypoint gameobject in a reacheable position.\n"+
	"Don't put a waypoint in air if the creature are not able to fly";
	const string TIP3 =
	"Using a priority of 100% will disable all autonomous AI for this waypoint\n"+
	"Obstacle avoid AI and custom targets search still enabled";
	const string TIP4 =
	"Use gameobjects to assign a custom enemy/friend for this creature\n"+
	"Can be any kind of gameobject e.g : player, other creature.\n"+
	"The creature will include friend/enemy goals in its search. \n"+
	"Enemy: triggered if the target is in range. \n"+
	"Friend: triggered when the target moves away.";
	const string TIP5 =
	"If MaxRange is zero, range is infinite. \n"+
	"Creature will start his attack/tracking once in range.";
	//Path editor
	[Space(10)]
	[Tooltip(TIP1)] public List<PathEditor> pathEditor;
	[HideInInspector] public int nextPath=0;
	[HideInInspector] public enum PathType { Walk, Run };
	[HideInInspector] public enum TargetAction { None, Sleep, Eat, Drink };
	[System.Serializable]
	public struct PathEditor
	{
		[Tooltip(TIP2)] public GameObject waypoint;
		public PathType pathType;
		public TargetAction targetAction;
		[Tooltip(TIP3)] [Range(1,100)] public int priority;

		public PathEditor(GameObject Waypoint,PathType PathType,TargetAction TargetAction,int Priority)
		{ waypoint=Waypoint; pathType=PathType; targetAction=TargetAction; priority=Priority; }
	}

	//Target editor
	[Space(10)]
	[Tooltip(TIP4)] public List<TargetEditor> targetEditor;
	[HideInInspector] public enum TargetType { Enemy, Friend };
	[System.Serializable]
	public struct TargetEditor
	{
		public GameObject _GameObject;
		public TargetType _TargetType;
		[Tooltip(TIP5)]
		public int MaxRange;
	}

	[Space(10)]
	[Header("CREATURE SETTINGS")]
	public Skin bodyTexture;
	public Eyes eyesTexture;
	[Space(5)]
	[Range(0.0f,1000.0f)] public float health=100f;
	[Range(0.0f,100.0f)] public float water=100f;
	[Range(0.0f,100.0f)] public float food=100f;
	[Range(0.0f,100.0f)] public float stamina=100f;
	[Space(5)]
	[Range(1.0f,10.0f)] public float damageMultiplier=1.0f;
	[Range(1.0f,10.0f)] public float armorMultiplier=1.0f;
	[Range(0.0f,2.0f)] public float animSpeed=1.0f;
	public bool herbivorous, canAttack, canHeadAttack, canTailAttack, canWalk, canJump, canFly, canSwim, lowAltitude, canInvertBody;
	public float baseMass=1, ang_T=0.025f, crouch_Max=0, yaw_Max=0, pitch_Max=0;
	
	[Space(10)]
	[Header("STAMINA SETTINGS")]
	[Tooltip("Stamina consumption rate when running")]
	[Range(0.1f, 5.0f)] public float runStaminaDrain = 1.0f;
	[Tooltip("Stamina consumption rate when flying")]
	[Range(0.1f, 5.0f)] public float flyStaminaDrain = 1.5f;
	[Tooltip("Minimum stamina required to run")]
	[Range(0.0f, 50.0f)] public float minStaminaToRun = 10.0f;
	[Tooltip("Stamina regeneration rate when resting")]
	[Range(0.1f, 3.0f)] public float staminaRegenRate = 0.5f;
	
	[Header("AQUATIC DASH SYSTEM")]
	[Tooltip("Button for aquatic dash/burst movement")]
	public Button dashButton;
	[Tooltip("Stamina cost for dash burst")]
	[Range(5.0f, 30.0f)] public float dashStaminaCost = 15.0f;
	[Tooltip("Force multiplier for dash burst")]
	[Range(2.0f, 10.0f)] public float dashForceMultiplier = 5.0f;
	[Tooltip("Cooldown time between dashes (seconds)")]
	[Range(0.5f, 5.0f)] public float dashCooldown = 2.0f;

	[HideInInspector] public bool isDashPressed = false;
	[HideInInspector] public bool isDashing = false;
	[HideInInspector] public float dashTimer = 0f;
	[HideInInspector] public float dashCooldownTimer = 0f;

	[Space(20)]
	[Header("COMPONENTS AND TEXTURES")]
	public Rigidbody body;
	public LODGroup lod;
	public Animator anm;
	public AudioSource[] source;
	public SkinnedMeshRenderer[] rend;
	public Texture[] skin, eyes;
	public enum Skin { SkinA, SkinB, SkinC };
	public enum Eyes { Type0, Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10, Type11, Type12, Type13, Type14, Type15 };
	[Space(20)]
	[Header("TRANSFORMS AND SOUNDS")]
	public Transform Head;

	// STANDALONE VARIABLES (previously from Manager)
	[HideInInspector] public static List<Creature> allCreatures = new List<Creature>();
	[HideInInspector] public Camera mainCamera;
	[HideInInspector] public AnimatorStateInfo OnAnm;
	[HideInInspector] public bool isActive, isVisible, isDead, isOnGround, isOnWater, isInWater, isConstrained, isOnLevitation;
	[HideInInspector] public bool onAttack, onJump, onCrouch, onReset, onInvert, onHeadMove, onAutoLook, onTailAttack;
	[HideInInspector] public int rndX, rndY, rndMove, rndIdle, loop;
	[HideInInspector] public string behavior, specie;
	[HideInInspector] public GameObject objTGT=null, objCOL=null;
	[HideInInspector] public Vector3 headPos, posCOL=Vector3.zero, posTGT=Vector3.zero, lookTGT=Vector3.zero, boxscale=Vector3.zero, normal=Vector3.zero;
	[HideInInspector] public Quaternion angTGT=Quaternion.identity, normAng=Quaternion.identity;
	[HideInInspector] public float currframe, lastframe, lastHit;
	[HideInInspector] public float crouch, spineX, spineY, headX, headY, pitch, roll, reverse;
	[HideInInspector] public float posY, waterY, withersSize, size, speed;
	[HideInInspector] public float behaviorCount, distTGT, delta, actionDist, angleAdd, avoidDelta, avoidAdd;
	const int enemyMaxRange=50, waterMaxRange=200, foodMaxRange=200, friendMaxRange=200, preyMaxRange=200;

	// TERRAIN DATA
	[HideInInspector] public Terrain t = null;
	[HideInInspector] public TerrainData tdata = null;
	[HideInInspector] public Vector3 tpos = Vector3.zero;
	[HideInInspector] public float tres = 0;
	
	[Header("CAMERA FLIGHT CONTROL")]
	[Tooltip("Enable camera pitch control for flying creatures")]
	public bool useCameraPitchControl = false;
	[Tooltip("Sensitivity for camera pitch control")]
	[Range(0.5f, 3.0f)] public float cameraPitchSensitivity = 1.0f;
	[Tooltip("Dead zone for camera pitch (degrees from horizon)")]
	[Range(1.0f, 15.0f)] public float cameraPitchDeadZone = 5.0f;
	
	[Header("UNDERWATER VISION")]
	[Tooltip("Custom underwater fog density for this creature (-1 = use default)")]
	[Range(-1f, 0.1f)] public float customUnderwaterDensity = -1f;
	[Tooltip("Custom underwater visibility multiplier")]
	[Range(1f, 10f)] public float customVisibilityMultiplier = 1f;

	// NETWORKING VARIABLES
	private Vector3 networkPosition;
	private Quaternion networkRotation;
	private Vector3 networkVelocity;
	private float networkTurn;
	private int networkMove;
	private int networkIdle;
	private float networkPitch;
	private bool networkAttack;
	private bool networkOnGround;
	private float networkHealth;
	private bool networkIsDead;
	private string networkBehavior = "";
	private bool networkIsOnWater;
	private bool networkIsInWater;
	
	// NUEVAS VARIABLES PARA SINCRONIZAR ROTACIONES DE HUESOS
	private float networkSpineX;
	private float networkSpineY;
	private float networkHeadX;
	private float networkHeadY;
	private float networkPitchBone;
	private float networkRoll;
	private float networkCrouch;
	private float networkSpeed;
	private Vector3 networkLookTGT;
	private float networkFood;
	private float networkWater;
	private float networkStamina;
	
	// Network optimization
	//private float lastSendTime;
	private const float sendRate = 15f; // 15 times per second
	//private Vector3 lastSentPosition;
	//private Quaternion lastSentRotation;
	private float positionThreshold = 0.1f;
	private float rotationThreshold = 1f;
	
	// VARIABLES ADICIONALES NECESARIAS
private string lastSentBehavior = "";

// Variables adicionales necesarias para optimización
private Vector3 lastSentPosition = Vector3.zero;
private Quaternion lastSentRotation = Quaternion.identity;
private float lastSendTime = 0f;
private int lastSentIdle = 0;      // NUEVO: Para detectar cambios de animaciones idle
private int lastSentMove = 0;      // NUEVO: Para detectar cambios de movimiento/jump

	// INTEREST MANAGEMENT SYSTEM
	[Header("INTEREST MANAGEMENT")]
	[Tooltip("Tamaño de cada celda del grid en metros")]
	public float gridCellSize = 100f;
	[Tooltip("Grupos adyacentes a considerar (1 = solo adyacentes, 2 = más radio)")]
	public int adjacentRadius = 1;
	[Tooltip("Tiempo máximo sin actualización antes de ocultar (segundos)")]
	public float maxTimeWithoutUpdate = 3f;

	private byte currentInterestGroup = 0;
	private float lastInterestGroupUpdate = 0f;
	private const float INTEREST_UPDATE_INTERVAL = 2f;
	
	// NUEVO: Para detectar fantasmas
	private float lastNetworkUpdate = 0f;
	private Vector3 lastKnownPosition = Vector3.zero;

	//IK TYPES
	public enum IkType { None, Convex, Quad, Flying, SmBiped, LgBiped }
	// IK goal position
	Vector3 FR_HIT, FL_HIT, BR_HIT, BL_HIT;
	// Terrain normals
	Vector3 FR_Norm=Vector3.up, FL_Norm=Vector3.up, BR_Norm=Vector3.up, BL_Norm=Vector3.up;
	//Back Legs
	float BR1, BR2, BR3, BR_Add; //Right
	float BL1, BL2, BL3, BL_Add; //Left
	float alt1, alt2, a1, a2, b1, b2, c1, c2;
	//Front Legs
	float FR1, FR2, FR3, FR_Add; //Right
	float FL1, FL2, FL3, FL_Add; //Left
	float alt3, alt4, a3, a4, b3, b4, c3, c4;
	#endregion
	
	#region PHOTON NETWORKING
public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    if (stream.IsWriting)
    {
        // Solo sincronizamos si somos el owner
        if(photonView.IsMine && body != null)
        {
            // USAR TUS OPTIMIZACIONES EXISTENTES
            float currentTime = Time.time;
            
            // 1. Verificar si es momento de enviar (tu sendRate = 15fps)
            if (currentTime - lastSendTime < 1f / sendRate)
                return; // No enviar hasta que pase el tiempo
            
            // 2. Verificar cambios significativos (tus thresholds)
            bool hasPositionChange = Vector3.Distance(transform.position, lastSentPosition) > positionThreshold;
            bool hasRotationChange = Quaternion.Angle(transform.rotation, lastSentRotation) > rotationThreshold;
            
            // NUEVO: Detectar cambios de animación para sincronización inmediata
            int currentIdle = anm.GetInteger("Idle");
            int currentMove = anm.GetInteger("Move");
            bool hasAnimationChange = (currentIdle != lastSentIdle) || (currentMove != lastSentMove);
            
            bool hasCriticalState = onAttack || onJump || hasAnimationChange; // Incluir cambios de animación
            
            // 3. Solo enviar si hay cambios significativos o han pasado 2 segundos
            if (!hasPositionChange && !hasRotationChange && !hasCriticalState && currentTime - lastSendTime < 2f)
                return; // Saltar este frame
            
            // 4. ENVIAR DATOS (sin rotaciones de huesos)
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(body.velocity);
            
            // Datos del animador
            stream.SendNext(anm.GetFloat("Turn"));
            stream.SendNext(anm.GetInteger("Move"));
            stream.SendNext(anm.GetInteger("Idle"));
            stream.SendNext(anm.GetFloat("Pitch"));
            stream.SendNext(anm.GetBool("Attack"));
            stream.SendNext(anm.GetBool("OnGround"));
            
            // Estados críticos siempre
            stream.SendNext(onAttack);
            stream.SendNext(onJump);
            
            // Stats menos frecuentes (cada 3 envíos)
            bool sendStats = Time.frameCount % 3 == 0;
            stream.SendNext(sendStats);
            if (sendStats)
            {
                stream.SendNext(health);
                stream.SendNext(food);
                stream.SendNext(water);
                stream.SendNext(stamina);
                stream.SendNext(isDead);
                stream.SendNext(isOnGround);
                stream.SendNext(isInWater);
                stream.SendNext(isOnWater);
            }
            
            // Behavior solo si cambió
            bool behaviorChanged = behavior != lastSentBehavior;
            stream.SendNext(behaviorChanged);
            if (behaviorChanged)
            {
                stream.SendNext(behavior);
                lastSentBehavior = behavior;
            }
            
            // *** ELIMINADO: Rotaciones de huesos (44 bytes menos) ***
            // stream.SendNext(spineX);     // ELIMINADO
            // stream.SendNext(spineY);     // ELIMINADO  
            // stream.SendNext(headX);      // ELIMINADO
            // stream.SendNext(headY);      // ELIMINADO
            // stream.SendNext(pitch);      // ELIMINADO
            // stream.SendNext(roll);       // ELIMINADO
            // stream.SendNext(crouch);     // ELIMINADO
            // stream.SendNext(speed);      // ELIMINADO
            // stream.SendNext(lookTGT);    // ELIMINADO
            
            // 5. Actualizar valores de referencia
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            lastSendTime = currentTime;
            lastSentIdle = currentIdle; // NUEVO: Guardar última animación idle enviada  
            lastSentMove = currentMove; // NUEVO: Guardar último move enviado
        }
    }
    else
    {
		    // NUEVO: Marcar que recibimos actualización
		lastNetworkUpdate = Time.time;
		lastKnownPosition = transform.position;
        // RECEPCIÓN OPTIMIZADA
        networkPosition = (Vector3)stream.ReceiveNext();
        networkRotation = (Quaternion)stream.ReceiveNext();
        networkVelocity = (Vector3)stream.ReceiveNext();
        
        networkTurn = (float)stream.ReceiveNext();
        networkMove = (int)stream.ReceiveNext();
        networkIdle = (int)stream.ReceiveNext();
        networkPitch = (float)stream.ReceiveNext();
        networkAttack = (bool)stream.ReceiveNext();
        networkOnGround = (bool)stream.ReceiveNext();
        
        // Estados críticos
        onAttack = (bool)stream.ReceiveNext();
        onJump = (bool)stream.ReceiveNext();
        
        // Stats condicionales
        bool hasStats = (bool)stream.ReceiveNext();
        if (hasStats)
        {
            networkHealth = (float)stream.ReceiveNext();
            networkFood = (float)stream.ReceiveNext();
            networkWater = (float)stream.ReceiveNext();
            networkStamina = (float)stream.ReceiveNext();
            networkIsDead = (bool)stream.ReceiveNext();
            isOnGround = (bool)stream.ReceiveNext();
            isInWater = (bool)stream.ReceiveNext();
            isOnWater = (bool)stream.ReceiveNext();
        }
        
        // Behavior condicional
        bool hasBehavior = (bool)stream.ReceiveNext();
        if (hasBehavior)
        {
            networkBehavior = (string)stream.ReceiveNext();
        }
        
        // Compensación de lag
        float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
        networkPosition += networkVelocity * lag;
        
        // Aplicar datos
        if (!photonView.IsMine)
        {
            anm.SetBool("OnGround", networkOnGround);
            if (hasStats)
            {
                food = networkFood;
                water = networkWater;
                stamina = networkStamina;
                health = networkHealth;
                isDead = networkIsDead;
            }
        }
    }
}

	
	public void OnOwnerChange(Photon.Realtime.Player newOwner, Photon.Realtime.Player previousOwner)
{
    Debug.Log($"OWNERSHIP CHANGE - {gameObject.name}: De {previousOwner?.NickName ?? "NULL"} a {newOwner?.NickName ?? "NULL"}");
    
    // Si no hay nuevo owner (jugador se desconectÃƒÂ³), destruir o transferir
    if(newOwner == null)
    {
        Debug.Log($"Jugador se desconectÃƒÂ³. Destruyendo dinosaurio: {gameObject.name}");
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}

	// Agregar estos RPCs para sincronizar animaciones importantes:
	[PunRPC]
	void SyncJumpState(bool jumping)
	{
		onJump = jumping;
		if(!jumping)
		{
			// Forzar el reseteo del salto en clientes remotos
			anm.SetInteger("Move", 0);
			if(body != null)
			{
				body.velocity = new Vector3(body.velocity.x, 0.0f, body.velocity.z);
			}
		}
	}

	[PunRPC]
	void SyncAttackState(bool attacking)
	{
		onAttack = attacking;
		anm.SetBool("Attack", attacking);
	}

	[PunRPC]
	void SyncAnimationTrigger(string triggerName, int intValue = 0, float floatValue = 0f, bool boolValue = false)
	{
		switch(triggerName)
		{
			case "Move":
				anm.SetInteger("Move", intValue);
				break;
			case "Idle":
				anm.SetInteger("Idle", intValue);
				break;
			case "Attack":
				anm.SetBool("Attack", boolValue);
				onAttack = boolValue;
				break;
			case "Turn":
				anm.SetFloat("Turn", floatValue);
				break;
			case "Pitch":
				anm.SetFloat("Pitch", floatValue);
				break;
			case "OnGround":
				anm.SetBool("OnGround", boolValue);
				isOnGround = boolValue;
				break;
		}
	}

	// MÃƒÂ©todo para enviar sincronizaciÃƒÂ³n de animaciones crÃƒÂ­ticas (llamar cuando sea necesario)
	public void SyncCriticalAnimation(string animName, int intVal = 0, float floatVal = 0f, bool boolVal = false)
	{
		if(photonView != null && photonView.IsMine)
		{
			photonView.RPC("SyncAnimationTrigger", RpcTarget.Others, animName, intVal, floatVal, boolVal);
		}
	}

	// RPCs for important events
[PunRPC]
void TakeDamage(float damageMultiplier, Vector3 hitPoint, string hitCollider, int attackerViewID)
{
    // Debug mejorado
    Debug.Log($"RPC TakeDamage ejecutado en {gameObject.name} - ViewID del atacante: {attackerViewID} - PhotonView.IsMine: {photonView?.IsMine}");
    
    // Evitar daño múltiple en corto tiempo
    if(isDead || lastHit > 0) 
    {
        Debug.Log($"Daño rechazado en {gameObject.name}: isDead={isDead}, lastHit={lastHit}");
        return;
    }

    // Encontrar al atacante
    PhotonView attackerPV = null;
    Creature attacker = null;
    
    if(attackerViewID != -1)
    {
        attackerPV = PhotonView.Find(attackerViewID);
        if(attackerPV == null) 
        {
            Debug.LogWarning($"No se encontró PhotonView del atacante con ID: {attackerViewID}");
            return;
        }

        attacker = attackerPV.GetComponent<Creature>();
        if(attacker == null) 
        {
            Debug.LogWarning($"No se encontró script Creature en atacante: {attackerPV.name}");
            return;
        }
    }
    else
    {
        Debug.LogWarning("AttackerViewID es -1, usando valores por defecto para el daño");
    }

    // FÓRMULA DE DAÑO CORREGIDA
    float finalDamage;
    if(attacker != null)
    {
        // Obtener masa real del atacante (incluye el factor de tamaño)
        float attackerMass = attacker.body.mass;
        float defenderMass = body.mass;
        
        // Obtener multipliers de crecimiento si existen
        CreatureGrowthSystem attackerGrowth = attacker.GetComponent<CreatureGrowthSystem>();
        CreatureGrowthSystem defenderGrowth = GetComponent<CreatureGrowthSystem>();
        
        float attackerDamageMultiplier = attacker.damageMultiplier;
        float defenderArmorMultiplier = armorMultiplier;
        
        // Si hay sistema de crecimiento, los multipliers ya están aplicados correctamente
        // La fórmula ahora usa la masa real (que incluye el factor de tamaño)
        finalDamage = (attackerMass * attackerDamageMultiplier) / (defenderMass * defenderArmorMultiplier);
        
        // Aplicar factor de tamaño adicional para balance
        // Un bebé vs adulto debería hacer mucho menos daño
        float sizeRatio = attacker.size / size; // Relación de tamaños
        finalDamage *= sizeRatio;
        
        // Clamp con valores más realistas
        // Bebés pueden hacer daño muy bajo, adultos hasta 100
        finalDamage = Mathf.Clamp(finalDamage, 0.5f, 100f);
        
        Debug.Log($"CÁLCULO DE DAÑO: Atacante={attacker.gameObject.name} (Masa:{attackerMass:F1}, DmgMult:{attackerDamageMultiplier:F2}, Tamaño:{attacker.size:F2})");
        Debug.Log($"VÍCTIMA: {gameObject.name} (Masa:{defenderMass:F1}, ArmorMult:{defenderArmorMultiplier:F2}, Tamaño:{size:F2})");
        Debug.Log($"RATIO DE TAMAÑO: {sizeRatio:F2} | DAÑO FINAL: {finalDamage:F1}");
    }
    else
    {
        finalDamage = 10f; // Daño por defecto moderado
    }

    // Resto del código de efectos visuales y físicos...
    SpawnBlood(hitPoint);
    
    if(!isInWater && attacker != null)
    {
        Vector3 force = (transform.position - attacker.transform.position).normalized * (attacker.body != null ? attacker.body.mass : body.mass) / 4;
        body.AddForce(force, ForceMode.Acceleration);
    }

    lastHit = 50;
    
    // Sonidos (solo para el owner local para evitar sonidos duplicados)
    if(photonView.IsMine && source != null && source.Length > 0)
    {
        source[0].pitch = Random.Range(1.0f, 1.5f);
        
        // Usar sonidos específicos si están disponibles (RapLP)
        RapLP rapScript = GetComponent<RapLP>();
        if(rapScript != null)
        {
            int rndPainsnd = Random.Range(0, 4);
            AudioClip painSnd = null;
            switch (rndPainsnd) 
            { 
                case 0: painSnd = rapScript.Rap1; break; 
                case 1: painSnd = rapScript.Rap2; break; 
                case 2: painSnd = rapScript.Rap3; break; 
                case 3: painSnd = rapScript.Rap6; break; 
            }
            
            if(painSnd != null) source[0].PlayOneShot(painSnd, 1.0f);
            
            // Sonidos de impacto específicos
            if(hitCollider.StartsWith("jaw"))
            {
                if(source.Length > 1) source[1].PlayOneShot(rapScript.Hit_jaw, Random.Range(0.1f, 0.4f));
            }
            else if(hitCollider.Equals("head"))
            {
                if(source.Length > 1) source[1].PlayOneShot(rapScript.Hit_head, Random.Range(0.1f, 0.4f));
            }
            else
            {
                if(source.Length > 1) source[1].PlayOneShot(rapScript.Hit_tail, Random.Range(0.1f, 0.4f));
            }
        }
    }

    // Aplicar daño basado en hit location
    if(hitCollider.StartsWith("jaw"))
    {
        ApplyDamageWithGrowthSystem(finalDamage);
    }
    else if(hitCollider.Equals("head"))
    {
        float headDamage = herbivorous ? finalDamage / 10 : finalDamage;
        ApplyDamageWithGrowthSystem(headDamage);
    }
    else // tail or body damage
    {
        float bodyDamage = herbivorous ? finalDamage / 10 : finalDamage;
        ApplyDamageWithGrowthSystem(bodyDamage);
    }


    // Debug final para verificar que el daño se aplicó
    Debug.Log($"{gameObject.name} recibió {finalDamage:F1} de daño. Vida restante: {health:F1}");
}

// Función auxiliar para aplicar daño considerando el sistema de crecimiento
void ApplyDamageWithGrowthSystem(float damage)
{
    CreatureGrowthSystem growthSystem = GetComponent<CreatureGrowthSystem>();
    
    if (growthSystem != null && growthSystem.UseDynamicHealth())
    {
        // Usar el sistema de vida dinámico
        growthSystem.TakeDynamicDamage(damage);
    }
    else
    {
        // Usar el sistema tradicional
        health = Mathf.Clamp(health - damage, 0.0f, 100f);
    }
}

	[PunRPC]
	void SyncHealth(float newHealth)
	{
		health = newHealth;
	}

	[PunRPC]
	void ChangeCreatureName(string newName)
	{
		gameObject.name = newName;
	}

	[PunRPC]
	void SetAnimSpeed(float newAnimSpeed)
	{
		animSpeed = newAnimSpeed;
	}

	[PunRPC]
	void SetHealth(float newHealth)
	{
		health = newHealth;
	}

	[PunRPC]
	void SetFood(float newFood)
	{
		food = newFood;
	}

	[PunRPC]
	void SetWater(float newWater)
	{
		water = newWater;
	}

	[PunRPC]
	void SetStamina(float newStamina)
	{
		stamina = newStamina;
	}

	[PunRPC]
	void SetDamageMultiplier(float newDamage)
	{
		damageMultiplier = newDamage;
	}

	[PunRPC]
	void SetArmorMultiplier(float newArmor)
	{
		armorMultiplier = newArmor;
	}

	[PunRPC]
	public void ConfigureNewCreature(bool spawnAI, bool rndSkin, bool rndSize, bool rndSetting, int rndSizeSpan, string creatureName)
	{
		// Primero establece el nombre (importante para debugging)
		gameObject.name = creatureName;
		
		// Log para debugging
		Debug.Log($"Configurando dinosaurio: {creatureName}, AI: {spawnAI}, RandomSkin: {rndSkin}");
		
		// Configurar AI
		useAI = spawnAI;
		
		// Aplicar skins aleatorios o personalizaciÃƒÂ³n
		if(rndSkin) 
		{ 
			int bodyIdx = Random.Range(0, 3);
			int eyesIdx = Random.Range(0, 16);
			bodyTexture = (Skin)bodyIdx; 
			eyesTexture = (Eyes)eyesIdx;
			
			// Asegurarse de que rend no sea null y tenga elementos
			if(rend != null && rend.Length > 0)
			{
				foreach(SkinnedMeshRenderer o in rend)
				{
					if(o != null && o.materials.Length >= 2 && skin.Length > bodyIdx && eyes.Length > eyesIdx)
					{
						o.materials[0].mainTexture = skin[bodyIdx];
						o.materials[1].mainTexture = eyes[eyesIdx];
					}
				}
			}
		}
		
		// Configurar tamaÃƒÂ±o (aleatorio o fijo)
		if(rndSize) 
		{ 
			float newSize = 0.5f + Random.Range((float)rndSizeSpan / -10, (float)rndSizeSpan / 10);
			SetSizeWithScale(newSize);
		} 
		else 
		{
			SetSizeWithScale(0.5f);
		}
		
		// Configurar estado (salud, comida, etc.)
		if(rndSetting)
		{
			health = 100; 
			stamina = Random.Range(0, 100);
			food = Random.Range(0, 100); 
			water = Random.Range(0, 100);
		}
		else
		{
			// Valores por defecto
			health = 100;
			stamina = 100;
			food = 100;
			water = 100;
		}
	}

	// MÃƒÂ©todo auxiliar para establecer el tamaÃƒÂ±o y actualizar valores relacionados
	private void SetSizeWithScale(float newSize)
	{
		size = newSize;
		transform.localScale = new Vector3(newSize, newSize, newSize);
		
		// Actualizar masa del cuerpo
		if(body != null)
		{
			    CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
				if (growthSys == null || !growthSys.enableGrowthSystem)
				{
					body.mass = baseMass * size;
				}
		}
		
		// Actualizar tamaÃƒÂ±o de withers
		if(transform.childCount > 0 && transform.GetChild(0).childCount > 0)
		{
			withersSize = (transform.GetChild(0).GetChild(0).position - transform.position).magnitude;
		}
		
		// Actualizar escala de caja
		if(rend != null && rend.Length > 0 && rend[0] != null)
		{
			boxscale = rend[0].bounds.extents;
		}
		
		// Ajustar distancias de sonido
		if(source != null && source.Length > 1)
		{
			source[0].maxDistance = Mathf.Lerp(50f, 300f, size);
			source[1].maxDistance = Mathf.Lerp(50f, 150f, size);
		}
	}

	bool IsOwner()
	{
		return photonView == null || photonView.IsMine;
	}
	#endregion
	
	#region CREATURE INITIALIZATION
	void Start()
	{
		// Initialize standalone creature
		InitializeStandalone();
		    // NUEVO: Inicializar tiempo de última actualización
    lastNetworkUpdate = Time.time;
    lastKnownPosition = transform.position;
	
	// NUEVO: Configurar Interest Groups
	if(photonView != null && photonView.IsMine)
	{
		PhotonNetwork.SendRate = 15;
		PhotonNetwork.SerializationRate = 15;
		
		// Delay para asegurar que otros jugadores ya están en sala
		StartCoroutine(DelayedInterestGroupSetup());
	}
		SetScale(transform.localScale.x);
		SetMaterials(bodyTexture.GetHashCode(), eyesTexture.GetHashCode());
		loop = Random.Range(0, 100);
		specie = transform.GetChild(0).name;

		// Initialize network variables
		networkPosition = transform.position;
		networkRotation = transform.rotation;
		networkHealth = health;
		networkBehavior = behavior;
		networkFood = food;
		networkWater = water;
		networkStamina = stamina;

		// Add this creature to the global list
		if(!allCreatures.Contains(this))
		{
			allCreatures.Add(this);
		}
		
		// Initialize UI bars
		UpdateUIBars();
		
		if(photonView != null && photonView.IsMine)
		{
			InvokeRepeating("SendHeartbeat", 2.5f, 2.5f);
		}
		
		// El callback de ownership change se maneja automÃƒÂ¡ticamente por la interface IOnPhotonViewOwnerChange
	}
	
	/// <summary>
	/// Envía señal de "estoy vivo" cada 2.5 segundos para evitar que dinosaurios se oculten
	/// Solo consume ~18 bytes por envío (0.65% del tráfico total)
	/// </summary>
	void SendHeartbeat()
	{
		// Solo enviar si somos el dueño y no estamos muertos
		//if(photonView != null && photonView.IsMine && !isDead)
			    // Enviar incluso si está muerto (cadáveres deben verse)
			if(photonView != null && photonView.IsMine)
		{
			photonView.RPC("ReceiveHeartbeat", RpcTarget.Others);
		}
	}
	
	/// <summary>
	/// RPC que actualiza el timestamp de última actualización
	/// Previene que dinosaurios cercanos se oculten por falta de sincronización
	/// Consumo: ~18 bytes por llamada
	/// </summary>
	[PunRPC]
	void ReceiveHeartbeat()
	{
		// Simplemente actualizar el timestamp de última actualización de red
		lastNetworkUpdate = Time.time;
		
		// Debug opcional - descomentar para ver heartbeats en consola
		// Debug.Log($"[Heartbeat] ✓ {gameObject.name} - Grupo: {photonView.Group}, Tiempo: {Time.time:F1}s");
	}
	
	IEnumerator DelayedInterestGroupSetup()
	{
		// Esperar a que la red se estabilice
		yield return new WaitForSeconds(0.5f);
		
		// Forzar actualización inicial
		UpdateInterestGroup();
		
		// Forzar segunda actualización para asegurar
		yield return new WaitForSeconds(0.5f);
		UpdateInterestGroup();
		
		Debug.Log($"[Interest] {gameObject.name} configurado en grupo {currentInterestGroup}");
	}

	void InitializeStandalone()
	{
		// Find main camera
		if(mainCamera == null)
		{
			mainCamera = Camera.main;
			if(mainCamera == null)
			{
				mainCamera = FindObjectOfType<Camera>();
			}
		}

		// Get terrain data
		if(Terrain.activeTerrain)
		{
			t = Terrain.activeTerrain;
			tdata = t.terrainData;
			tpos = t.GetPosition();
			tres = tdata.heightmapResolution;
		}

		// Layers left-shift
		treeLayer = (1 << treeLayer);

		// Initialize creature-specific variables
		if(Head == null && transform.childCount > 0)
		{
			// Try to find head automatically
			Transform[] children = GetComponentsInChildren<Transform>();
			foreach(Transform child in children)
			{
				if(child.name.ToLower().Contains("head"))
				{
					Head = child;
					break;
				}
			}
		}
	}

void Update()
{
    // Clean up null references in allCreatures list
    allCreatures.RemoveAll(c => c == null);

    // Solo aplicar datos de red si NO somos el owner
    if (photonView != null && !photonView.IsMine)
    {
        // Interpolación más suave de posición y rotación
        float lerpRate = Time.deltaTime * 20f; // Aumentar velocidad de interpolación
        
        transform.position = Vector3.Lerp(transform.position, networkPosition, lerpRate);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, lerpRate);
        
        // Aplicar datos de animación de red INMEDIATAMENTE para mejor sincronización
        anm.SetFloat("Turn", networkTurn);
        anm.SetInteger("Move", networkMove);
        anm.SetInteger("Idle", networkIdle);
        anm.SetFloat("Pitch", networkPitch);
        anm.SetBool("Attack", networkAttack);
        
        // CRÍTICO: Aplicar estados de combate recibidos
        // onAttack y onJump ya se aplicaron directamente en OnPhotonSerializeView
        
        // Aplicar estado de red
        health = networkHealth;
        isDead = networkIsDead;
        behavior = networkBehavior;
        
        // Aplicar velocidad para predicción más precisa
        if(body != null)
        {
            body.velocity = Vector3.Lerp(body.velocity, networkVelocity, Time.deltaTime * 15f);
        }
    }
    
    // Asegurar que el objeto esté activo y visible
    if(photonView != null && !photonView.IsMine)
    {
        // Forzar que el objeto remoto sea visible
        gameObject.SetActive(true);
        
        // Asegurar que el renderer esté habilitado
        if(rend != null && rend.Length > 0)
        {
            foreach(var renderer in rend)
            {
                if(renderer != null) renderer.enabled = true;
            }
        }
    }
    
    // Actualizar estado de botones y UI (solo para el owner)
    if(IsOwner())
    {
        UpdateFoodDrinkButtons();
        UpdateUIBars();
        ProcessStaminaConsumption();
    }
    
    // NUEVO: Actualizar Interest Group cada 2 segundos
    if(photonView != null && photonView.IsMine && Time.time - lastInterestGroupUpdate > INTEREST_UPDATE_INTERVAL)
    {
        UpdateInterestGroup();
        lastInterestGroupUpdate = Time.time;
    }
    
    // ========================================
    // ✅ OPCIÓN B: Sistema de ocultamiento inteligente
    // ========================================
    if(photonView != null && !photonView.IsMine)
    {
        float timeSinceLastUpdate = Time.time - lastNetworkUpdate;
        
        // ✅ EXCEPCIONES: Estados que NO deben ocultarse
        bool isStationary = (anm.GetInteger("Move") == 0); // Parado
        bool isSleeping = OnAnm.IsName(specie+"|Sleep") || OnAnm.IsName(specie+"|SitIdle"); // Dormido
        bool isNearby = mainCamera != null && Vector3.Distance(transform.position, mainCamera.transform.position) < 300f; // Cerca (<50m)
        
        // 🎯 LÓGICA INTELIGENTE:
        // - Si está CERCA (<50m) Y en estado protegido → NUNCA ocultar
        // - Si está LEJOS (>50m) sin updates → Ocultar después de maxTimeWithoutUpdate (correcto)
        bool shouldBeVisible = timeSinceLastUpdate < maxTimeWithoutUpdate || 
                              isDead || 
                              isStationary || 
                              isSleeping || 
                              isNearby;
        
        // Ocultar/mostrar renderers
        if(rend != null && rend.Length > 0)
        {
            foreach(var renderer in rend)
            {
                if(renderer != null) 
                    renderer.enabled = shouldBeVisible;
            }
        }
        
        // Debug mejorado (cada 5 segundos)
        if(Time.frameCount % 300 == 0 && !shouldBeVisible)
        {
            Debug.Log($"[Ghost] {gameObject.name} oculto - Tiempo={timeSinceLastUpdate:F1}s | Muerto={isDead} | Parado={isStationary} | Dormido={isSleeping} | Cerca={isNearby}");
        }
    }
}

	void OnDestroy()
	{
			//Detener heartbeat al destruir el objeto
			CancelInvoke("SendHeartbeat");
		// Remove from global list when destroyed
		if(allCreatures.Contains(this))
		{
			allCreatures.Remove(this);
		}
	}
	#endregion
	
	#region UI MANAGEMENT
	// NUEVA FUNCIÃ“N: Actualizar barras de UI
void UpdateUIBars()
{
    if(healthBar != null)
    {
        // USAR SISTEMA DE CRECIMIENTO DINÁMICO SI EXISTE
        CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
        float healthPercentage;
        
        if (growthSys != null && growthSys.UseDynamicHealth())
        {
            // Usar el porcentaje correcto del sistema de crecimiento
            healthPercentage = growthSys.GetHealthPercentage();
        }
        else
        {
            // Fallback al sistema original (vida máxima = 100)
            healthPercentage = health / 100f;
        }
        
        healthBar.fillAmount = healthPercentage;
        
        // Cambiar color según el nivel de vida (usar porcentaje, no valor absoluto)
        if(healthPercentage > 0.6f) healthBar.color = Color.green;
        else if(healthPercentage > 0.3f) healthBar.color = Color.yellow;
        else healthBar.color = Color.red;
    }
    
    if(foodBar != null)
    {
        foodBar.fillAmount = food / 100f;
        
        // Cambiar color según el nivel de hambre
        if(food > 60f) foodBar.color = new Color(1f, 0.5f, 0f); // Naranja
        else if(food > 30f) foodBar.color = Color.yellow;
        else foodBar.color = Color.red;
    }
    
    if(waterBar != null)
    {
        waterBar.fillAmount = water / 100f;
        
        // Cambiar color según el nivel de sed
        if(water > 60f) waterBar.color = Color.cyan;
        else if(water > 30f) waterBar.color = Color.blue;
        else waterBar.color = Color.red;
    }
    
    if(staminaBar != null)
    {
        staminaBar.fillAmount = stamina / 100f;
        
        // Cambiar color según el nivel de estamina
        if(stamina > 60f) staminaBar.color = Color.green;
        else if(stamina > 30f) staminaBar.color = Color.yellow;
        else staminaBar.color = Color.red;
    }
}

void UpdateDashButton()
{
    if(dashButton != null)
    {
        // Solo habilitar para criaturas acuáticas
        bool canDash = canSwim && 
                      (isInWater || isOnWater) && 
                      stamina >= dashStaminaCost && 
                      dashCooldownTimer <= 0f && 
                      !isDead && 
                      !isDashing;
        
        dashButton.interactable = canDash;
        
        // Cambiar color según disponibilidad
        ColorBlock colors = dashButton.colors;
        
        if(isDashing)
        {
            colors.normalColor = Color.red; // Rojo durante dash
        }
        else if(dashCooldownTimer > 0f)
        {
            colors.normalColor = Color.gray; // Gris durante cooldown
        }
        else if(canDash)
        {
            colors.normalColor = Color.cyan; // Cian cuando disponible
        }
        else
        {
            colors.normalColor = Color.white; // Blanco por defecto
        }
        
        dashButton.colors = colors;
        
        // Opcional: Mostrar tiempo de cooldown en el botón
        Text buttonText = dashButton.GetComponentInChildren<Text>();
        if(buttonText != null)
        {
            if(dashCooldownTimer > 0f)
            {
                buttonText.text = $"DASH\n{dashCooldownTimer:F1}s";
            }
            else
            {
                buttonText.text = "DASH";
            }
        }
    }
}
	
	// NUEVA FUNCIÃ“N: Procesar consumo de estamina
void ProcessStaminaConsumption()
{
    if(isDead) return;
    
    int currentMove = anm.GetInteger("Move");
    bool isRunning = (currentMove == 2);
	bool isWalking = (currentMove == 1);
    bool isFlying = (canFly && !isOnGround && currentMove > 0);
    bool isHovering = (canFly && !isOnGround && currentMove == 0);
	
	    if(OnAnm.IsName(specie+"|Sleep") || OnAnm.IsName(specie+"|SitIdle"))
    {
        float sleepRegenRate = staminaRegenRate * 8.0f;
        stamina = Mathf.Clamp(stamina + (sleepRegenRate * Time.deltaTime), 0.0f, 100f);
        
        UpdateDashButton();
        if(runButton != null) runButton.interactable = (stamina >= minStaminaToRun);
        
        return; // Salir para no procesar otras condiciones
    }
    
    // DASH SYSTEM
    if(isDashing)
    {
        // CORRECCIÓN: No drenar stamina extra durante dash, ya se consumió al inicio
        // stamina = Mathf.Clamp(stamina - (dashStaminaCost * Time.deltaTime), 0.0f, 100f);
        
        // Reducir timer del dash
        dashTimer -= Time.deltaTime;
        if(dashTimer <= 0f)
        {
            isDashing = false;
            dashCooldownTimer = dashCooldown; // Iniciar cooldown
            Debug.Log("DASH TERMINADO");
        }
    }
    
    // Reducir cooldown timer
    if(dashCooldownTimer > 0f)
    {
        dashCooldownTimer -= Time.deltaTime;
    }
    
    // Consumir estamina al correr o volar (SOLO si no está en dash)
    if(!isDashing && (isFlying || isHovering))
    {
        float drainRate = flyStaminaDrain;
        
        if(isFlying) drainRate *= 1.5f;
        else if(isHovering) drainRate *= 0.8f;
        
        stamina = Mathf.Clamp(stamina - (drainRate * Time.deltaTime), 0.0f, 100f);
        
        if(stamina <= 0f)
        {
            anm.SetInteger("Move", 0);
            anm.SetBool("OnGround", false);
        }
    }
    else if(!isDashing && isRunning)
    {
        stamina = Mathf.Clamp(stamina - (runStaminaDrain * Time.deltaTime), 0.0f, 100f);
        
        if(stamina <= 0f)
        {
            anm.SetInteger("Move", 1);
        }
    }
	else if(isWalking)
    {
        // NUEVO: CAMINAR - Regenera stamina lentamente
        // Regeneración al 30% de la velocidad normal de reposo
        float walkingRegenRate = staminaRegenRate * 1.2f;
        
        stamina = Mathf.Clamp(stamina + (walkingRegenRate * Time.deltaTime), 0.0f, 100f);
    }
    else if(currentMove == 0 || OnAnm.IsName(specie+"|Sleep"))
    {
        float regenRate = staminaRegenRate;
        
        if(currentMove == 0 && isOnGround) regenRate *= 1.5f;
        
        stamina = Mathf.Clamp(stamina + (regenRate * Time.deltaTime), 0.0f, 100f);
    }
    
    // Deshabilitar botón de correr si no hay suficiente estamina
    if(runButton != null)
    {
        bool canRun = stamina >= minStaminaToRun;
        runButton.interactable = canRun;
        
        if(!canRun && isRunToggled)
        {
            isRunToggled = false;
            ColorBlock colors = runButton.colors;
            colors.normalColor = Color.white;
            runButton.colors = colors;
        }
    }
    
    UpdateDashButton();
    
    if(canFly && stamina <= 5f && !isOnGround)
    {
        if(staminaBar != null)
        {
            staminaBar.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 3f, 1f));
        }
    }
}

	
	// NUEVA FUNCIÃ“N: Detectar si hay comida disponible sin modificar el target
	bool CanEat()
	{
		//Find carnivorous food (looking for a dead creature in range)
		if(!herbivorous)
		{
			foreach(Creature other in allCreatures)
			{
				if(other == null || other == this) continue;
				if((other.transform.position-Head.position).magnitude>boxscale.z) continue; //not in range
				if(other.isDead) return true; // meat found
			}
		}
		else
		{
			//Find herbivorous food (looking for trees/details on terrain in range )
			if(t)
			{
				//Large creature, look for trees
				if(withersSize>8)
				{
					if(Physics.CheckSphere(Head.position,withersSize,treeLayer)) return true;
					else return false;
				}
				//Look for grass detail
				else
				{
					float x=((transform.position.x-t.transform.position.x)/tdata.size.z*tres);
					float y=((transform.position.z-t.transform.position.z)/tdata.size.x*tres);

					for(int layer=0;layer<tdata.detailPrototypes.Length;layer++)
					{
						if(tdata.GetDetailLayer((int)x,(int)y,1,1,layer)[0,0]>0)
						{
							return true;
						}
					}
				}
			}
		}

		return false; //nothing found...
	}
	
	// NUEVA FUNCIÃ“N: Actualizar estado de botones de comida y bebida
	void UpdateFoodDrinkButtons()
	{
		// Verificar si se puede comer
		bool canEatNow = CanEat() && food < 100f && !isDead;
		
		// Verificar si se puede beber
		bool canDrinkNow = isOnWater && water < 100f && !isDead;
		
		// Actualizar botÃ³n EAT
		if(eatButton != null)
		{
			eatButton.interactable = canEatNow;
			
			// Cambiar color del botÃ³n segÃºn disponibilidad
			ColorBlock eatColors = eatButton.colors;
			if(canEatNow)
			{
				eatColors.normalColor = Color.green; // Verde cuando hay comida disponible
				eatColors.disabledColor = Color.green;
			}
			else
			{
				eatColors.normalColor = Color.gray; // Gris cuando no hay comida
				eatColors.disabledColor = Color.gray;
			}
			eatButton.colors = eatColors;
			
			// Opcional: Ocultar completamente el botÃ³n si no se puede usar
			// eatButton.gameObject.SetActive(canEatNow);
		}
		
		// Actualizar botÃ³n DRINK
		if(drinkButton != null)
		{
			drinkButton.interactable = canDrinkNow;
			
			// Cambiar color del botÃ³n segÃºn disponibilidad
			ColorBlock drinkColors = drinkButton.colors;
			if(canDrinkNow)
			{
				drinkColors.normalColor = Color.cyan; // Cian cuando hay agua disponible
				drinkColors.disabledColor = Color.cyan;
			}
			else
			{
				drinkColors.normalColor = Color.gray; // Gris cuando no hay agua
				drinkColors.disabledColor = Color.gray;
			}
			drinkButton.colors = drinkColors;
			
			// Opcional: Ocultar completamente el botÃ³n si no se puede usar
			// drinkButton.gameObject.SetActive(canDrinkNow);
		}
	}
	#endregion
	
	#region CREATURE SETUP FUNCTIONS
	//AI on/off
	public void ChangeAI(bool UseAI) 
	{ 
		if(photonView != null && !photonView.IsMine) return;
		
		this.useAI = UseAI; 
		if(!this.useAI) 
		{ 
			posTGT = Vector3.zero; 
			objTGT = null; 
			objCOL = null; 
			behaviorCount = 0; 
		}
		
		// Sync over network
		if(photonView != null)
			photonView.RPC("SetAI", RpcTarget.Others, UseAI);
	}
	
	[PunRPC]
	void SetAI(bool useAI)
	{
		this.useAI = useAI;
		if(!this.useAI) 
		{ 
			posTGT = Vector3.zero; 
			objTGT = null; 
			objCOL = null; 
			behaviorCount = 0; 
		}
	}
	
	//Change materials
#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		if(photonView != null && !photonView.IsMine) return;
		
		foreach(SkinnedMeshRenderer o in rend)
		{
			if(o.sharedMaterials[0].mainTexture != skin[bodyTexture.GetHashCode()]) 
				o.sharedMaterials[0].mainTexture = skin[bodyTexture.GetHashCode()];
			if(o.sharedMaterials[1].mainTexture != eyes[eyesTexture.GetHashCode()]) 
				o.sharedMaterials[1].mainTexture = eyes[eyesTexture.GetHashCode()];
		}
	}
#endif

	public void ChangeMaterials(int bodyindex, int eyesindex)
	{
		if(photonView != null && !photonView.IsMine) return;
		
		bodyTexture = (Skin)bodyindex; 
		eyesTexture = (Eyes)eyesindex;
		foreach(SkinnedMeshRenderer o in rend)
		{
			o.materials[0].mainTexture = skin[bodyindex];
			o.materials[1].mainTexture = eyes[eyesindex];
		}
		
		// Sync over network
		if(photonView != null)
			photonView.RPC("SetMaterials", RpcTarget.Others, bodyindex, eyesindex);
	}
	
	[PunRPC]
	public void SetMaterials(int bodyindex, int eyesindex)
	{
		bodyTexture = (Skin)bodyindex; 
		eyesTexture = (Eyes)eyesindex;
		foreach(SkinnedMeshRenderer o in rend)
		{
			o.materials[0].mainTexture = skin[bodyindex];
			o.materials[1].mainTexture = eyes[eyesindex];
		}
	}

	//Creature size
	public void ChangeScale(float resize)
	{
		if(photonView != null && !photonView.IsMine) return;
		
		size = resize;
		transform.localScale = new Vector3(resize, resize, resize);
		// Solo calcular masa si NO hay GrowthSystem
		CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
		if (growthSys == null || !growthSys.enableGrowthSystem)
		{
			body.mass = baseMass * size;
		}
		withersSize = (transform.GetChild(0).GetChild(0).position - transform.position).magnitude;
		boxscale = rend[0].bounds.extents;
		if(source != null && source.Length > 1)
		{
			source[0].maxDistance = Mathf.Lerp(50f, 300f, size);
			source[1].maxDistance = Mathf.Lerp(50f, 150f, size);
		}
		
		// Sync over network
		if(photonView != null)
			photonView.RPC("SetScale", RpcTarget.Others, resize);
	}
	
	[PunRPC]
	public void SetScale(float resize)
	{
		size = resize;
		transform.localScale = new Vector3(resize, resize, resize);
		// Solo calcular masa si NO hay GrowthSystem
		CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
		if (growthSys == null || !growthSys.enableGrowthSystem)
		{
			body.mass = baseMass * size;
		}
		withersSize = (transform.GetChild(0).GetChild(0).position - transform.position).magnitude;
		boxscale = rend[0].bounds.extents;
		if(source != null && source.Length > 1)
		{
			source[0].maxDistance = Mathf.Lerp(50f, 300f, size);
			source[1].maxDistance = Mathf.Lerp(50f, 150f, size);
		}
	}
	#endregion 
	
	#region CREATURE STATUS UPDATE
	private float GetMaxHealthValue()
{
    CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
    if (growthSys != null && growthSys.UseDynamicHealth())
    {
        return growthSys.GetCurrentMaxHealth();
    }
    return 100f;
}

public void StatusUpdate()
{
    // Only update status for owned creatures
    if(!IsOwner()) return;
    
    // Check if this creature is visible or near the camera
    isVisible = false;
    foreach(SkinnedMeshRenderer o in rend) { if(o.isVisible) isVisible = true; }
    
    if(!realtimeGame && mainCamera != null)
    {
        float dist = (mainCamera.transform.position - transform.position).magnitude;
        if(!isVisible && dist > 100f) { isActive = false; anm.cullingMode = AnimatorCullingMode.CullCompletely; return; }
        else { isActive = true; anm.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
    }
    else { isActive = true; anm.cullingMode = AnimatorCullingMode.AlwaysAnimate; }

    anm.speed = animSpeed;
    if(anm.GetNextAnimatorClipInfo(0).Length != 0) OnAnm = anm.GetNextAnimatorStateInfo(0);
    else if(anm.GetCurrentAnimatorClipInfo(0).Length != 0) OnAnm = anm.GetCurrentAnimatorStateInfo(0);
    
    // Sincronizar onAttack con el estado del animador
    if(canAttack)
    {
        onAttack = anm.GetBool("Attack");
    }

    if(currframe == 15f | anm.GetAnimatorTransitionInfo(0).normalizedTime > 0.5) { currframe = 0.0f; lastframe = -1; }
    else currframe = Mathf.Round((OnAnm.normalizedTime % 1.0f) * 15f);

    // Manage health bar with enhanced consumption rates
    if(health > 0.0f)
    {
        if(loop > 100)
        {
            // Obtener movimiento actual para ajustar consumo
            int currentMove = anm.GetInteger("Move");
            bool isMoving = (currentMove != 0);
            bool isRunning = (currentMove == 2);
            bool isFlying = (canFly && !isOnGround && isMoving);
            
            // Calcular multiplicadores de consumo basados en actividad
            float hungerMultiplier = 1.0f;
            float thirstMultiplier = 1.0f;
            
            if(isRunning || isFlying)
            {
                hungerMultiplier = 30.0f; // Corriendo o volando consume más hambre
                thirstMultiplier = 25.5f; // Y más sed
            }
            else if(isMoving)
            {
                hungerMultiplier = 15.5f; // Caminando consume un poco más
                thirstMultiplier = 12.2f;
            }
            
            // SISTEMA ESPECÍFICO PARA ACUÁTICOS
            if(canSwim)
            {
                if(isMoving) food = Mathf.Clamp(food - (0.01f * hungerMultiplier), 0.0f, 100f);
                
                if(isInWater | isOnWater) { 
                    stamina = Mathf.Clamp(stamina + 1.0f, 0.0f, 100f); 
                    water = Mathf.Clamp(water + 1.0f, 0.0f, 100f); 
                }
                else if(canWalk) { 
                    stamina = Mathf.Clamp(stamina - (0.01f * (isRunning ? 2.0f : 1.0f)), 0.0f, 100f); 
                    water = Mathf.Clamp(water - (0.01f * thirstMultiplier), 0.0f, 100f); 
                }
                else { 
                    // Solo pierden stamina y water, NO vida
                    stamina = Mathf.Clamp(stamina - 0.3f, 0.0f, 100f); 
                    water = Mathf.Clamp(water - 0.3f, 0.0f, 100f); 
                }
                
                // REGENERACIÓN SOLO cuando está completamente inmóvil (Move = 0)
                if(currentMove == 0) // SOLO cuando Move = 0 (completamente parado)
                {
                    CreatureGrowthSystem idleGrowthSys = GetComponent<CreatureGrowthSystem>();
                    if (idleGrowthSys != null && idleGrowthSys.UseDynamicHealth())
                    {
                        if(isInWater | isOnWater)
                        {
                            //idleGrowthSys.HealDynamic(0.7f); // Regeneración rápida en agua
							idleGrowthSys.HealDynamic(2f);
                        }
                        else
                        {
                            //idleGrowthSys.HealDynamic(0.2f); // Regeneración más lenta fuera del agua
							idleGrowthSys.HealDynamic(0.5f);
                        }
                    }
                    else
                    {
                        float maxHealth = GetMaxHealthValue();
                        if(isInWater | isOnWater)
                        {
                            health = Mathf.Clamp(health + 1.0f, 0.0f, maxHealth); // Rápida en agua
                        }
                        else
                        {
                            health = Mathf.Clamp(health + 0.5f, 0.0f, maxHealth); // Lenta fuera del agua
                        }
                    }
                }
            }
            // SISTEMA PARA CRIATURAS TERRESTRES
            else
            {
                if(isMoving) { 
                    stamina = Mathf.Clamp(stamina - (0.01f * (isRunning ? 2.0f : 1.0f)), 0.0f, 100f); 
                    water = Mathf.Clamp(water - (0.025f * thirstMultiplier), 0.0f, 100f); 
                    food = Mathf.Clamp(herbivorous ? food - (0.015f * hungerMultiplier) : food - (0.015f * hungerMultiplier), 0.0f, 100f); 
                }
                if(isInWater) { 
                    stamina = Mathf.Clamp(stamina - 1.0f, 0.0f, 100f); 
                    
                    CreatureGrowthSystem growthComponent = GetComponent<CreatureGrowthSystem>();
                    if (growthComponent != null && growthComponent.UseDynamicHealth())
                    {
                        growthComponent.TakeDynamicDamage(1.0f);
                    }
                    else
                    {
                        float maxHealth = GetMaxHealthValue();
                        health = Mathf.Clamp(health - 1.0f, 0.0f, maxHealth);
                    }
                }
            }

            // Efectos de stats bajos en la salud - USANDO VIDA MÁXIMA DINÁMICA
            CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
            float maxHealthToUse = GetMaxHealthValue(); // Usar función dinámica

            if(food == 0.0f | stamina == 0.0f | water == 0.0f) 
            {
                if (growthSys != null && growthSys.UseDynamicHealth())
                {
                    growthSys.TakeDynamicDamage(0.1f);
                }
                else
                {
                    health = Mathf.Clamp(health - 0.1f, 0.0f, maxHealthToUse);
                }
            } 
            else 
            {
                if (growthSys != null && growthSys.UseDynamicHealth())
                {
                    growthSys.HealDynamic(0.05f);
                }
                else
                {
                    health = Mathf.Clamp(health + 0.05f, 0.0f, maxHealthToUse);
                }
            }
            
            loop = 0;
        }
        else loop++;
    }
    else
    {
        if(!isDead)
        {
            isDead = true;
            // Notify death over network
            if(photonView != null)
                photonView.RPC("SyncHealth", RpcTarget.Others, 0f);
        }
        
        water = 0.0f; food = 0.0f; stamina = 0.0f; behavior = "Dead";
        if(timeAfterDead == 0) return;
        if(behaviorCount > 0) behaviorCount = 0;
        else if(behaviorCount == -timeAfterDead)
        {
            //Remove from list and destroy gameobject
            allCreatures.Remove(this);
            
            if(photonView != null)
                PhotonNetwork.Destroy(gameObject);
            else
                Destroy(transform.gameObject);
        }
        else behaviorCount--;
		}
	}
	#endregion
	
	#region COLLISIONS AND DAMAGES
	//Spawn blood particle
	public void SpawnBlood(Vector3 position)
	{
		if(bloodParticle != null)
		{
			ParticleSystem particle = Instantiate(bloodParticle, position, Quaternion.Euler(-90, 0, 0)) as ParticleSystem;
			particle.transform.localScale = new Vector3(boxscale.z / 10, boxscale.z / 10, boxscale.z / 10);
			Destroy(particle.gameObject, 1.0f);
		}
	}

	//Collisions
	public void OnCollisionExit(Collision col) { if(IsOwner()) objCOL = null; }
	
public void ManageCollision(Collision col, AudioSource[] source, AudioClip pain, AudioClip Hit_jaw, AudioClip Hit_head, AudioClip Hit_tail)
{
    //if(!IsOwner()) return; // Solo el owner de ESTA criatura procesa sus colisiones
	if(!IsOwner() && !useAI) return; // Solo salir si no es owner Y no es IA
    
    //Collided with a Creature
    if(col.transform.root.CompareTag("Creature"))
    {
        Creature other = col.gameObject.GetComponent<Creature>(); 
        if(other == null) return;
        
        objCOL = other.gameObject;

        //Is Player attacking?
        if(!useAI && onAttack)
        {
            objTGT = other.gameObject; 
            behaviorCount = 500; 
            
            // Send attack to other creature
            if(other.photonView != null)
            {
                other.photonView.RPC("SetBehaviorFromAttack", RpcTarget.All, photonView.ViewID, specie);
            }
            
            if(other.specie == specie) { behavior = "Contest"; }
            else if(other.canAttack) { behavior = "Battle"; }
            else { behavior = "Battle"; }
        }
        
	//Eat ?
		if(other.isDead && !herbivorous && isConstrained)
		{
			// CORREGIDO: Verificar si podemos comer (cadáver muerto y nosotros en pose de comer)
			if(other.photonView != null && other.photonView.Owner != null)
			{
				// Jugador conectado - usar RPC normal
				other.photonView.RPC("TakeDamage", RpcTarget.All, 1f, col.GetContact(0).point, "body", photonView != null ? photonView.ViewID : -1);
			}
			else
			{
				// CORREGIDO: Jugador desconectado - aplicar daño LOCAL al cadáver
				Debug.Log($"Comiendo cadáver sin owner: {other.gameObject.name}");
				
				// Reducir vida del cadáver
				other.health = Mathf.Clamp(other.health - 1f, 0.0f, 100f);
				
				// Efectos visuales
				other.SpawnBlood(col.GetContact(0).point);
				
				// CORREGIDO: Cooldown en EL CADÁVER para evitar comerse todo instantáneamente
				other.lastHit = 50;
				
				// NUEVO: Restaurar comida/agua del carnívoro que come
				food = Mathf.Clamp(food + 0.05f, 0.0f, 100f);
				if(water < 25) water = Mathf.Clamp(water + 0.05f, 0.0f, 100f);
				
				// Sonidos de comer
				if(source != null && source.Length > 0)
				{
					source[0].pitch = Random.Range(0.8f, 1.2f);
					source[0].Play();
				}
				
				Debug.Log($"Cadáver comido. Vida del cadáver: {other.health}, Mi comida: {food}");
			}
			
			// CRÍTICO: Salir inmediatamente para NO ejecutar la lógica de combate
			return;
		}
			//Attack ? - CAMBIO CRÃƒTICO: Solo procesamos daÃƒÂ±o si NOSOTROS somos la vÃƒÂ­ctima Y el otro estÃƒÂ¡ atacando
        else if(lastHit == 0 && other.onAttack) // Quitamos other.IsOwner() - no importa quiÃƒÂ©n es owner del atacante
        {
            string hitCollider = col.collider.gameObject.name;
            
            Debug.Log($"PROCESANDO DAÃƒ'O: {gameObject.name} recibe ataque de {other.gameObject.name}");
            
            // SIEMPRE usar RPC para sincronizar daÃƒÂ±o - pero solo SI NOSOTROS somos owner de la vÃƒÂ­ctima
            if(photonView != null)
            {
                photonView.RPC("TakeDamage", RpcTarget.All, 1f, col.GetContact(0).point, hitCollider, other.photonView != null ? other.photonView.ViewID : -1);
            }
            else
            {
                // Fallback para modo local (sin red)
                ProcessLocalDamage(other, hitCollider, col, source, pain, Hit_jaw, Hit_head, Hit_tail);
            }
        }

        //Not the current target creature, avoid and look at
        if(objTGT != objCOL) { lookTGT = other.Head.position; posCOL = col.GetContact(0).point; }
    }
    //Collided with world, avoid
    else if(col.gameObject != objTGT)
    {
        objCOL = col.gameObject;
        posCOL = col.GetContact(0).point;
    }
}


	// MÃƒÂ©todo auxiliar para procesar daÃƒÂ±o local (sin red)
	private void ProcessLocalDamage(Creature attacker, string hitCollider, Collision col, AudioSource[] source, AudioClip pain, AudioClip Hit_jaw, AudioClip Hit_head, AudioClip Hit_tail)
	{
		float baseDamages = Mathf.Clamp((attacker.baseMass * attacker.damageMultiplier) / (baseMass * armorMultiplier), 10, 100);

		SpawnBlood(col.GetContact(0).point);
		if(!isInWater) body.AddForce(-col.GetContact(0).normal * attacker.body.mass / 4, ForceMode.Acceleration);
		lastHit = 50; 
		
		if(isDead) return;
		
		// Sonidos
		if(source != null && source.Length > 0)
		{
			source[0].pitch = Random.Range(1.0f, 1.5f); 
			source[0].PlayOneShot(pain, 1.0f);
		}
		
		// Aplicar daÃƒÂ±o segÃƒÂºn la parte golpeada
		if(hitCollider.StartsWith("jaw"))
		{
			if(source != null && source.Length > 1) source[1].PlayOneShot(Hit_jaw, Random.Range(0.1f, 0.4f));
			health = Mathf.Clamp(health - baseDamages, 0.0f, 100f);
		}
		else if(hitCollider.Equals("head"))
		{
			if(source != null && source.Length > 1) source[1].PlayOneShot(Hit_head, Random.Range(0.1f, 0.4f));
			if(!herbivorous) health = Mathf.Clamp(health - baseDamages, 0.0f, 100f);
			else health = Mathf.Clamp(health - baseDamages / 10, 0.0f, 100f);
		}
		else if(!hitCollider.Equals("root"))
		{
			if(source != null && source.Length > 1) source[1].PlayOneShot(Hit_tail, Random.Range(0.1f, 0.4f));
			if(!herbivorous) health = Mathf.Clamp(health - baseDamages, 0.0f, 100f);
			else health = Mathf.Clamp(health - baseDamages / 10, 0.0f, 100f);
		}
	}

	[PunRPC]
	void SetBehaviorFromAttack(int attackerViewID, string attackerSpecie)
	{
		PhotonView attackerPV = PhotonView.Find(attackerViewID);
		if(attackerPV != null)
		{
			objTGT = attackerPV.gameObject;
			behaviorCount = 500;
			
			if(attackerSpecie == specie) { behavior = "Contest"; }
			else if(canAttack) { behavior = "Battle"; }
			else { behavior = "ToFlee"; }
		}
	}
	#endregion
	
	#region ENVIRONMENTAL CHECKING - STANDALONE VERSION
	public void GetGroundPos(IkType ikType,Transform RLeg1=null,Transform RLeg2=null,Transform RLeg3=null,Transform LLeg1=null,Transform LLeg2=null,Transform LLeg3=null,
										 Transform RArm1=null,Transform RArm2=null,Transform RArm3=null,Transform LArm1=null,Transform LArm2=null,Transform LArm3=null,float FeetOffset=0.0f)
	{
		posY=-transform.position.y;
		#region Use Raycast
		if(useRaycast)
		{
			if(ikType==IkType.None|isDead|isInWater|!isOnGround)
			{
				if(Physics.Raycast(transform.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit hit,withersSize*1.5f,1<<0))
				{ posY=hit.point.y; normal=hit.normal; isOnGround=true; }
				else isOnGround=false;
			}
			else if(ikType>=IkType.SmBiped) // Biped
			{
				if(Physics.Raycast((transform.position+transform.forward*2)+Vector3.up,-Vector3.up,out RaycastHit hit,withersSize*2.0f,1<<0))
				{ posY=hit.point.y; normal=hit.normal; }
				if(Physics.Raycast(RLeg3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit BR,withersSize*2.0f,1<<0))
				{ isOnGround=true; BR_HIT=BR.point; BR_Norm=BR.normal; }
				else BR_HIT.y=-transform.position.y;
				if(Physics.Raycast(LLeg3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit BL,withersSize*2.0f,1<<0))
				{ isOnGround=true; BL_HIT=BL.point; BL_Norm=BL.normal; }
				else BL_HIT.y=-transform.position.y;

				if(posY>BL_HIT.y&&posY>BR_HIT.y) posY=Mathf.Max(BL_HIT.y,BR_HIT.y); else posY=Mathf.Min(BL_HIT.y,BR_HIT.y);
				normal=(BL_Norm+BR_Norm+normal)/3;
			}
			else if(ikType==IkType.Flying) // Flying
			{
				isOnGround=false;
				if(Physics.Raycast(transform.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit hit,withersSize*4.0f,1<<0))
				{
					normal=hit.normal; isOnGround=true;
					if(Physics.Raycast(RArm3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit FR,withersSize*4.0f,1<<0))
					{ FR_HIT=FR.point; FR_Norm=FR.normal; }
					else { FR_Norm=hit.normal; FR_HIT.y=-transform.position.y; }
					if(Physics.Raycast(LArm3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit FL,withersSize*4.0f,1<<0))
					{ FL_HIT=FL.point; FL_Norm=FL.normal; }
					else { FL_Norm=hit.normal; FL_HIT.y=-transform.position.y; }
					if(Physics.Raycast(RLeg3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit BR,withersSize*4.0f,1<<0))
					{ BR_HIT=BR.point; BR_Norm=BR.normal; }
					else { BR_Norm=hit.normal; BR_HIT.y=-transform.position.y; }
					if(Physics.Raycast(LLeg3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit BL,withersSize*4.0f,1<<0))
					{ BL_HIT=BL.point; BL_Norm=BL.normal; }
					else { BL_Norm=hit.normal; BL_HIT.y=-transform.position.y; }
					posY=hit.point.y;
				}

			}
			else //Quadruped
			{
				isOnGround=false;
				if(Physics.Raycast(RArm3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit FR,withersSize*2.0f,1<<0))
				{ FR_HIT=FR.point; FR_Norm=FR.normal; isOnGround=true; }
				else FR_HIT.y=-transform.position.y;
				if(Physics.Raycast(LArm3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit FL,withersSize*2.0f,1<<0))
				{ FL_HIT=FL.point; FL_Norm=FL.normal; isOnGround=true; }
				else FL_HIT.y=-transform.position.y;
				if(Physics.Raycast(RLeg3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit BR,withersSize*2.0f,1<<0))
				{ BR_HIT=BR.point; BR_Norm=BR.normal; isOnGround=true; }
				else BR_HIT.y=-transform.position.y;
				if(Physics.Raycast(LLeg3.position+Vector3.up*withersSize,-Vector3.up,out RaycastHit BL,withersSize*2.0f,1<<0))
				{ BL_HIT=BL.point; BL_Norm=BL.normal; isOnGround=true; }
				else BL_HIT.y=-transform.position.y;

				if(ikType==IkType.Convex)
				{
					if(isConstrained) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
					else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y)/4;
				}
				else
				{
					if(isConstrained|!useIK) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
					else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y-size)/4;
				}

				normal=Vector3.Cross(FR_HIT-BL_HIT,BR_HIT-FL_HIT).normalized;
			}
		}
		#endregion
		#region Terrain Only
		else
		{
			if(ikType==IkType.None|isDead|isInWater|!isOnGround)
			{
				if(t != null)
				{
					float x=((transform.position.x-t.transform.position.x)/t.terrainData.size.x)*tres;
					float y=((transform.position.z-t.transform.position.z)/t.terrainData.size.z)*tres;
					normal=t.terrainData.GetInterpolatedNormal(x/tres,y/tres);
					posY=t.SampleHeight(transform.position)+t.GetPosition().y;
				}
			}
			else if(ikType>=IkType.SmBiped) // Biped
			{
				if(t != null)
				{
					BR_HIT=new Vector3(RLeg3.position.x,t.SampleHeight(RLeg3.position)+tpos.y,RLeg3.position.z);
					float x=((RLeg3.position.x-tpos.x)/tdata.size.x)*tres, y=((RLeg3.position.z-tpos.z)/tdata.size.z)*tres;
					BR_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					BL_HIT=new Vector3(LLeg3.position.x,t.SampleHeight(LLeg3.position)+tpos.y,LLeg3.position.z);
					x=((LLeg3.position.x-tpos.x)/tdata.size.x)*tres; y=((LLeg3.position.z-tpos.z)/tdata.size.z)*tres;
					BL_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);

					if(posY>BL_HIT.y&&posY>BR_HIT.y) posY=Mathf.Max(BL_HIT.y,BR_HIT.y); else posY=Mathf.Min(BL_HIT.y,BR_HIT.y);
					normal=(BL_Norm+BR_Norm+normal)/3;
				}
			}
			else if(ikType==IkType.Flying) // Flying
			{
				if(t != null)
				{
					float x=((transform.position.x-t.transform.position.x)/t.terrainData.size.x)*tres;
					float y=((transform.position.z-t.transform.position.z)/t.terrainData.size.z)*tres;
					normal=t.terrainData.GetInterpolatedNormal(x/tres,y/tres);
					posY=t.SampleHeight(transform.position)+t.GetPosition().y;

					BR_HIT=new Vector3(RLeg3.position.x,t.SampleHeight(RLeg3.position)+tpos.y,RLeg3.position.z);
					x=((RLeg3.position.x-tpos.x)/tdata.size.x)*tres; y=((RLeg3.position.z-tpos.z)/tdata.size.z)*tres;
					BR_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					BL_HIT=new Vector3(LLeg3.position.x,t.SampleHeight(LLeg3.position)+tpos.y,LLeg3.position.z);
					x=((LLeg3.position.x-tpos.x)/tdata.size.x)*tres; y=((LLeg3.position.z-tpos.z)/tdata.size.z)*tres;
					BL_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					FR_HIT=new Vector3(RArm3.position.x,t.SampleHeight(RArm3.position)+tpos.y,RArm3.position.z);
					x=((RArm3.position.x-tpos.x)/tdata.size.x)*tres; y=((RArm3.position.z-tpos.z)/tdata.size.z)*tres;
					FR_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					FL_HIT=new Vector3(LArm3.position.x,t.SampleHeight(LArm3.position)+tpos.y,LArm3.position.z);
					x=((LArm3.position.x-tpos.x)/tdata.size.x)*tres; y=((LArm3.position.z-tpos.z)/tdata.size.z)*tres;
					FL_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
				}
			}
			else //Quadruped
			{
				if(t != null)
				{
					BR_HIT=new Vector3(RLeg3.position.x,t.SampleHeight(RLeg3.position)+tpos.y,RLeg3.position.z);
					float x=((RLeg3.position.x-tpos.x)/tdata.size.x)*tres, y=((RLeg3.position.z-tpos.z)/tdata.size.z)*tres;
					BR_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					BL_HIT=new Vector3(LLeg3.position.x,t.SampleHeight(LLeg3.position)+tpos.y,LLeg3.position.z);
					x=((LLeg3.position.x-tpos.x)/tdata.size.x)*tres; y=((LLeg3.position.z-tpos.z)/tdata.size.z)*tres;
					BL_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					FR_HIT=new Vector3(RArm3.position.x,t.SampleHeight(RArm3.position)+tpos.y,RArm3.position.z);
					x=((RArm3.position.x-tpos.x)/tdata.size.x)*tres; y=((RArm3.position.z-tpos.z)/tdata.size.z)*tres;
					FR_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);
					FL_HIT=new Vector3(LArm3.position.x,t.SampleHeight(LArm3.position)+tpos.y,LArm3.position.z);
					x=((LArm3.position.x-tpos.x)/tdata.size.x)*tres; y=((LArm3.position.z-tpos.z)/tdata.size.z)*tres;
					FL_Norm=tdata.GetInterpolatedNormal(x/tres,y/tres);

					if(ikType==IkType.Convex)
					{
						if(isConstrained) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
						else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y)/4;
					}
					else
					{
						if(isConstrained|!useIK) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
						else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y-size)/4;
					}
					normal=Vector3.Cross(FR_HIT-BL_HIT,BR_HIT-FL_HIT).normalized;
				}
			}
		}
		#endregion
		#region Set status
		//Set status
		if((transform.position.y-size)<=posY) isOnGround=true; else isOnGround=false; //On ground?
		waterY=waterAlt-crouch; //Check for water altitude
		if((transform.position.y)<waterY&&body.worldCenterOfMass.y>waterY) isOnWater=true; else isOnWater=false; //On water ?
		if(body.worldCenterOfMass.y<waterY) isInWater=true; else isInWater=false; // In water ?

		//Setup Rigidbody
		if(isDead)
		{
			body.maxDepenetrationVelocity=0.25f;
			body.constraints=RigidbodyConstraints.None;
		}
		else if(isConstrained)
		{
			body.maxDepenetrationVelocity=0.0f; crouch=0.0f;
			body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ;
		}
		else
		{
			body.maxDepenetrationVelocity=5.0f;
			if(lastHit==0) body.constraints=RigidbodyConstraints.FreezeRotationZ;
			else body.constraints=RigidbodyConstraints.None;
		}

		//Setup Y position and rotation
		if(isOnGround&&!isInWater) //On Ground outside water
		{
			Quaternion n=Quaternion.LookRotation(Vector3.Cross(transform.right,normal),normal);
			if(!canFly)
			{
				float rx=Mathf.DeltaAngle(n.eulerAngles.x,0.0f), rz=Mathf.DeltaAngle(n.eulerAngles.z,0.0f);
				float pitch=Mathf.Clamp(rx,-45f,45f), roll=Mathf.Clamp(rz,-10f,10f);
				normAng=Quaternion.Euler(-pitch,anm.GetFloat("Turn"),-roll);
			}
			else normAng=Quaternion.Euler(n.eulerAngles.x,anm.GetFloat("Turn"),n.eulerAngles.z); posY-=crouch;
		}
		else if(isInWater|isOnWater) //On Water or In water
		{ normAng=Quaternion.Euler(0,anm.GetFloat("Turn"),0); posY=waterY-body.centerOfMass.y; }
		else //In Air
		{ normAng=Quaternion.Euler(0,anm.GetFloat("Turn"),0); posY=-transform.position.y; }

		if(!isVisible|!useIK) return;
		switch(ikType)
		{
			case IkType.None: break;
			case IkType.Convex: Convex(RLeg1,RLeg2,RLeg3,LLeg1,LLeg2,LLeg3,RArm1,RArm2,RArm3,LArm1,LArm2,LArm3); break;
			case IkType.Quad: Quad(RLeg1,RLeg2,RLeg3,LLeg1,LLeg2,LLeg3,RArm1,RArm2,RArm3,LArm1,LArm2,LArm3,FeetOffset); break;
			case IkType.Flying: Flying(RLeg1,RLeg2,RLeg3,LLeg1,LLeg2,LLeg3,RArm1,RArm2,RArm3,LArm1,LArm2,LArm3); break;
			case IkType.SmBiped: SmBiped(RLeg1,RLeg2,RLeg3,LLeg1,LLeg2,LLeg3); break;
			case IkType.LgBiped: LgBiped(RLeg1,RLeg2,RLeg3,LLeg1,LLeg2,LLeg3); break;
		}
		#endregion
	}
	#endregion
	
	#region PHYSICAL FORCES
	public void ApplyGravity(float multiplier=1.0f)
	{
		if(!IsOwner()) return;
		body.AddForce((Vector3.up*size)*(body.velocity.y>0 ? -20*body.drag : -50*body.drag)*multiplier,ForceMode.Acceleration);
	}
	public void ApplyYPos()
	{
		if(!IsOwner()) return;
		if(isOnGround&&(Mathf.Abs(normal.x)>MaxSlope|Mathf.Abs(normal.z)>MaxSlope))
		{ body.AddForce(new Vector3(normal.x,-normal.y,normal.z)*64,ForceMode.Acceleration); behaviorCount=0; }
		body.AddForce(Vector3.up*Mathf.Clamp(posY-transform.position.y,-size,size),ForceMode.VelocityChange);
	}
public void Move(Vector3 dir,float force=0,bool jump=false)
{
    if(!IsOwner()) return;
    
    // CORRECCIÓN: No aplicar rotación durante dash para no interferir
    if(!isDashing)
    {
        if(canAttack&&anm.GetBool("Attack").Equals(true))
        {
            force*=1.5f; transform.rotation=Quaternion.Lerp(transform.rotation,normAng,ang_T*2);
        }
        else transform.rotation=Quaternion.Lerp(transform.rotation,normAng,ang_T);
    }

    // CORRECCIÓN: No aplicar fuerzas de movimiento durante dash
    if(dir!=Vector3.zero && !isDashing)
    {
        if(!canSwim&&!isOnGround)
        {
            if(isInWater|isOnWater) force/=8;
            else if(!canFly&&!onJump) force/=8;
            else force/=(4/body.drag);
        }
        else force/=(4/body.drag);

        body.AddForce(dir*force*speed,jump ? ForceMode.VelocityChange : ForceMode.Acceleration);
    }
    
    // Manejo de drag dinámico
    if(isDashing)
    {
        // Mantener drag bajo durante dash
        body.drag = 0.1f;
    }
    else
    {
        // Restaurar drag normal gradualmente
        body.drag = Mathf.Lerp(body.drag, 2.0f, Time.deltaTime * 3f); // Ajusta el valor base según tu configuración
    }
}
	#endregion
	
	#region LERP SKELETON ROTATION
	public void RotateBone(IkType ikType,float maxX,float maxY=0,bool CanMoveHead=true,float t=0.5f)
	{
		//Freeze all
		if(animSpeed==0.0f) return;

		//Slowdown on turning
		if(!onAttack&&!onJump)
		{ speed=size*anm.speed*(1.0f-Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y,anm.GetFloat("Turn")))/135f); }

		//Lerp feet position
		if(useIK&&ikType!=IkType.None)
		{
			float s;
			switch(ikType)
			{
				case IkType.Convex:
				s=0.1f;
				if(!isConstrained&&!isDead&&isOnGround&&!isInWater)
				{
					FR1=Mathf.Lerp(FR1,Mathf.Clamp(-alt1,-55,0),s); FR2=Mathf.Lerp(FR2,b1,s); FR3=Mathf.Lerp(FR3,c1,s);
					FL1=Mathf.Lerp(FL1,Mathf.Clamp(-alt2,-55,0),s); FL2=Mathf.Lerp(FL2,b2,s); FL3=Mathf.Lerp(FL3,c2,s);
					BR1=Mathf.Lerp(BR1,Mathf.Clamp(-alt3,-55,0),s); BR2=Mathf.Lerp(BR2,b3,s); BR3=Mathf.Lerp(BR3,c3,s);
					BL1=Mathf.Lerp(BL1,Mathf.Clamp(-alt4,-55,0),s); BL2=Mathf.Lerp(BL2,b4,s); BL3=Mathf.Lerp(BL3,c4,s);
				}
				else
				{
					FR_Add=Mathf.Lerp(FR_Add,0,s); FR1=Mathf.Lerp(FR1,0,s); FR2=Mathf.Lerp(FR2,0,s); FR3=Mathf.Lerp(FR3,0,s);
					FL_Add=Mathf.Lerp(FL_Add,0,s); FL1=Mathf.Lerp(FL1,0,s); FL2=Mathf.Lerp(FL2,0,s); FL3=Mathf.Lerp(FL3,0,s);
					BR_Add=Mathf.Lerp(BR_Add,0,s); BR1=Mathf.Lerp(BR1,0,s); BR2=Mathf.Lerp(BR2,0,s); BR3=Mathf.Lerp(BR3,0,s);
					BL_Add=Mathf.Lerp(BL_Add,0,s); BL1=Mathf.Lerp(BL1,0,s); BL2=Mathf.Lerp(BL2,0,s); BL3=Mathf.Lerp(BL3,0,s);
				}
				break;
				case IkType.Quad:
				s=0.1f;
				if(!isConstrained&&!isDead&&isOnGround)
				{

					FR1=Mathf.Lerp(FR1,Mathf.Clamp(-alt1,-50,0),s); FR2=Mathf.Lerp(FR2,b1,s); FR3=Mathf.Lerp(FR3,c1,s);
					FL1=Mathf.Lerp(FL1,Mathf.Clamp(-alt2,-50,0),s); FL2=Mathf.Lerp(FL2,b2,s); FL3=Mathf.Lerp(FL3,c2,s);
					BR1=Mathf.Lerp(BR1,Mathf.Clamp(-alt3,-50,0),s); BR2=Mathf.Lerp(BR2,b3,s); BR3=Mathf.Lerp(BR3,c3,s);
					BL1=Mathf.Lerp(BL1,Mathf.Clamp(-alt4,-50,0),s); BL2=Mathf.Lerp(BL2,b4,s); BL3=Mathf.Lerp(BL3,c4,s);
				}
				else
				{
					FR_Add=Mathf.Lerp(FR_Add,0,s); FR1=Mathf.Lerp(FR1,0,s); FR2=Mathf.Lerp(FR2,0,s); FR3=Mathf.Lerp(FR3,0,s);
					FL_Add=Mathf.Lerp(FL_Add,0,s); FL1=Mathf.Lerp(FL1,0,s); FL2=Mathf.Lerp(FL2,0,s); FL3=Mathf.Lerp(FL3,0,s);
					BR_Add=Mathf.Lerp(BR_Add,0,s); BR1=Mathf.Lerp(BR1,0,s); BR2=Mathf.Lerp(BR2,0,s); BR3=Mathf.Lerp(BR3,0,s);
					BL_Add=Mathf.Lerp(BL_Add,0,s); BL1=Mathf.Lerp(BL1,0,s); BL2=Mathf.Lerp(BL2,0,s); BL3=Mathf.Lerp(BL3,0,s);
				}
				break;
				case IkType.Flying:
				s=0.25f;
				if(!isConstrained&&!isDead&&isOnGround&&!isOnLevitation)
				{
					FR1=Mathf.Lerp(FR1,Mathf.Clamp(-alt1,-100,0),s); FR2=Mathf.Lerp(FR2,b1,s); FR3=Mathf.Lerp(FR3,c1,s);
					FL1=Mathf.Lerp(FL1,Mathf.Clamp(-alt2,-100,0),s); FL2=Mathf.Lerp(FL2,b2,s); FL3=Mathf.Lerp(FL3,c2,s);
					BR1=Mathf.Lerp(BR1,Mathf.Clamp(-alt3,-60,0),s); BR2=Mathf.Lerp(BR2,b3,s); BR3=Mathf.Lerp(BR3,c3,s);
					BL1=Mathf.Lerp(BL1,Mathf.Clamp(-alt4,-60,0),s); BL2=Mathf.Lerp(BL2,b4,s); BL3=Mathf.Lerp(BL3,c4,s);
				}
				else
				{
					FR_Add=Mathf.Lerp(FR_Add,0,s); FR1=Mathf.Lerp(FR1,0,s); FR2=Mathf.Lerp(FR2,0,s); FR3=Mathf.Lerp(FR3,0,s);
					FL_Add=Mathf.Lerp(FL_Add,0,s); FL1=Mathf.Lerp(FL1,0,s); FL2=Mathf.Lerp(FL2,0,s); FL3=Mathf.Lerp(FL3,0,s);
					BR_Add=Mathf.Lerp(BR_Add,0,s); BR1=Mathf.Lerp(BR1,0,s); BR2=Mathf.Lerp(BR2,0,s); BR3=Mathf.Lerp(BR3,0,s);
					BL_Add=Mathf.Lerp(BL_Add,0,s); BL1=Mathf.Lerp(BL1,0,s); BL2=Mathf.Lerp(BL2,0,s); BL3=Mathf.Lerp(BL3,0,s);
				}
				break;
				case IkType.SmBiped:
				s=0.25f;
				if(!isConstrained&&!isDead&&isOnGround)
				{
					BR1=Mathf.Lerp(BR1,Mathf.Clamp(-alt1,-60,0),s); BR2=Mathf.Lerp(BR2,b1,s); BR3=Mathf.Lerp(BR3,c1,s);
					BL1=Mathf.Lerp(BL1,Mathf.Clamp(-alt2,-60,0),s); BL2=Mathf.Lerp(BL2,b2,s); BL3=Mathf.Lerp(BL3,c2,s);
				}
				else
				{
					BR_Add=Mathf.Lerp(BR_Add,0,s); BR1=Mathf.Lerp(BR1,0,s); BR2=Mathf.Lerp(BR2,0,s); BR3=Mathf.Lerp(BR3,0,s);
					BL_Add=Mathf.Lerp(BL_Add,0,s); BL1=Mathf.Lerp(BL1,0,s); BL2=Mathf.Lerp(BL2,0,s); BL3=Mathf.Lerp(BL3,0,s);
				}
				break;
				case IkType.LgBiped:
				s=0.25f;
				if(!isDead&&isOnGround)
				{
					BR1=Mathf.Lerp(BR1,Mathf.Clamp(-alt1,-55,0),s); BR2=Mathf.Lerp(BR2,b1,s); BR3=Mathf.Lerp(BR3,c1,s);
					BL1=Mathf.Lerp(BL1,Mathf.Clamp(-alt2,-55,0),s); BL2=Mathf.Lerp(BL2,b2,s); BL3=Mathf.Lerp(BL3,c2,s);
				}
				else
				{
					BR_Add=Mathf.Lerp(BR_Add,0,s); BR1=Mathf.Lerp(BR1,0,s); BR2=Mathf.Lerp(BR2,0,s); BR3=Mathf.Lerp(BR3,0,s);
					BL_Add=Mathf.Lerp(BL_Add,0,s); BL1=Mathf.Lerp(BL1,0,s); BL2=Mathf.Lerp(BL2,0,s); BL3=Mathf.Lerp(BL3,0,s);
				}
				break;
			}
		}

		//Take damages animation
		if(lastHit!=0) { if(!isDead&&canWalk) crouch=Mathf.Lerp(crouch,(crouch_Max*size)/2,1.0f); lastHit--; }

		//Reset skeleton rotations
		if(onReset)
		{
			pitch=Mathf.Lerp(pitch,0.0f,t/10f);
			roll=Mathf.Lerp(roll,0.0f,t/10f);
			headX=Mathf.LerpAngle(headX,0.0f,t/10f);
			headY=Mathf.LerpAngle(headY,0.0f,t/10f);
			crouch=Mathf.Lerp(crouch,0.0f,t/10f);
			spineX=Mathf.LerpAngle(spineX,0.0f,t/10f);
			spineY=Mathf.LerpAngle(spineY,0.0f,t/10f);
			return;
		}

		//Smooth avoiding angle
		if(avoidDelta!=0)
		{
			if(Mathf.Abs(avoidAdd)>90) avoidDelta=0;
			avoidAdd=Mathf.MoveTowardsAngle(avoidAdd,avoidDelta>0.0f ? 135f : -135f,t);
		}
		else avoidAdd=Mathf.MoveTowardsAngle(avoidAdd,0.0f,t);

		//Setup Look target position
		if(objTGT)
		{
			if(behavior.EndsWith("Hunt")|behavior.Equals("Battle")|behavior.EndsWith("Contest")) lookTGT=objTGT.transform.position;
			else if(herbivorous&&behavior.Equals("Food")) lookTGT=posTGT;
			else if(loop==0) lookTGT=Vector3.zero;
		}
		else if(loop==0) lookTGT=Vector3.zero;

		//Lerp all skeleton parts
		if(CanMoveHead)
		{
			if(!onTailAttack&&!anm.GetInteger("Move").Equals(0))
			{
				spineX=Mathf.MoveTowardsAngle(spineX,(Mathf.DeltaAngle(anm.GetFloat("Turn"),transform.eulerAngles.y)/360f)*maxX,t);
				spineY=Mathf.LerpAngle(spineY,0.0f,t/10f);
			}
			else
			{
				spineX=Mathf.MoveTowardsAngle(spineX,0.0f,t/10f);
				spineY=Mathf.LerpAngle(spineY,0.0f,t/10f);
			}

			if((!canFly&&!canSwim&&anm.GetInteger("Move")!=2)|!isOnGround) roll=Mathf.Lerp(roll,0.0f,ang_T);
			crouch=Mathf.Lerp(crouch,0.0f,t/10f);

			if(onHeadMove) return;

			if(lookTGT!=Vector3.zero&&(lookTGT-transform.position).magnitude>boxscale.z)
			{
				Quaternion dir;
				if(objTGT&&objTGT.tag.Equals("Creature")) dir=Quaternion.LookRotation(objTGT.GetComponent<Rigidbody>().worldCenterOfMass-headPos);
				else dir=Quaternion.LookRotation(lookTGT-headPos);

				headX=Mathf.MoveTowardsAngle(headX,(Mathf.DeltaAngle(dir.eulerAngles.y,transform.eulerAngles.y)/(180f-yaw_Max))*yaw_Max,t);
				headY=Mathf.MoveTowardsAngle(headY,(Mathf.DeltaAngle(dir.eulerAngles.x,transform.eulerAngles.x)/(90f-pitch_Max))*pitch_Max,t);
			}
			else
			{
				if(Mathf.RoundToInt(anm.GetFloat("Turn"))==Mathf.RoundToInt(transform.eulerAngles.y))
				{
					if(loop==0&&Mathf.RoundToInt(headX*100)==Mathf.RoundToInt(rndX*100)&&Mathf.RoundToInt(headY*100)==Mathf.RoundToInt(rndY*100))
					{
						rndX=Random.Range((int)-yaw_Max/2,(int)yaw_Max/2);
						rndY=Random.Range((int)-pitch_Max/2,(int)pitch_Max/2);
					}
					headX=Mathf.LerpAngle(headX,rndX,t/10f);
					headY=Mathf.LerpAngle(headY,rndY,t/10f);
				}
				else
				{
					headX=Mathf.LerpAngle(headX,spineX,t/10f);
					headY=Mathf.LerpAngle(headY,0.0f,t/10f);
				}
			}
		}
		else
		{
			spineX=Mathf.LerpAngle(spineX,(Mathf.DeltaAngle(anm.GetFloat("Turn"),transform.eulerAngles.y)/360f)*maxX,ang_T);
			if(isOnGround&&!isInWater) { spineY=Mathf.LerpAngle(spineY,0.0f,t/10f); roll=Mathf.LerpAngle(roll,0.0f,t/10f); pitch=Mathf.Lerp(pitch,0.0f,t/10f); }
			else if(canFly)
			{
				if(anm.GetInteger("Move")>=2&&anm.GetInteger("Move")<3)
					spineY=Mathf.LerpAngle(spineY,(Mathf.DeltaAngle(anm.GetFloat("Pitch")*90f,pitch)/180f)*maxY,ang_T);
				roll=Mathf.LerpAngle(roll,-spineX,t/10f);
			}
			else { spineY=Mathf.LerpAngle(spineY,(Mathf.DeltaAngle(anm.GetFloat("Pitch")*90f,pitch)/180f)*maxY,ang_T); roll=Mathf.LerpAngle(roll,-spineX,t/10f); }
			headX=Mathf.LerpAngle(headX,spineX,t);
			headY=Mathf.LerpAngle(headY,spineY,t);
		}
	}
	#endregion
	
	#region FEET INVERSE KINEMATICS
	//QUADRUPED
	void Quad(Transform RLeg1,Transform RLeg2,Transform RLeg3,Transform LLeg1,Transform LLeg2,Transform LLeg3,
						Transform RArm1,Transform RArm2,Transform RArm3,Transform LArm1,Transform LArm2,Transform LArm3,float FeetOffset)
	{
		//Right arm
		float offset=(RArm3.position-RArm3.GetChild(0).GetChild(0).position).magnitude+FeetOffset;
		Vector3 va1=RArm3.position-transform.up*offset;

		RArm1.rotation*=Quaternion.Euler(0,-FR1+(FR1+FR_Add),0);
		a1=Vector3.Angle(RArm1.position-RArm2.position,RArm1.position-RArm3.position);
		RArm2.rotation*=Quaternion.Euler(0,(FR1*2f)-FR_Add,0);
		b1=Vector3.Angle(FR_Norm,RArm3.right)-100f;
		c1=Vector3.Angle(-FR_Norm,RArm3.up)-90;
		RArm3.rotation*=Quaternion.Euler(FR3,FR2,0);

		Vector3 va3=FR_HIT+(FR_HIT-RArm3.position)+transform.up*offset;
		Vector3 va2=new Vector3(va1.x,va1.y-(va1.y-RArm1.position.y)-(va1.y-FR_HIT.y),va1.z);
		alt1=((va1-va2).magnitude-(va3-va2).magnitude)*(100/(va1-va2).magnitude);
		//Left arm
		offset=(LArm3.position-LArm3.GetChild(0).GetChild(0).position).magnitude+FeetOffset;
		Vector3 vb1=LArm3.position-transform.up*offset;

		LArm1.rotation*=Quaternion.Euler(-FL1+(FL1+FL_Add),0,0);
		a2=Vector3.Angle(LArm1.position-LArm2.position,LArm1.position-LArm3.position);
		LArm2.rotation*=Quaternion.Euler((FL1*2f)-FL_Add,0,0);
		b2=Vector3.Angle(FL_Norm,LArm3.right)-90f;
		c2=Vector3.Angle(-FL_Norm,LArm3.up)-100f;
		LArm3.rotation*=Quaternion.Euler(FL3,FL2,0);

		Vector3 vb3=FL_HIT+(FL_HIT-LArm3.position)+transform.up*offset;
		Vector3 vb2=new Vector3(vb1.x,vb1.y-(vb1.y-LArm1.position.y)-(vb1.y-FL_HIT.y),vb1.z);
		alt2=((vb1-vb2).magnitude-(vb3-vb2).magnitude)*(100/(vb1-vb2).magnitude);
		//Right leg
		offset=(RLeg3.position-RLeg3.GetChild(0).GetChild(0).position).magnitude+FeetOffset;
		Vector3 vc1=RLeg3.position-transform.up*offset;

		RLeg1.rotation*=Quaternion.Euler(0,BR1-(BR1+BR_Add),0);
		a3=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position);
		RLeg2.rotation*=Quaternion.Euler(0,(-BR1*2f)+BR_Add,0);
		b3=Vector3.Angle(BR_Norm,RLeg3.right)-90f;
		c3=Vector3.Angle(-BR_Norm,RLeg3.up)-90f;
		RLeg3.rotation*=Quaternion.Euler(BR3,BR2,0);

		Vector3 vc3=BR_HIT+(BR_HIT-RLeg3.position)+transform.up*offset;
		Vector3 vc2=new Vector3(vc1.x,vc1.y-(vc1.y-RLeg1.position.y)-(vc1.y-BR_HIT.y),vc1.z);
		alt3=((vc1-vc2).magnitude-(vc3-vc2).magnitude)*(100/(vc1-vc2).magnitude);
		//Left leg
		offset=(LLeg3.position-LLeg3.GetChild(0).GetChild(0).position).magnitude+FeetOffset;
		Vector3 vd1=LLeg3.position-transform.up*offset;

		LLeg1.rotation*=Quaternion.Euler(0,BL1-(BL1+BL_Add),0);
		a4=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position);
		LLeg2.rotation*=Quaternion.Euler(0,(-BL1*2f)+BL_Add,0);
		b4=Vector3.Angle(BL_Norm,LLeg3.right)-90f;
		c4=Vector3.Angle(-BL_Norm,LLeg3.up)-90f;
		LLeg3.rotation*=Quaternion.Euler(BL3,BL2,0);

		Vector3 vd3=BL_HIT+(BL_HIT-LLeg3.position)+transform.up*offset;
		Vector3 vd2=new Vector3(vd1.x,vd1.y-(vd1.y-LLeg1.position.y)-(vd1.y-BL_HIT.y),vd1.z);
		alt4=((vd1-vd2).magnitude-(vd3-vd2).magnitude)*(100/(vd1-vd2).magnitude);

		//Add rotations
		if(!isConstrained&&!isDead&&isOnGround)
		{
			FR_Add=Vector3.Angle(RArm1.position-RArm2.position,RArm1.position-RArm3.position)-a1;
			FL_Add=Vector3.Angle(LArm1.position-LArm2.position,LArm1.position-LArm3.position)-a2;
			BR_Add=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position)-a3;
			BL_Add=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position)-a4;
		}
	}

	//SMALL BIPED
	void SmBiped(Transform RLeg1,Transform RLeg2,Transform RLeg3,Transform LLeg1,Transform LLeg2,Transform LLeg3)
	{
		Transform RLeg4=RLeg3.GetChild(0);
		//Right leg
		float offset1=(RLeg4.position-RLeg4.GetChild(0).position).magnitude;
		Vector3 va1=RLeg4.position-transform.up*offset1;
		float inv1=Mathf.Clamp(Vector3.Cross(RLeg4.position-transform.position,RLeg1.position-transform.position).y,-1.0f,1.0f);

		RLeg1.rotation*=Quaternion.Euler(0,BR1-(BR1+BR_Add),0);
		a1=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position);
		RLeg2.rotation*=Quaternion.Euler(0,-BR1*2f,0);
		RLeg3.rotation*=Quaternion.Euler(0,BR1-BR_Add*inv1,0);
		b1=Vector3.Angle(-BR_Norm,RLeg4.GetChild(0).right)-90f;
		c1=Vector3.Angle(-BR_Norm,RLeg4.up)-90f;
		RLeg4.rotation*=Quaternion.Euler(BR3,0,0);
		RLeg4.GetChild(0).rotation*=Quaternion.Euler(0,-BR2,0);

		Vector3 va3=BR_HIT+(BR_HIT-RLeg4.GetChild(0).position)+transform.up*offset1;
		Vector3 va2=(va1+transform.up*(va1-RLeg1.position).magnitude);
		alt1=((va1-va2).magnitude-(va3-va2).magnitude)*(100/(va1-va2).magnitude);

		Transform LLeg4=LLeg3.GetChild(0);
		//Left Leg
		float offset2=(LLeg4.position-LLeg4.GetChild(0).position).magnitude;
		Vector3 vb1=LLeg4.position-transform.up*offset2;
		float inv2=Mathf.Clamp(Vector3.Cross(LLeg4.position-transform.position,LLeg1.position-transform.position).y,-1.0f,1.0f);

		LLeg1.rotation*=Quaternion.Euler(BL1-(BL1+BL_Add),0,0);
		a2=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position);
		LLeg2.rotation*=Quaternion.Euler(-BL1*2f,0,0);
		LLeg3.rotation*=Quaternion.Euler(BL1+BL_Add*inv2,0,0);

		b2=Vector3.Angle(-BL_Norm,-LLeg4.GetChild(0).up)-90f;
		c2=Vector3.Angle(-BL_Norm,LLeg4.up)-90f;
		LLeg4.rotation*=Quaternion.Euler(BL3,0,0);
		LLeg4.GetChild(0).rotation*=Quaternion.Euler(0,0,BL2);


		Vector3 vb3=BL_HIT+(BL_HIT-LLeg4.GetChild(0).position)+transform.up*offset2;
		Vector3 vb2=(vb1+transform.up*(vb1-LLeg1.position).magnitude);
		alt2=((vb1-vb2).magnitude-(vb3-vb2).magnitude)*(100/(vb1-vb2).magnitude);

		//Add rotations
		if(!isConstrained&&!isDead&&isOnGround)
		{
			BR_Add=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position)-a1;
			BL_Add=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position)-a2;
		}


	}

	//LARGE BIPED
	public void LgBiped(Transform RLeg1,Transform RLeg2,Transform RLeg3,Transform LLeg1,Transform LLeg2,Transform LLeg3)
	{
		//Right leg
		Transform RLeg4=RLeg3.GetChild(0);
		float offset1=(RLeg4.position-RLeg4.GetChild(1).position).magnitude;
		Vector3 va1=RLeg4.position-transform.up*offset1;
		float inv1=Mathf.Clamp(Vector3.Cross(RLeg4.position-transform.position,RLeg1.position-transform.position).y,-1.0f,1.0f);

		RLeg1.rotation*=Quaternion.Euler(0,BR1-(BR1+BR_Add),0);
		a1=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position);
		RLeg2.rotation*=Quaternion.Euler(0,-BR1*2f,0);
		RLeg3.rotation*=Quaternion.Euler(0,BR1-BR_Add*inv1,0);
		b1=Vector3.Angle(-BR_Norm,RLeg4.GetChild(1).right)-90f;
		c1=Vector3.Angle(-BR_Norm,RLeg4.up)-90f;
		RLeg4.rotation*=Quaternion.Euler(BR3,0,0);
		RLeg4.GetChild(0).rotation*=Quaternion.Euler(0,-BR2,0);
		RLeg4.GetChild(1).rotation*=Quaternion.Euler(0,-BR2,0);
		RLeg4.GetChild(2).rotation*=Quaternion.Euler(0,-BR2,0);

		Vector3 va3=BR_HIT+(BR_HIT-RLeg4.position)+transform.up*offset1;
		Vector3 va2=(va1+transform.up*(va1-RLeg1.position).magnitude);
		alt1=((va1-va2).magnitude-(va3-va2).magnitude)*(100/(va1-va2).magnitude);

		//Left Leg
		Transform LLeg4=LLeg3.GetChild(0);
		float offset2=(LLeg4.position-LLeg4.GetChild(1).position).magnitude;
		Vector3 vb1=LLeg4.position-transform.up*offset2;
		float inv2=Mathf.Clamp(Vector3.Cross(LLeg4.position-transform.position,LLeg1.position-transform.position).y,-1.0f,1.0f);

		LLeg1.rotation*=Quaternion.Euler(0,BL1-(BL1+BL_Add),0);
		a2=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position);
		LLeg2.rotation*=Quaternion.Euler(0,-BL1*2f,0);
		LLeg3.rotation*=Quaternion.Euler(0,BL1+BL_Add*inv2,0);

		b2=Vector3.Angle(-BL_Norm,LLeg4.GetChild(1).up)-90f;
		c2=Vector3.Angle(-BL_Norm,LLeg4.up)-90f;
		LLeg4.rotation*=Quaternion.Euler(BL3,0,0);
		LLeg4.GetChild(0).rotation*=Quaternion.Euler(0,BL2,0);
		LLeg4.GetChild(1).rotation*=Quaternion.Euler(BL2,0,0);
		LLeg4.GetChild(2).rotation*=Quaternion.Euler(0,BL2,0);

		Vector3 vb3=BL_HIT+(BL_HIT-LLeg4.position)+transform.up*offset2;
		Vector3 vb2=(vb1+transform.up*(vb1-LLeg1.position).magnitude);
		alt2=((vb1-vb2).magnitude-(vb3-vb2).magnitude)*(100/(vb1-vb2).magnitude);

		//Add rotations
		if(!isDead&&isOnGround)
		{
			BR_Add=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position)-a1;
			BL_Add=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position)-a2;
		}
	}

	//CONVEX QUADRUPED
	void Convex(Transform RLeg1,Transform RLeg2,Transform RLeg3,Transform LLeg1,Transform LLeg2,Transform LLeg3,
										Transform RArm1,Transform RArm2,Transform RArm3,Transform LArm1,Transform LArm2,Transform LArm3)
	{
		//Right arm
		float offset1=(RArm3.position-RArm3.GetChild(0).position).magnitude;
		Vector3 va1=RArm3.position-transform.up*offset1;

		RArm1.rotation*=Quaternion.Euler(FR1-(FR1+FR_Add),0,0);
		a1=Vector3.Angle(RArm1.position-RArm2.position,RArm1.position-RArm3.GetChild(0).GetChild(0).position);
		RArm2.rotation*=Quaternion.Euler(0,FR1-FR_Add,0);
		b1=Vector3.Angle(FR_Norm,RArm3.GetChild(0).right)-90f;
		c1=Vector3.Angle(FR_Norm,-RArm3.GetChild(0).up)-90f;
		RArm3.rotation*=Quaternion.Euler(-FR3/2,-FR2/2,0);
		RArm3.GetChild(0).rotation*=Quaternion.Euler(-FR3/2,-FR2/2,0);

		Vector3 va3=FR_HIT+(FR_HIT-RArm3.GetChild(0).GetChild(0).position)+transform.up*offset1;
		Vector3 va2=new Vector3(va1.x,va1.y-(va1.y-RArm1.position.y)-(va1.y-FR_HIT.y),va1.z);
		alt1=((va1-va2).magnitude-(va3-va2).magnitude)*(100/(va1-va2).magnitude);

		//Left arm
		float offset2=(LArm3.position-LArm3.GetChild(0).position).magnitude;
		Vector3 vb1=LArm3.position-transform.up*offset2;

		LArm1.rotation*=Quaternion.Euler(FL1-(FL1+FL_Add),0,0);
		a2=Vector3.Angle(LArm1.position-LArm2.position,LArm1.position-LArm3.GetChild(0).GetChild(0).position);
		LArm2.rotation*=Quaternion.Euler(-FL1+FL_Add,0,0);
		b2=Vector3.Angle(FL_Norm,-LArm3.GetChild(0).up)-90f;
		c2=Vector3.Angle(FL_Norm,LArm3.GetChild(0).right)-90f;
		LArm3.rotation*=Quaternion.Euler(-FL2/2,-FL3/2,0);
		LArm3.GetChild(0).rotation*=Quaternion.Euler(-FL2/2,-FL3/2,0);

		Vector3 vb3=FL_HIT+(FL_HIT-LArm3.GetChild(0).GetChild(0).position)+transform.up*offset2;
		Vector3 vb2=new Vector3(vb1.x,vb1.y-(vb1.y-LArm1.position.y)-(vb1.y-FL_HIT.y),vb1.z);
		alt2=((vb1-vb2).magnitude-(vb3-vb2).magnitude)*(100/(vb1-vb2).magnitude);

		//Right leg
		float offset3=(RLeg3.position-RLeg3.GetChild(0).GetChild(0).position).magnitude;
		Vector3 vc1=RLeg3.position-transform.up*offset3;

		RLeg1.rotation*=Quaternion.Euler(0,-(BR1+(BR1+BR_Add)),0);
		a3=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position);
		RLeg2.rotation*=Quaternion.Euler(0,(BR1*2f)-BR_Add,0);
		b3=Vector3.Angle(BR_Norm,RLeg3.GetChild(0).right)-90f;
		c3=Vector3.Angle(-BR_Norm,RLeg3.GetChild(0).up)-90f;
		RLeg3.rotation*=Quaternion.Euler(-BR3/2,-BR2/2,0);
		RLeg3.GetChild(0).rotation*=Quaternion.Euler(-BR3/2,-BR2/2,0);

		Vector3 vc3=BR_HIT+(BR_HIT-RLeg3.position)+transform.up*offset3;
		Vector3 vc2=new Vector3(vc1.x,vc1.y-(vc1.y-RLeg1.position.y)-(vc1.y-BR_HIT.y),vc1.z);
		alt3=((vc1-vc2).magnitude-(vc3-vc2).magnitude)*(100/(vc1-vc2).magnitude);

		//Left leg
		float offset4=(LLeg3.position-LLeg3.GetChild(0).GetChild(0).position).magnitude;
		Vector3 vd1=LLeg3.position-transform.up*offset4;

		LLeg1.rotation*=Quaternion.Euler(BL1+(BL1+BL_Add),0,0);
		a4=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position);
		LLeg2.rotation*=Quaternion.Euler(-(BL1*2f)+BL_Add,0,0);
		b4=Vector3.Angle(BL_Norm,LLeg3.GetChild(0).right)-90f;
		c4=Vector3.Angle(-BL_Norm,LLeg3.GetChild(0).up)-90f;
		LLeg3.rotation*=Quaternion.Euler(-BL3/2,-BL2/2,0);
		LLeg3.GetChild(0).rotation*=Quaternion.Euler(-BL3/2,-BL2/2,0);

		Vector3 vd3=BL_HIT+(BL_HIT-LLeg3.position)+transform.up*offset4;
		Vector3 vd2=new Vector3(vd1.x,vd1.y-(vd1.y-LLeg1.position.y)-(vd1.y-BL_HIT.y),vd1.z);
		alt4=((vd1-vd2).magnitude-(vd3-vd2).magnitude)*(100/(vd1-vd2).magnitude);

		if(!isConstrained&&!isDead&&isOnGround&&!isInWater)
		{
			FR_Add=Vector3.Angle(RArm1.position-RArm2.position,RArm1.position-RArm3.GetChild(0).GetChild(0).position)-a1;
			FL_Add=Vector3.Angle(LArm1.position-LArm2.position,LArm1.position-LArm3.GetChild(0).GetChild(0).position)-a2;
			BR_Add=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.position)-a3;
			BL_Add=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.position)-a4;
		}
	}

	//FLYING
	void Flying(Transform RLeg1,Transform RLeg2,Transform RLeg3,Transform LLeg1,Transform LLeg2,Transform LLeg3,
								Transform RArm1,Transform RArm2,Transform RArm3,Transform LArm1,Transform LArm2,Transform LArm3)
	{
		//Right wing
		Vector3 va1=RArm3.GetChild(1).position;

		RArm1.rotation*=Quaternion.Euler(FR1,FR1-(FR1-FR_Add),FR1);
		a1=Vector3.Angle(RArm1.position-RArm2.position,RArm1.position-RArm3.GetChild(1).position);
		RArm2.rotation*=Quaternion.Euler(0,0,(-FR1*2.4f)-FR_Add);
		b1=Vector3.Angle(FR_Norm,RArm3.right)-90f;
		c1=Vector3.Angle(-FR_Norm,RArm3.up)-90f;
		RArm3.rotation*=Quaternion.Euler(FR3,FR2,0);

		Vector3 va3=FR_HIT+(FR_HIT-RArm3.GetChild(1).position);
		Vector3 va2=new Vector3(va1.x,va1.y-(va1.y-RArm1.position.y)-(va1.y-FR_HIT.y),va1.z);
		alt1=((va1-va2).magnitude-(va3-va2).magnitude)*(100/(va1-va2).magnitude);

		//Left Wing
		Vector3 vb1=LArm3.GetChild(1).position;

		LArm1.rotation*=Quaternion.Euler(-FL1,FL1-(FL1-FL_Add),-FL1);
		a2=Vector3.Angle(LArm1.position-LArm2.position,LArm1.position-LArm3.GetChild(1).position);
		LArm2.rotation*=Quaternion.Euler(0,0,(FL1*2.4f)+FL_Add);
		b2=Vector3.Angle(FL_Norm,LArm3.right)-90f;
		c2=Vector3.Angle(-FL_Norm,LArm3.up)-90f;
		LArm3.rotation*=Quaternion.Euler(FL3,FL2,0);

		Vector3 vb3=FL_HIT+(FL_HIT-LArm3.GetChild(1).position);
		Vector3 vb2=new Vector3(vb1.x,vb1.y-(vb1.y-LArm1.position.y)-(vb1.y-FL_HIT.y),vb1.z);
		alt2=((vb1-vb2).magnitude-(vb3-vb2).magnitude)*(100/(vb1-vb2).magnitude);

		//Right leg
		float offset1=(RLeg3.position-RLeg3.GetChild(2).position).magnitude/1.5f;
		Vector3 vc1=RLeg3.position-transform.up*offset1;
		float inv1=Mathf.Clamp(Vector3.Cross(RLeg3.GetChild(2).position-transform.position,RLeg1.position-transform.position).y,-1.0f,1.0f);

		RLeg1.rotation*=Quaternion.Euler(0,-BR1+(BR1-BR_Add),0);
		a3=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.GetChild(2).position);
		RLeg2.rotation*=Quaternion.Euler(0,-BR1*2,0);
		c3=Vector3.Angle(BR_Norm,RLeg3.GetChild(2).up)-90f;
		RLeg3.rotation*=Quaternion.Euler(0,BR1-BR_Add*inv1,BR3);
		b3=Vector3.Angle(BR_Norm,RLeg3.GetChild(2).right)-90f;
		RLeg3.GetChild(0).rotation*=Quaternion.Euler(0,-BR2,0);
		RLeg3.GetChild(1).rotation*=Quaternion.Euler(0,-BR2,0);
		RLeg3.GetChild(2).rotation*=Quaternion.Euler(0,-BR2,0);
		RLeg3.GetChild(3).rotation*=Quaternion.Euler(0,-BR2,0);

		Vector3 vc3=BR_HIT+(BR_HIT-RLeg3.GetChild(2).position)+transform.up*offset1;
		Vector3 vc2=(vc1+transform.up*(vc1-RLeg1.position).magnitude);
		alt3=((vc1-vc2).magnitude-(vc3-vc2).magnitude)*(100/(vc1-vc2).magnitude);

		//Left leg
		float offset2=(LLeg3.position-LLeg3.GetChild(2).position).magnitude/1.5f;
		Vector3 vd1=LLeg3.position-transform.up*offset2;
		float inv2=Mathf.Clamp(Vector3.Cross(LLeg3.GetChild(2).position-transform.position,LLeg1.position-transform.position).y,-1.0f,1.0f);

		LLeg1.rotation*=Quaternion.Euler(0,-BL1+(BL1-BL_Add),0);
		a4=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.GetChild(2).position);
		LLeg2.rotation*=Quaternion.Euler(0,-BL1*2,0);
		c4=Vector3.Angle(BL_Norm,LLeg3.GetChild(2).up)-90f;
		LLeg3.rotation*=Quaternion.Euler(0,BL1-BL_Add*inv2,BL3);
		b4=Vector3.Angle(BL_Norm,LLeg3.GetChild(2).right)-90f;
		LLeg3.GetChild(0).rotation*=Quaternion.Euler(0,-BL2,0);
		LLeg3.GetChild(1).rotation*=Quaternion.Euler(0,-BL2,0);
		LLeg3.GetChild(2).rotation*=Quaternion.Euler(0,-BL2,0);
		LLeg3.GetChild(3).rotation*=Quaternion.Euler(0,-BL2,0);

		Vector3 vd3=BL_HIT+(BL_HIT-LLeg3.GetChild(2).position)+transform.up*offset2;
		Vector3 vd2=(vd1+transform.up*(vd1-LLeg1.position).magnitude);
		alt4=((vd1-vd2).magnitude-(vd3-vd2).magnitude)*(100/(vd1-vd2).magnitude);

		//Add rotations
		if(!isConstrained&&!isDead&&isOnGround&&!isOnLevitation)
		{
			FR_Add=Vector3.Angle(RArm1.position-RArm2.position,LArm1.position-RArm3.GetChild(1).position)-a1;
			FL_Add=Vector3.Angle(LArm1.position-LArm2.position,LArm1.position-LArm3.GetChild(1).position)-a2;
			BR_Add=Vector3.Angle(RLeg1.position-RLeg2.position,RLeg1.position-RLeg3.GetChild(2).position)-a3;
			BL_Add=Vector3.Angle(LLeg1.position-LLeg2.position,LLeg1.position-LLeg3.GetChild(2).position)-a4;
		}
	}
	#endregion
	
	#region PLAYER INPUTS
public void GetUserInputs(int idle1=0,int idle2=0,int idle3=0,int idle4=0,int eat=0,int drink=0,int sleep=0,int rise=0)
{
    // Only process inputs for owned creatures
    if(!IsOwner()) return;
    
    if(behavior=="Repose"&&anm.GetInteger("Move")!=0) behavior="Player";
    else if(behaviorCount<=0) { objTGT=null; behavior="Player"; behaviorCount=0; } else behaviorCount--;

    // Only process input if this is a local player or if we're the main controlled creature
    if(isLocalPlayer || photonView == null || photonView.IsMine)
    {
        //Run Button (Toggle) - Check stamina requirements
        bool run = isRunToggled && stamina >= minStaminaToRun;

        //Attack Button
        if(canAttack)
        {
            if(isAttackPressed) { behaviorCount=500; behavior="Hunt"; anm.SetBool("Attack",true); }
            else anm.SetBool("Attack",false);
        }

        //Crouch Button
        if(useIK && isCrouchPressed) { crouch=crouch_Max*size; onCrouch=true; }
        else onCrouch=false;

        // OBTENER INPUT DEL JOYSTICK ÚNICAMENTE
        float horizontalInput = 0f;
        float verticalInput = 0f;
        
        if(movementJoystick != null)
        {
            horizontalInput = movementJoystick.Horizontal();
            verticalInput = movementJoystick.Vertical();
            
            // Filtro adicional para evitar micro-movimientos
            if(Mathf.Abs(horizontalInput) < 0.1f) horizontalInput = 0f;
            if(Mathf.Abs(verticalInput) < 0.1f) verticalInput = 0f;
        }

        // Jump Button
        bool jumpPressed = isJumpPressed;

        // DASH SYSTEM - Solo para criaturas acuáticas
        if(canSwim && (isInWater || isOnWater) && isDashPressed && 
           stamina >= dashStaminaCost && dashCooldownTimer <= 0f && !isDashing)
        {
            // Ejecutar dash
            ExecuteAquaticDash();
        }

        //Fly/swim up/down
        if(canFly && !canSwim) // SOLO VOLADORES - CON SISTEMA HÍBRIDO CÁMARA
        {
            float pitchValue = 0.0f;
            
            // Prioridad a botones si están presionados
            if(isCrouchPressed) 
            {
                pitchValue = -1.0f; // Descender forzado
            }
            else if(jumpPressed) 
            {
                pitchValue = 1.0f;  // Ascender forzado
            }
            else if(useCameraPitchControl && mainCamera != null)
            {
                // Solo usar cámara si no hay botones presionados
                float cameraPitch = mainCamera.transform.eulerAngles.x;
                if(cameraPitch > 180f) cameraPitch -= 360f;
                
                if(Mathf.Abs(cameraPitch) > cameraPitchDeadZone)
                {
                    if(cameraPitch > 0) // Cámara hacia abajo = descender
                    {
                        pitchValue = Mathf.Clamp((cameraPitch - cameraPitchDeadZone) / 30f, 0f, 1f) * cameraPitchSensitivity;
                    }
                    else // Cámara hacia arriba = ascender
                    {
                        pitchValue = Mathf.Clamp((cameraPitch + cameraPitchDeadZone) / -30f, 0f, 1f) * -cameraPitchSensitivity;
                    }
                }
            }
            
            anm.SetFloat("Pitch", pitchValue);
        }
        else if(canSwim) // ACUÁTICOS
        {
            if(isCrouchPressed) 
            {
                anm.SetFloat("Pitch", 1.0f);
            }
            else if(jumpPressed) 
            {
                anm.SetFloat("Pitch", -1.0f);
            }
            else 
            {
                anm.SetFloat("Pitch", 0.0f);
            }
        }

        if(canJump && jumpPressed && !onJump) anm.SetInteger("Move",3);
        //Move
        else if(horizontalInput!=0||verticalInput!=0)
        {
            // SISTEMA DE MOVIMIENTO DUAL
            if(!isPreciseMovementToggled) 
            {
                // MOVIMIENTO SIMPLE - Va directo hacia donde apunta el joystick
                float cameraAngle = 0;
                if(mainCamera != null)
                {
                    cameraAngle = mainCamera.transform.eulerAngles.y;
                }
                float targetAngle = cameraAngle + Mathf.Atan2(horizontalInput, verticalInput) * Mathf.Rad2Deg;
                
                // Orientar dinosaurio directamente hacia esa dirección
                anm.SetFloat("Turn", targetAngle);
                
                // Caminar hacia adelante - Check stamina for running
                if(run && stamina >= minStaminaToRun)
                {
                    anm.SetInteger("Move", 2);
                }
                else
                {
                    anm.SetInteger("Move", 1);
                    // Auto-disable run if no stamina
                    if(isRunToggled && stamina < minStaminaToRun)
                    {
                        isRunToggled = false;
                        if(runButton != null)
                        {
                            ColorBlock colors = runButton.colors;
                            colors.normalColor = Color.white;
                            runButton.colors = colors;
                        }
                    }
                }
            }
            else
            {
                // MOVIMIENTO PRECISO - Strafe y retroceso
                if(verticalInput > 0) 
                {
                    if(run && stamina >= minStaminaToRun) anm.SetInteger("Move", 2);
                    else anm.SetInteger("Move", 1);
                }
                else if(verticalInput < 0) anm.SetInteger("Move", -1);
                else if(horizontalInput > 0) anm.SetInteger("Move", -10);
                else if(horizontalInput < 0) anm.SetInteger("Move", 10);
                
                if(Mathf.Abs(verticalInput) > 0 && Mathf.Abs(horizontalInput) > 0)
                {
                    if(verticalInput > 0)
                    {
                        if(run && stamina >= minStaminaToRun) anm.SetInteger("Move", 2);
                        else anm.SetInteger("Move", 1);
                        float currentTurn = anm.GetFloat("Turn");
                        anm.SetFloat("Turn", currentTurn + (45f * horizontalInput * Time.deltaTime));
                    }
                    else
                    {
                        anm.SetInteger("Move", -1);
                        float currentTurn = anm.GetFloat("Turn");
                        anm.SetFloat("Turn", currentTurn + (45f * horizontalInput * Time.deltaTime));
                    }
                }
            }
        }
        //Stop
        else
        {
            //Flying/Swim
            if((canSwim|canFly)&&!isOnGround)
            {
                if(canSwim&&anm.GetFloat("Pitch")!=0) 
                {
                    if(run && stamina >= minStaminaToRun) anm.SetInteger("Move", 2);
                    else anm.SetInteger("Move", 1);
                }
                else anm.SetInteger("Move",0);
            }
            //Terrestrial
            else
            {
                anm.SetInteger("Move",0); //Stop
            }
        }

        //Roar Button (Idles)
        if(isRoarPressed)
        {

        }
        //Eat Button
        else if(isEatPressed)
        {
            if(posTGT==Vector3.zero) FindPlayerFood(); //looking for food
            
            //Eat - CORREGIDO: Permitir comer en agua para acuáticos
            bool canEatHere = canSwim ? true : !isOnWater; // Acuáticos pueden comer en agua, terrestres no
            
            if(posTGT!=Vector3.zero && canEatHere)
            {
                anm.SetInteger("Idle",eat); behavior="Food";
                if(food<100) food=Mathf.Clamp(food+0.05f,0.0f,100f);
                if(water<25) water+=0.05f;
                if(!isEatPressed) posTGT=Vector3.zero;
            }
            //nothing found or can't eat here
            else
            {
                if(!canEatHere && !canSwim) Debug.Log("Use Drink button to drink water...");
                else Debug.Log("Nothing to eat...");
            }
        }
        //Drink Button
        else if(isDrinkPressed)
        {
            //Drink
            if(isOnWater)
            {
                anm.SetInteger("Idle",drink);
                if(water<100) { behavior="Water"; water=Mathf.Clamp(water+0.05f,0.0f,100f); }
                if(!isDrinkPressed) posTGT=Vector3.zero;
                else posTGT=transform.position;
            }
            //not near water
            else
            {
                Debug.Log("No water source nearby to drink...");
            }
        }
        //Sleep Button
        else if(isSleepPressed)
        {
            anm.SetInteger("Idle",sleep);
            if(anm.GetInteger("Move")!=0) anm.SetInteger("Idle",0);
        }
        //Rise
        else if(rise!=0 && jumpPressed) anm.SetInteger("Idle",rise);
        else { anm.SetInteger("Idle",0); if(!isDrinkPressed) posTGT=Vector3.zero; }

        //Angle gap
        if(mainCamera != null)
        {
            delta=Mathf.DeltaAngle(mainCamera.transform.eulerAngles.y,anm.GetFloat("Turn"));
        }

        if(OnAnm.IsName(specie+"|Sleep"))
        { 
            behavior="Repose"; 
            stamina=Mathf.Clamp(stamina+0.05f,0.0f,100f);
            
            // Regeneración acelerada de vida al dormir
            CreatureGrowthSystem growthSys = GetComponent<CreatureGrowthSystem>();
            if (growthSys != null && growthSys.UseDynamicHealth())
            {
                growthSys.HealDynamic(0.08f);
            }
            else
            {
                health = Mathf.Clamp(health + 1.0f, 0.0f, 100f);
            }
        }
    }
    // Not controlled, reset parameters
    else
    {
        anm.SetInteger("Move",0); anm.SetInteger("Idle",0); //Stop
        if(canAttack) anm.SetBool("Attack",false);
        if(canFly|canSwim) anm.SetFloat("Pitch",0.0f);
    }
}

// FUNCIÃ"N PARA EL TOGGLE DEL RUN BUTTON (Llamar desde el botÃ³n)
public void ToggleRun()
{
    if(!IsOwner() || isDead) return;
    
    // Check stamina solo al activar
    if(!isRunToggled && stamina < minStaminaToRun)
    {
        Debug.Log("Sin stamina para correr!");
        return;
    }
    
    isRunToggled = !isRunToggled;
    
    // Cambio visual inmediato
    if(runButton != null)
    {
        ColorBlock colors = runButton.colors;
        colors.normalColor = isRunToggled ? Color.green : Color.white;
        runButton.colors = colors;
    }
    
    Debug.Log($"RUN TOGGLE: {isRunToggled}");
}

public void ToggleCrouch()
{
    if(!IsOwner()) return;
    
    isCrouchPressed = !isCrouchPressed;
    
    if(crouchButton != null)
    {
        ColorBlock colors = crouchButton.colors;
        if(isCrouchPressed)
            colors.normalColor = Color.yellow;
        else
            colors.normalColor = Color.white;
        crouchButton.colors = colors;
    }
}

// FUNCIÃ"N PARA EL TOGGLE DEL MOVIMIENTO PRECISO (Llamar desde el botÃ³n)
public void TogglePreciseMovement()
{
    if(!IsOwner()) return;
    
    isPreciseMovementToggled = !isPreciseMovementToggled;
    
    // Cambiar color del botÃ³n para mostrar el estado
    if(preciseMovementButton != null)
    {
        ColorBlock colors = preciseMovementButton.colors;
        if(isPreciseMovementToggled)
        {
            colors.normalColor = Color.blue; // Azul cuando estÃ¡ activo (movimiento preciso)
        }
        else
        {
            colors.normalColor = Color.white; // Blanco cuando estÃ¡ inactivo (movimiento normal)
        }
        preciseMovementButton.colors = colors;
    }
}

// FUNCIONES PARA BOTONES (Llamar desde eventos OnPointerDown/OnPointerUp)
public void AttackButtonDown() { if(IsOwner()) isAttackPressed = true; }
public void AttackButtonUp() { if(IsOwner()) isAttackPressed = false; }

public void JumpButtonDown() { if(IsOwner()) isJumpPressed = true; }
public void JumpButtonUp() { if(IsOwner()) isJumpPressed = false; }

public void RoarButtonDown() 
{ 
    if(IsOwner() && !isDead) 
    {
        isRoarPressed = true;
        
        int idleToPlay = GetRandomRoarIdle();
        anm.SetInteger("Idle", idleToPlay);
        Debug.Log($"ROAR EJECUTADO - Idle: {idleToPlay} de {gameObject.name}");
    }
}

// Nueva función para obtener un idle aleatorio según el tipo de criatura
private int GetRandomRoarIdle()
{
    // Detectar tipo de criatura por script adjunto
    if(GetComponent<CarnLP>() != null)
    {
        // CarnLP tiene idles 1, 2, 3
        int[] carnIdles = new int[] { 1 };
        return carnIdles[Random.Range(0, carnIdles.Length)];
    }
    else if(GetComponent<RapLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] rapIdles = new int[] { 2 };
        return rapIdles[Random.Range(0, rapIdles.Length)];
    }
	    else if(GetComponent<AnkyLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] ankyIdles = new int[] { 1, 2 };
        return ankyIdles[Random.Range(0, ankyIdles.Length)];
    }
		else if(GetComponent<DiloLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] diloIdles = new int[] { 2, 3 };
        return diloIdles[Random.Range(0, diloIdles.Length)];
    }
		else if(GetComponent<DimeLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] dimeIdles = new int[] { 1 };
        return dimeIdles[Random.Range(0, dimeIdles.Length)];
    }
		else if(GetComponent<BaryLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] baryIdles = new int[] { 1 };
        return baryIdles[Random.Range(0, baryIdles.Length)];
    }
		else if(GetComponent<OviLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] oviIdles = new int[] { 1, 2 };
        return oviIdles[Random.Range(0, oviIdles.Length)];
    }
		else if(GetComponent<QuetLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] quetIdles = new int[] { 1, 3 };
        return quetIdles[Random.Range(0, quetIdles.Length)];
    }
		else if(GetComponent<SpinoLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] spinoIdles = new int[] { 1 };
        return spinoIdles[Random.Range(0, spinoIdles.Length)];
    }
		else if(GetComponent<StegLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] stegIdles = new int[] { 1, 2, 3 };
        return stegIdles[Random.Range(0, stegIdles.Length)];
    }
		else if(GetComponent<PachyLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] pachyIdles = new int[] { 3 };
        return pachyIdles[Random.Range(0, pachyIdles.Length)];
    }
		else if(GetComponent<KentLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] kentIdles = new int[] { 2 };
        return kentIdles[Random.Range(0, kentIdles.Length)];
    }
	
		else if(GetComponent<SarcoLP>() != null)
    {
        // RapLP tiene idles 1, 2, 4
        int[] sarcodles = new int[] { 1 };
        return sarcodles[Random.Range(0, sarcodles.Length)];
    }
    else if(GetComponent<RexLP>() != null)
    {
        // TrexLP tiene todos los idles
        //return Random.Range(1, 5);
		//----------------
		 int[] rexIdles = new int[] { 1 };
        return rexIdles[Random.Range(0, rexIdles.Length)];
		
    }
    // Agregar más tipos de criaturas aquí según necesites
    // else if(GetComponent<MosaLP>() != null)
    // {
    //     int[] mosaIdles = new int[] { 1, 3 };
    //     return mosaIdles[Random.Range(0, mosaIdles.Length)];
    // }
    
    // Por defecto, usar rango completo si no se reconoce el tipo
    return Random.Range(1, 5);
}

public void RoarButtonUp() 
{ 
    if(IsOwner() && !isDead) 
    {
        isRoarPressed = false; 
    }
}

public void SleepButtonDown() { if(IsOwner()) isSleepPressed = true; }
public void SleepButtonUp() { if(IsOwner()) isSleepPressed = false; }

public void EatButtonDown() { if(IsOwner()) isEatPressed = true; }
public void EatButtonUp() { if(IsOwner()) isEatPressed = false; }

public void DrinkButtonDown() { if(IsOwner()) isDrinkPressed = true; }
public void DrinkButtonUp() { if(IsOwner()) isDrinkPressed = false; }

public void CrouchButtonDown() 
{ 
    if(IsOwner()) 
    {
        isCrouchPressed = true; 
        Debug.Log("CROUCH BUTTON DOWN ACTIVADO");
    }
}

public void CrouchButtonUp() 
{ 
    if(IsOwner()) 
    {
        isCrouchPressed = false; 
        Debug.Log("CROUCH BUTTON UP ACTIVADO");
    }
}

void ExecuteAquaticDash()
{
    if(!canSwim || (!isInWater && !isOnWater) || isDead || isDashing) return;
    
    // Verificar stamina
    if(stamina < dashStaminaCost)
    {
        Debug.Log("Sin stamina para dash!");
        return;
    }
    
    // DIAGNÓSTICO: Verificar estado del Rigidbody
    Debug.Log($"DASH DEBUG - Masa: {body.mass}, Drag: {body.drag}, Constraints: {body.constraints}");
    Debug.Log($"DASH DEBUG - Velocidad actual: {body.velocity.magnitude}");
    
    // Calcular dirección del dash
    Vector3 dashDirection = Vector3.zero;
    
    // Usar la dirección actual del movimiento o hacia adelante por defecto
    if(movementJoystick != null)
    {
        float horizontalInput = movementJoystick.Horizontal();
        float verticalInput = movementJoystick.Vertical();
        
        if(Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            // Dash en dirección del joystick
            float cameraAngle = 0;
            if(mainCamera != null)
            {
                cameraAngle = mainCamera.transform.eulerAngles.y;
            }
            float targetAngle = cameraAngle + Mathf.Atan2(horizontalInput, verticalInput) * Mathf.Rad2Deg;
            dashDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }
        else
        {
            // Sin input de joystick, dash hacia adelante
            dashDirection = transform.forward;
        }
    }
    else
    {
        // Fallback: dash hacia adelante
        dashDirection = transform.forward;
    }
    
    // Agregar componente vertical si está nadando
    if(isInWater)
    {
        float pitchInput = anm.GetFloat("Pitch");
        dashDirection.y = pitchInput * 0.3f; // 30% de la fuerza hacia arriba/abajo
        dashDirection = dashDirection.normalized;
    }
    
    // CORRECCIÓN 1: Limpiar velocidad previa para evitar interferencias
    body.velocity = Vector3.zero;
    
    // CORRECCIÓN 2: Remover restricciones temporalmente
    RigidbodyConstraints originalConstraints = body.constraints;
    body.constraints = RigidbodyConstraints.FreezeRotation; // Solo bloquear rotación
    
    // CORRECCIÓN 3: Reducir drag ANTES del impulso
    float originalDrag = body.drag;
    body.drag = 0.1f;
    
    // CORRECCIÓN 4: Calcular fuerza más agresiva
    // Usar ForceMode.VelocityChange para ignorar masa
    float dashVelocity = dashForceMultiplier * 20f; // Velocidad directa en lugar de fuerza
    body.AddForce(dashDirection * dashVelocity, ForceMode.VelocityChange);
    
    // ALTERNATIVA: Si aún no funciona, usar velocidad directa
    // body.velocity = dashDirection * dashVelocity;
    
    // Configurar estado de dash
    isDashing = true;
    dashTimer = 0.5f; // Aumentar duración para mantener el efecto
    
    // Consumir stamina inmediatamente
    stamina = Mathf.Clamp(stamina - dashStaminaCost, 0.0f, 100f);
    
    // Efectos visuales/sonoros
    if(source != null && source.Length > 0)
    {
        source[0].pitch = Random.Range(0.8f, 1.2f);
    }
    
    // Sincronizar dash en red
    if(photonView != null && photonView.IsMine)
    {
        photonView.RPC("SyncAquaticDash", RpcTarget.Others, dashDirection, dashVelocity);
    }
    
    Debug.Log($"DASH EJECUTADO: Dirección={dashDirection}, Velocidad={dashVelocity}");
    Debug.Log($"DASH DEBUG - Nueva velocidad: {body.velocity.magnitude}");
    
    // Restaurar configuración original después de un frame
    StartCoroutine(RestoreRigidbodySettings(originalConstraints, originalDrag));
}

// ========== NUEVA CORRUTINA PARA RESTAURAR CONFIGURACIONES ==========
System.Collections.IEnumerator RestoreRigidbodySettings(RigidbodyConstraints originalConstraints, float originalDrag)
{
    yield return new WaitForFixedUpdate(); // Esperar un frame de física
    
    // Restaurar constraints originales
    body.constraints = originalConstraints;
    
    // El drag se restaurará gradualmente en la función Move()
    // No lo restauramos aquí para mantener el efecto de dash
}

// ========== NUEVO RPC SyncAquaticDash() ==========

[PunRPC]
void SyncAquaticDash(Vector3 direction, float velocity)
{
    if(!photonView.IsMine && canSwim)
    {
        // Limpiar velocidad previa
        body.velocity = Vector3.zero;
        
        // Aplicar impulso
        body.AddForce(direction * velocity, ForceMode.VelocityChange);
        
        isDashing = true;
        dashTimer = 0.5f;
        
        // Efectos visuales en clientes remotos
        if(source != null && source.Length > 0)
        {
            source[0].pitch = Random.Range(0.8f, 1.2f);
        }
        
        Debug.Log($"DASH SINCRONIZADO: Velocidad={body.velocity.magnitude}");
    }
}

// ========== FUNCIÓN DE DEBUG PARA PROBAR ==========
// Llama esta función desde el botón o consola para probar
public void TestDashForce()
{
    if(!canSwim) return;
    
    Debug.Log("=== PRUEBA DE DASH ===");
    Debug.Log($"CanSwim: {canSwim}");
    Debug.Log($"IsInWater: {isInWater}");
    Debug.Log($"IsOnWater: {isOnWater}");
    Debug.Log($"Stamina: {stamina}/{dashStaminaCost}");
    Debug.Log($"Cooldown: {dashCooldownTimer}");
    Debug.Log($"IsDashing: {isDashing}");
    Debug.Log($"Masa del body: {body.mass}");
    Debug.Log($"Drag del body: {body.drag}");
    Debug.Log($"Constraints: {body.constraints}");
    Debug.Log($"DashForceMultiplier: {dashForceMultiplier}");
    
    // Forzar dash para prueba
    if(stamina >= dashStaminaCost && dashCooldownTimer <= 0f)
    {
        ExecuteAquaticDash();
    }
}

// ========== FUNCIONES PARA BOTONES DE DASH ==========
public void DashButtonDown() 
{ 
    if(IsOwner() && canSwim && !isDead) 
    {
        isDashPressed = true; 
        Debug.Log("DASH BUTTON PRESSED");
    }
}

public void DashButtonUp() 
{ 
    if(IsOwner() && canSwim && !isDead) 
    {
        isDashPressed = false; 
    }
}



	bool FindPlayerFood()
	{
		//Find carnivorous food (looking for a dead creature in range)
		if(!herbivorous)
		{
			foreach(Creature other in allCreatures)
			{
				if(other == null || other == this) continue;
				if((other.transform.position-Head.position).magnitude>boxscale.z) continue; //not in range
				if(other.isDead) { objTGT=other.gameObject; posTGT=other.body.worldCenterOfMass; return true; } // meat found
			}
		}
		else
		{
			//Find herbivorous food (looking for trees/details on terrain in range )
			if(t)
			{
				//Large creature, look for trees
				if(withersSize>8)
				{
					if(Physics.CheckSphere(Head.position,withersSize,treeLayer)) { posTGT=Head.position; return true; }
					else return false;
				}
				//Look for grass detail
				else
				{
					float x=((transform.position.x-t.transform.position.x)/tdata.size.z*tres);
					float y=((transform.position.z-t.transform.position.z)/tdata.size.x*tres);

					for(int layer=0;layer<tdata.detailPrototypes.Length;layer++)
					{
						if(tdata.GetDetailLayer((int)x,(int)y,1,1,layer)[0,0]>0)
						{
							posTGT.x=(tdata.size.x/tres)*x+t.transform.position.x;
							posTGT.z=(tdata.size.z/tres)*y+t.transform.position.z;
							posTGT.y=t.SampleHeight(new Vector3(posTGT.x,0,posTGT.z));
							objTGT=null; return true;
						}
					}
				}
			}
		}

		objTGT=null; posTGT=Vector3.zero; return false; //nothing found...
	}
	#endregion
	
	#region ARTIFICIAL INTELLIGENCE
	#region CORE
	public void AICore(int idle1=0,int idle2=0,int idle3=0,int idle4=0,int eat=0,int drink=0,int sleep=0)
	{
		// Only run AI for owned creatures
		if(!IsOwner()) return;
		
		//Look for a target
		if(posTGT==Vector3.zero)
		{
			if(nextPath>=pathEditor.Count) nextPath=0; //reset path list
																								 // edited path 
			if(pathEditor.Count>0&&Random.Range(0,100)<pathEditor[nextPath].priority)
			{ objTGT=pathEditor[nextPath].waypoint; posTGT=pathEditor[nextPath].waypoint.transform.position; behavior="ToWaypoint"; behaviorCount=4000; }
			// look for water
			else if(canWalk&&Random.Range(0,75)>water) { FindWater(); behaviorCount=4000; }
			// look for food or prey
			else if(Random.Range(0,75)>food)
			{
				if(!herbivorous) { if(!FindFood()) FindPrey(); }
				else FindFood(); behaviorCount=4000;
			}
			// to sleep
			else if(!canSwim&&Random.Range(0,50)>stamina) { behavior="ToRepose"; FindPath(); behaviorCount=4000; }
			// look for friend
			else if(Random.Range(0,5)==0) { FindFriend(); behaviorCount=4000; }
			// if nothing found, find a random path
			if(posTGT==Vector3.zero) { behavior="ToPath"; FindPath(); behaviorCount=4000; }
		}
		//Target found
		else
		{
			//search for enemy
			if(targetEditor.Count>0) { if(!FindCustomTarget()) FindEnemy(); } else FindEnemy();
			//Execute current behavior
			ExecuteBehavior(idle1,idle2,idle3,idle4,eat,drink,sleep);
		}
	}
	#endregion
	#region SEARCH
	//***********************************************************************************************************************************************************************************************************
	//FIND WATER (Find nearest water point, need water layer)
	bool FindWater()
	{
		objTGT=null; float i=0, range=withersSize;
		while(range<waterMaxRange)
		{
			while(i<360)
			{
				Vector3 V1=transform.position+(Quaternion.Euler(0,i,0)*Vector3.forward*range);
				if(Physics.Raycast(V1+Vector3.up*withersSize,-Vector3.up*waterMaxRange,out RaycastHit hit)&&
					!Physics.Linecast(transform.position+Vector3.up*withersSize,V1+Vector3.up*withersSize))
				{
					if(hit.transform.gameObject.layer.Equals(waterLayer)&&Physics.Linecast(hit.point,hit.point-Vector3.up,-1,QueryTriggerInteraction.Ignore))
					{
						behavior="ToWater"; posTGT=hit.point; return true; //Found
					}
					else { i+=15; } //not match
				}
				else { i+=15; } //not match
			}
			range+=withersSize; i=0;
		}

		posTGT=Vector3.zero; return false;
	}

	//***********************************************************************************************************************************************************************************************************
	//FIND FOOD
	bool FindFood()
	{
		//Find carnivorous food (looking for a dead creature)
		if(!herbivorous)
		{
			foreach(Creature other in allCreatures)
			{
				if(other == null || other == this) continue;
				if((other.transform.position-transform.position).magnitude>foodMaxRange) continue; //not in range
				if(!canSwim&&other.isInWater) continue;
				if(canSwim&&!canWalk&&!other.isInWater) continue;
				if(other.isDead) { behavior="ToFood"; objTGT=other.gameObject; posTGT=other.body.worldCenterOfMass; return true; } // meat found
			}
		}
		else
		{
			//Find herbivorous food (looking for trees/details on terrain )
			if(t)
			{
				//Large creature, look for trees
				if(withersSize>8)
				{

					float i=0;
					while(i<360)
					{
						Vector3 V1=transform.position+(Quaternion.Euler(0,i,0)*Vector3.forward*(foodMaxRange/4));
						if(Physics.Linecast(V1+Vector3.up*withersSize,transform.position+Vector3.up*withersSize,out RaycastHit hit,treeLayer))
						{ behavior="ToFood"; posTGT=hit.point; return true; } //tree found
						else i++; // not found, continue
					}
					objTGT=null; posTGT=Vector3.zero; return false; //not found

				}
				//Look for grass detail
				else
				{
					float sx=((transform.position.x-t.transform.position.x)/tdata.size.x*tres)-2, x=sx;
					float sy=((transform.position.z-t.transform.position.z)/tdata.size.z*tres)-2, y=sy;

					for(y=sy;y<(sy+2);y++)
					{
						for(x=sx;x<(sx+2);x++)
						{
							for(int layer=0;layer<tdata.detailPrototypes.Length;layer++)
							{
								if(tdata.GetDetailLayer((int)x,(int)y,1,1,layer)[0,0]>0)
								{
									posTGT.x=(tdata.size.x/tres)*x+t.transform.position.x;
									posTGT.z=(tdata.size.z/tres)*y+t.transform.position.z;
									posTGT.y=t.SampleHeight(new Vector3(posTGT.x,0,posTGT.z))+t.GetPosition().y;
									if(!Physics.Linecast(transform.position+Vector3.up*withersSize,posTGT+Vector3.up*withersSize)) { objTGT=null; behavior="ToFood"; return true; }
								}
							}
						}
					}

				}
			}
		}
		objTGT=null; posTGT=Vector3.zero; return false; //not found
	}

	//***********************************************************************************************************************************************************************************************************
	//FIND PREY (Find a size suited prey for carnivorous by priority)
	bool FindPrey()
	{
		Vector3 V1=Vector3.zero;
		foreach(Creature other in allCreatures)
		{
			if(other == null || other == this) continue;
			if((other.transform.position-transform.position).magnitude>preyMaxRange) continue;  //not in range

			if(!canSwim&&other.isInWater) continue;
			if(canSwim&&!canWalk&&!other.isInWater) continue;

			if((other.herbivorous&&other.withersSize<withersSize*3))
			{ behavior="ToHunt"; objTGT=other.gameObject; posTGT=other.body.worldCenterOfMass; return true; } // suitable herbivorous prey found
			else if((!other.herbivorous&&other.withersSize<withersSize*1.5f)&&!other.specie.Equals(specie))
			{ behavior="ToHunt"; objTGT=other.gameObject; posTGT=other.body.worldCenterOfMass; } // suitable carmivorous prey found
			else if(food==0&&other.withersSize<withersSize*3) { objTGT=other.gameObject; V1=other.body.worldCenterOfMass; } // any prey, cannibalism allowed
		}
		if(V1==Vector3.zero) return false; else { behavior="ToHunt"; posTGT=V1; return true; }
	}

	//***********************************************************************************************************************************************************************************************************
	//FIND FRIEND (Find in current creature list a same specie creature and share his activity)
	bool FindFriend()
	{
		foreach(Creature other in allCreatures)
		{
			if(other == null || other == this) continue;
			float range=(other.transform.position-transform.position).magnitude;
			if(range>friendMaxRange|range<boxscale.x*25) continue; //not in range
			else if(!other.specie.Equals(specie)|other.isDead) continue;  //skip, not same specie or dead
			else if(other.isInWater&&!canSwim) continue; //skip, in water and can't swim
			else if(!other.isInWater&&canSwim) continue; //skip, not in water and can't walk

			// share friend prey
			if(other.behavior.EndsWith("Hunt")&&other.objTGT!=transform.gameObject)
			{ behavior="ToHunt"; objTGT=other.objTGT; posTGT=other.posTGT; return true; }
			else if(other.behavior.Equals("Battle")&&other.objTGT!=transform.gameObject)
			{ behavior="Battle"; objTGT=other.objTGT; posTGT=other.posTGT; return true; }
			// share friend food
			else if(other.behavior.EndsWith("Food")&&food<75)
			{ behavior="ToFood"; objTGT=other.objTGT; posTGT=other.posTGT; return true; }
			// share friend water
			else if(other.behavior.EndsWith("Water")&&water<75)
			{ behavior="ToWater"; posTGT=other.posTGT; return true; }
			// goto friend position
			else
			{
				lookTGT=other.transform.position;
				behavior="ToHerd"; objTGT=other.gameObject; posTGT=other.body.worldCenterOfMass; return true;
			}
		}

		objTGT=null; posTGT=Vector3.zero; return false; //nothing found
	}

	//***********************************************************************************************************************************************************************************************************
	//FIND ENEMY (find any hostile target in current creature list and adapts its behavior according to the target)
	bool FindEnemy()
	{
		if(loop==0&&!behavior.Equals("ToFlee")&&!behavior.Equals("Battle")&&!behavior.EndsWith("Hunt")&&!behavior.Equals("ToTarget"))
		{
			//Look for all creatures
			foreach(Creature other in allCreatures)
			{
				if(other == null || other == this) continue;
				float range=(other.transform.position-transform.position).magnitude; //range
				if(range>enemyMaxRange) continue; //not in range

				if(other.isDead) continue; //skip, dead

				//Carnivorous behavior
				if(!herbivorous)
				{
					if(!other.herbivorous&&(other.behavior.EndsWith("Hunt")|other.behavior.Equals("Battle")))
					{
						if(other.specie==specie&&other.objTGT!=transform.gameObject) continue;
						if(boxscale.z>other.boxscale.z/1.5f)
						{
							behavior="Battle"; objTGT=other.gameObject;
							posTGT=other.transform.position; return true;
						}
						else
						{ behavior="ToFlee"; objTGT=other.gameObject; FindPath(true); return true; }
					}
					else if((other.herbivorous&&other.behavior.Equals("Battle"))
						&&boxscale.z>other.boxscale.z/3&&other.objTGT==transform.gameObject)
					{
						behavior="ToFlee"; behaviorCount=1000;
						objTGT=other.gameObject;
						FindPath(true); return true;
					}
				}
				//Herbivorous behavior
				else
				{
					if(!other.herbivorous)
					{
						if(other.behavior.EndsWith("Hunt")|(other.objTGT==transform.gameObject&&other.behavior.Equals("Battle")))
						{
							if(canAttack&&boxscale.z>other.boxscale.z/3&&health>25&&other.objTGT==transform.gameObject)
							{
								behavior="Battle"; behaviorCount=1000;
								objTGT=other.gameObject;
								posTGT=other.body.worldCenterOfMass;
								return true;
							}
							else if(!other.behavior.Equals("ToFlee"))
							{
								behavior="ToFlee"; behaviorCount=1000;
								objTGT=other.gameObject;
								FindPath(true); return true;
							}
						}
					}
				}
			}
		}
		return false;
	}

	//***********************************************************************************************************************************************************************************************************
	//FIND CUSTOM TARGET (find any enemy or friend target added into target editor list)
	bool FindCustomTarget()
	{
		if(loop==0)
		{
			//Looking for custom target
			foreach(TargetEditor o in targetEditor)
			{
				if(!o._GameObject) { targetEditor.Remove(o); continue; } //gameobject no more exist
				if(o.MaxRange!=0&&(o._GameObject.transform.position-transform.position).magnitude>o.MaxRange) continue; //not in range

				if(o._TargetType==TargetType.Enemy)
				{
					if(canAttack)
					{
						if((o._GameObject.transform.position-transform.position).magnitude>enemyMaxRange)
						{ objTGT=o._GameObject; posTGT=o._GameObject.transform.position; behavior="ToTarget"; return true; }
						else
						{ objTGT=o._GameObject; posTGT=o._GameObject.transform.position; behavior="Battle"; return true; }

					}
					else if((o._GameObject.transform.position-transform.position).magnitude<enemyMaxRange)
					{ objTGT=o._GameObject; behavior="ToFlee"; FindPath(true); return true; }
				}
				else
				{
					if((o._GameObject.transform.position-transform.position).magnitude<boxscale.z*10) continue; //target are near
					Creature other=o._GameObject.GetComponent<Creature>(); //Get other creature script
					if(other)
					{
						// share friend prey/enemy
						if(other.behavior.EndsWith("Hunt")&&other.objTGT!=transform.gameObject)
						{
							if(!herbivorous) behavior="ToHunt"; else behavior="Battle";
							objTGT=other.objTGT; posTGT=other.posTGT; return true;
						}
						else if(other.behavior.Equals("Battle")&&other.objTGT!=transform.gameObject)
						{ behavior="Battle"; objTGT=other.objTGT; posTGT=other.posTGT; return true; }
						// share friend food
						else if(other.behavior.EndsWith("Food")&&food<75)
						{ behavior="ToFood"; objTGT=other.objTGT; posTGT=other.posTGT; return true; }
						// share friend water
						else if(other.behavior.EndsWith("Water")&&water<75)
						{ behavior="ToWater"; posTGT=other.posTGT; return true; }
						// goto friend position
						else
						{
							lookTGT=other.transform.position;
							behavior="ToFriend"; objTGT=other.gameObject; posTGT=other.body.worldCenterOfMass; return true;
						}
					}
					else
					{
						objTGT=o._GameObject; lookTGT=objTGT.transform.position;
						posTGT=o._GameObject.transform.position; behavior="ToFriend"; return true;
					}
				}
			}
			return false;
		}
		return true;
	}

	//***********************************************************************************************************************************************************************************************************
	//FIND PATH (find reachable path)
	bool FindPath(bool invert=false)
	{
		RaycastHit hit; Vector3 TGT=Vector3.zero; float dist, angle, alt;
		// FLY TYPE
		if(canFly)
		{
			//altitude
			if(isOnGround) alt=Random.Range(-90f,0);
			else if(posY<(lowAltitude ? -75 : -150)) { alt=Random.Range(0,45f); }

			else alt=Random.Range(-15f,0);
			//distance
			dist=Random.Range(boxscale.z*10,boxscale.z*20);
			//direction 
			if(invert&&objTGT)
			{
				angle=Random.Range(-15f-angleAdd,15f+angleAdd);
				TGT=transform.position+(Quaternion.Euler(alt,objTGT.transform.eulerAngles.y+angle,0)*Vector3.forward*dist);
			}
			else
			{
				angle=Random.Range(-45f-angleAdd,45f+angleAdd);
				TGT=transform.position+(Quaternion.Euler(alt,transform.eulerAngles.y+angle,0)*Vector3.forward*dist);
			}
			//check if position is reachable
			if(behavior.Equals("ToRepose"))  //check for ground...
			{
				if(Physics.Linecast(TGT+Vector3.up*withersSize,TGT-Vector3.up*200,out hit))
				{ if(hit.collider.gameObject.layer.Equals(waterLayer)) TGT=Vector3.zero; else TGT=hit.point; }
			}
			else if(Physics.Linecast(transform.position+Vector3.up*withersSize,TGT+Vector3.up*withersSize)) TGT=Vector3.zero; //check for obstacle
		}
		// GROUND TYPE
		else if((canWalk&&!canSwim)|(canSwim&&canWalk&&!isOnWater&&!isInWater))
		{
			//direction and distance
			if(invert&&objTGT)
			{
				dist=Random.Range(boxscale.z*4,boxscale.z*10);
				angle=Random.Range(-15-angleAdd,15+angleAdd);
				TGT=transform.position+(Quaternion.Euler(0,objTGT.transform.eulerAngles.y+angle,0)*Vector3.forward*dist);
			}
			else
			{
				dist=Random.Range(boxscale.z*4,boxscale.z*10);
				angle=Random.Range(-45-angleAdd,45+angleAdd);
				TGT=transform.position+(Quaternion.Euler(0,transform.eulerAngles.y+angle,0)*Vector3.forward*dist);
			}

			//check if position is reachable
			if(!Physics.Linecast(transform.position+Vector3.up*withersSize,TGT+Vector3.up*withersSize)&&Physics.Linecast(TGT+Vector3.up*withersSize,TGT-Vector3.up*withersSize*2,out hit))
			{ if(hit.collider.gameObject.layer.Equals(waterLayer)&&!canSwim) TGT=Vector3.zero; else TGT=hit.point; }
			else TGT=Vector3.zero;
		}
		// SWIM TYPE
		else if(canSwim)
		{
			//altitude
			if(isInWater)
			{
				if(lowAltitude) { if(isOnGround) alt=0; else alt=Random.Range(0,45f); }
				else if(isOnWater) alt=Random.Range(0,45f);
				else alt=Random.Range(-60f,60f);
			}
			else alt=Random.Range(0,45f);
			//direction and distance
			if(invert&&objTGT)
			{
				dist=Random.Range(boxscale.z*10,boxscale.z*20);
				angle=Random.Range(-15f-angleAdd,15f+angleAdd);
				TGT=transform.position+(Quaternion.Euler(alt,objTGT.transform.eulerAngles.y+angle,0)*Vector3.forward*dist);
			}
			else
			{
				dist=Random.Range(boxscale.z*4,boxscale.z*15);
				angle=Random.Range(-45f-angleAdd,45f+angleAdd);
				if(isInWater)
				{
					if(lowAltitude) { if(isOnGround) alt=0; else alt=Random.Range(0,45f); }
					else if(isOnWater) alt=Random.Range(0,45f);
					else alt=Random.Range(-60f,60f);
				}
				else alt=Random.Range(0,45f);
				TGT=transform.position+(Quaternion.Euler(alt,transform.eulerAngles.y+angle,0)*Vector3.forward*dist);
			}

			//check if position is reachable
			if(isInWater&&TGT.y>waterY) TGT=Vector3.zero;
			if(Physics.Linecast(transform.position+Vector3.up*withersSize,TGT+Vector3.up*withersSize)) TGT=Vector3.zero; //check for obstacle
		}

		// RESULT
		if(angleAdd>360) { angleAdd=0; posTGT=Vector3.zero; return false; }
		if(TGT==Vector3.zero) // not found
		{
			angleAdd+=5; anm.SetInteger("Move",0); anm.SetInteger("Idle",0);
			if(!invert) posTGT=Vector3.zero;
			return false;
		}
		else { angleAdd=0; posTGT=TGT; return true; } //reachable position found
	}
	#endregion
	#region EXECUTE BEHAVIOR
	//***********************************************************************************************************************************************************************************************************
	//EXECUTE BEHAVIOR
	void ExecuteBehavior(int idle1=0,int idle2=0,int idle3=0,int idle4=0,int eat=0,int drink=0,int sleep=0)
	{
		if(posTGT==Vector3.zero) return;
		bool EndBehavior=false; Creature other=null;
		// Idles to play for current instance
		int idles_lenght=0; if(idle1>0) idles_lenght++; if(idle2>0) idles_lenght++; if(idle3>0) idles_lenght++; if(idle4>0) idles_lenght++;
		// Generate random action
		if(loop==0) { rndMove=Random.Range(0,100); if(idles_lenght>0) rndIdle=Random.Range(0,idles_lenght+1); }

		if(objCOL) // obstacle object
		{
			Quaternion r=Quaternion.LookRotation(posCOL-transform.position); //obstacle direction
			avoidDelta=Mathf.DeltaAngle(r.eulerAngles.y,transform.eulerAngles.y); //obstacle angle gap
			if(Mathf.Abs(avoidDelta)>90) avoidDelta=0; //max avoid delta
		}

		if(objTGT) // object target
		{
			other=objTGT.GetComponent<Creature>();
			if(other&&!behavior.Equals("ToFlee")) posTGT=other.body.worldCenterOfMass;
		}

		distTGT=(transform.position-posTGT).magnitude; //target distance
		angTGT=Quaternion.LookRotation(posTGT-transform.position); //target direction
		delta=Mathf.DeltaAngle(angTGT.eulerAngles.y,transform.eulerAngles.y); //target angle gap
		actionDist=(transform.position-headPos).magnitude; //distance from head

		Debug.DrawLine(Head.transform.position,posTGT);

		// Set Animator parameters for each behavior
		switch(behavior)
		{
			case "ToPath":
			if(canFly)
			{
				if(distTGT>boxscale.z*4.0f) AnmRun(rndMove);
				else { if(isOnGround) AnmStop(); EndBehavior=true; }
			}
			else
			{
				if(distTGT>actionDist*2.0f)
				{
					if(!canSwim&&(!isOnGround&&(isInWater|isOnWater))) AnmRun(rndMove);
					else
						AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
				}
				else { AnmStop(); EndBehavior=true; }
			}
			break;

			case "ToWaypoint":
			if(distTGT>actionDist*3.0f)
			{
				if(pathEditor[nextPath].pathType==PathType.Run) AnmRun(rndMove);
				else
					AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
			}
			else
			{
				if(pathEditor[nextPath].targetAction==TargetAction.Sleep&&stamina!=100) AnmSleep(sleep);
				else if(pathEditor[nextPath].targetAction==TargetAction.Eat&&food!=100) AnmEat(rndMove,eat,rndIdle,idle1,idle2,idle3,idle4);
				else if(pathEditor[nextPath].targetAction==TargetAction.Drink&&water!=100) AnmDrink(drink);
				else { AnmStop(); EndBehavior=true; nextPath++; }
			}
			break;


			case "ToTarget":
			case "ToFriend":
			if(objTGT)
			{
				if(distTGT>actionDist*4.0f) AnmRun(rndMove);
				else if(distTGT>actionDist*2.0f)
				{
					if(canFly) AnmRun(rndMove);
					else
						AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
				}
				else { AnmStop(); EndBehavior=true; }
			}
			else { AnmStop(); EndBehavior=true; }
			break;

			case "ToFlee":
			if(objTGT&&(objTGT.transform.position-transform.position).magnitude<enemyMaxRange*2)
			{
				AnmRun(rndMove); if(distTGT<actionDist*4.0f) FindPath(true);
			}
			else { AnmStop(); EndBehavior=true; }
			break;

			case "ToHerd":
			if(objTGT)
			{
				if(other&&other.health==0) { AnmStop(); EndBehavior=true; }
				else if(other&&other.isInWater&&!canSwim&&!canWalk) { AnmStop(); EndBehavior=true; }
				else if(other&&!other.isInWater&&canSwim&&!canWalk) { AnmStop(); EndBehavior=true; }
				else if(distTGT>actionDist*10.0f&&canFly) AnmRun(rndMove);
				else if(distTGT>actionDist*3.0f) AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
				// same species contest
				else if(other)
				{
					if(other.behavior.Equals("Contest")) { AnmRun(rndMove); behavior="ToFlee"; FindPath(true); }
					else if(canWalk&&canAttack&&!canTailAttack&&rndMove<=10&&other.behavior.Equals("ToHerd"))
					{
						other.objTGT=transform.gameObject;
						other.behavior="Contest";
						other.behaviorCount=500;
						behavior="Contest"; behaviorCount=500; AnmStop();
					}
					else { AnmStop(); EndBehavior=true; }
				}
				else { AnmStop(); EndBehavior=true; }
			}
			else { AnmStop(); EndBehavior=true; }
			break;

			case "ToFood":
			if(food==100|(!herbivorous&&!objTGT)) { AnmStop(); EndBehavior=true; }
			else
			{
				if(other&&other.health!=0) { AnmStop(); EndBehavior=true; }
				else if(!canSwim&&isInWater) { AnmStop(); EndBehavior=true; }
				else if(!canWalk&&!isInWater) { AnmStop(); EndBehavior=true; }
				else if(canFly&&distTGT>actionDist*4.0f) AnmRun(rndMove);
				else if(!herbivorous&&distTGT>actionDist*1.25f) AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
				else if(herbivorous&&distTGT>actionDist) AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
				else { AnmStop(); behavior="Food"; behaviorCount=5000; }
			}
			break;

			case "Food":
			if(food==100|(!herbivorous&&!objTGT)) { AnmStop(); EndBehavior=true; }
			else
			{
				if(other&&other.health!=0) { AnmStop(); EndBehavior=true; }
				else if(!canSwim&&isInWater) { AnmStop(); EndBehavior=true; }
				else if(!canWalk&&!isInWater) { AnmStop(); EndBehavior=true; }
				else if(!herbivorous&&distTGT<actionDist*1.25f|(objTGT==objCOL)) AnmEat(rndMove,eat,rndIdle,idle1,idle2,idle3,idle4);
				else if(herbivorous&&distTGT<actionDist) AnmEat(rndMove,eat,rndIdle,idle1,idle2,idle3,idle4);
				else behavior="ToFood";
			}
			break;

			case "ToWater":
			if(water==100|isInWater) { AnmStop(); EndBehavior=true; }
			else if(canFly&&distTGT>actionDist) AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
			else if(isOnWater&&isOnGround)
			{
				if(canSwim) { AnmStop(); behavior="ToPath"; FindPath(); }
				else { AnmStop(); behavior="Water"; behaviorCount=5000; }
			}
			else AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
			break;

			case "Water":
			if(water==100|isInWater) { AnmStop(); EndBehavior=true; }
			else if(isOnWater&&isOnGround&&!canSwim) AnmDrink(drink);
			else behavior="ToWater";
			break;

			case "ToRepose":
			if(stamina==100) { AnmStop(); EndBehavior=true; }
			else if(canFly&&distTGT>boxscale.z*5.0f) AnmRun(rndMove);
			else if(distTGT>boxscale.z) AnmWalk(rndMove,rndIdle,idle1,idle2,idle3,idle4);
			else { AnmStop(); behavior="Repose"; behaviorCount=5000; }
			break;

			case "Repose":
			if(stamina==100) { AnmStop(); EndBehavior=true; }
			else AnmSleep(sleep);
			break;

			case "Contest":
			if(objTGT)
			{
				if(distTGT>enemyMaxRange) { AnmStop(); EndBehavior=true; }
				else if(isInWater) { AnmStop(); EndBehavior=true; }
				else AnmBattle(rndMove,idle1,idle2,idle3,idle4,other);
			}
			else { AnmStop(); EndBehavior=true; }
			break;
			case "Battle":
			if(objTGT)
			{
				if(distTGT>enemyMaxRange) { AnmStop(); EndBehavior=true; }
				else if(!canSwim&&isInWater) { AnmStop(); EndBehavior=true; }
				else if(!canWalk&&isOnGround&&!isOnWater) { AnmStop(); EndBehavior=true; }
				else if(other&&other.behavior.Equals("ToFlee")&&!herbivorous) { AnmStop(); behavior="ToHunt"; }
				else if(other&&other.behavior.Equals("ToFlee")&&herbivorous) { AnmStop(); EndBehavior=true; }
				else if(other&&other.health==0&&!herbivorous) { AnmStop(); behavior="ToFood"; }
				else if(other&&other.health==0&&herbivorous) { AnmStop(); EndBehavior=true; }
				else AnmBattle(rndMove,idle1,idle2,idle3,idle4,other);
			}
			else { AnmStop(); EndBehavior=true; }
			break;

			case "ToHunt":
			if(objTGT)
			{
				if(other&&other.health==0) { AnmStop(); behavior="ToFood"; }
				else if(other&&distTGT<enemyMaxRange&&(other.behavior.Equals("ToHunt")|other.behavior.Equals("Battle"))) { AnmStop(); behavior="Battle"; }
				else if(!canSwim&&isInWater) { AnmStop(); EndBehavior=true; }
				else if(!canWalk&&isOnGround&&!isOnWater) { AnmStop(); EndBehavior=true; }
				else if(distTGT>actionDist*1.5f) AnmRun(rndMove);
				else behavior="Hunt";
			}
			else { AnmStop(); EndBehavior=true; }
			break;
			case "Hunt":
			if(objTGT)
			{
				if(other&&other.health==0) { AnmStop(); behavior="ToFood"; }
				else if(other&&distTGT<enemyMaxRange&&(other.behavior.Equals("Hunt")|other.behavior.Equals("Battle"))) { AnmStop(); behavior="Battle"; }
				else if(!canSwim&&isInWater) { AnmStop(); EndBehavior=true; }
				else if(!canWalk&&isOnGround&&!isOnWater) { AnmStop(); EndBehavior=true; }
				else if(distTGT<actionDist*1.5f) AnmHunt(rndMove,other);
				else behavior="ToHunt";
			}
			else { AnmStop(); EndBehavior=true; }
			break;

			default: AnmStop(); EndBehavior=true; break;
		}

		if(behaviorCount<=0) EndBehavior=true; else behaviorCount--; // Behavior counter, end if reach 0
		if(EndBehavior) { objTGT=null; posTGT=Vector3.zero; }; // End of this behavior, go to the AI entry point...
	}
	#endregion
	#region ANIMATOR SETUP
	#region TURN
	enum Vect { forward, strafe, backward, zero };
	void AnmTurn(Vect type)
	{
		switch(type)
		{
			case Vect.backward: anm.SetFloat("Turn",angTGT.eulerAngles.y+(delta>0.0f ? 180f : -180f)); break;
			case Vect.strafe: anm.SetFloat("Turn",angTGT.eulerAngles.y+(delta>0.0f ? 90 : -90)); break;
			case Vect.forward: anm.SetFloat("Turn",angTGT.eulerAngles.y+avoidAdd); break;
			case Vect.zero:
			if(delta>135|delta<-135|(canSwim&&distTGT<actionDist*0.9f)|(canWalk&&distTGT<actionDist*0.4f)) anm.SetInteger("Move",-1);
			else if(delta>45) anm.SetInteger("Move",10);
			else if(delta<-45) anm.SetInteger("Move",-10);
			anm.SetFloat("Turn",angTGT.eulerAngles.y);
			break;
		}
	}
	#endregion
	#region MOVES
	void AnmStop()
	{
		if(canAttack) anm.SetBool("Attack",false); anm.SetInteger("Move",0); anm.SetInteger("Idle",0);
	}
	void AnmWalk(int rndMove,int rndIdle,int idle1,int idle2,int idle3,int idle4)
	{
		if(canAttack) anm.SetBool("Attack",false);

		if(canFly)
		{
			if(!isOnGround) anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
			else anm.SetFloat("Pitch",0.25f);
			if(rndMove>95) { AnmIdles(rndIdle,idle1,idle2,idle3,idle4); }
			else { anm.SetInteger("Idle",0); anm.SetInteger("Move",1); }
			AnmTurn(Vect.forward);
		}
		else
		{
			if(canSwim) anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
			if(rndMove>98) AnmIdles(rndIdle,idle1,idle2,idle3,idle4);
			else if(rndMove>96) { anm.SetInteger("Move",1); anm.SetInteger("Idle",1); }
			else { anm.SetInteger("Move",1); anm.SetInteger("Idle",0); }
			AnmTurn(Vect.forward);
		}
	}
	void AnmRun(int rndMove)
	{
		if(canAttack) anm.SetBool("Attack",false);
		if(canSwim) anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
		if(canFly) anm.SetFloat("Pitch",isOnGround ? -0.75f : (Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
		if(rndMove>98) anm.SetInteger("Idle",1); else anm.SetInteger("Idle",0);
		anm.SetInteger("Move",2);
		AnmTurn(Vect.forward);
	}
	#endregion
	#region ACTIONS
	void AnmDrink(int drink)
	{
		if(canAttack) anm.SetBool("Attack",false); anm.SetInteger("Move",0);
		if(canFly&&!isOnGround) { anm.SetFloat("Pitch",0.25f); }
		else
		{
			anm.SetInteger("Idle",drink);
			water=Mathf.Clamp(water+0.025f,0.0f,100f);
		}
	}
	void AnmEat(int rndMove,int eat,int rndIdle,int idle1,int idle2,int idle3,int idle4)
	{
		if(canAttack) anm.SetBool("Attack",false); anm.SetInteger("Move",0);
		if(canSwim)
		{
			body.MovePosition(Vector3.Lerp(transform.position,posTGT+(transform.position-Head.GetChild(0).GetChild(0).position),0.01f));
			anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
		}
		if(canFly&&!isOnGround) anm.SetFloat("Pitch",0.25f);
		else if((delta<45&&delta>-45))
		{
			if(anm.GetInteger("Idle")==eat) { food=Mathf.Clamp(food+0.05f,0.0f,100f); if(water<25) water+=0.1f; }

			if(rndMove>50) anm.SetInteger("Idle",eat);
			else if(rndMove>25) anm.SetInteger("Idle",0);
			else AnmIdles(rndIdle,idle1,idle2,idle3,idle4);
		}
		else anm.SetInteger("Idle",0);
		AnmTurn(Vect.zero);
	}
	void AnmSleep(int sleep)
	{
		if(canAttack) anm.SetBool("Attack",false); 
		anm.SetInteger("Move",0);
		
		if(canFly && !isOnGround) 
		{
			anm.SetFloat("Pitch",0.25f);
		}
		else
		{
			// SOLO establecer la animación, NO regenerar aquí
			if(!OnAnm.IsName(specie+"|SitIdle"))
			{
				anm.SetInteger("Idle", sleep);
			}
		}
	}
	void AnmIdles(int rndIdle,int idle1,int idle2,int idle3,int idle4)
	{
		if(canAttack) anm.SetBool("Attack",false); anm.SetInteger("Move",0);
		switch(rndIdle)
		{
			case 0: anm.SetInteger("Idle",0); break;
			case 1: anm.SetInteger("Idle",idle1); break;
			case 2: anm.SetInteger("Idle",idle2); break;
			case 3: anm.SetInteger("Idle",idle3); break;
			case 4: anm.SetInteger("Idle",idle4); break;
		}
	}
	#endregion
	#region BATTLES
	void AnmHunt(int rndMove,Creature other)
	{
		bool aim=false; if(delta<-25|delta>25|(other&&!other.anm.GetInteger("Move").Equals(2))) aim=true;
		//Air hunt
		if(canFly)
		{
			AnmTurn(Vect.forward); anm.SetBool("OnGround",false); isOnGround=false;
			anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
			if(other)
			{
				body.velocity=other.body.velocity;
				body.MovePosition(Vector3.Lerp(transform.position,other.body.worldCenterOfMass+(transform.position-Head.GetChild(0).position)+transform.up,0.1f));
			}
			else body.MovePosition(Vector3.Lerp(transform.position,objTGT.transform.position+(transform.position-Head.GetChild(0).position)+transform.up,0.025f));

			if(rndMove<25) { anm.SetInteger("Move",1); anm.SetBool("Attack",true); anm.SetInteger("Idle",0); }
			else if(rndMove<50) { anm.SetInteger("Move",-10); anm.SetBool("Attack",true); anm.SetInteger("Idle",0); }
			else if(rndMove<75) { anm.SetInteger("Move",-10); anm.SetBool("Attack",true); anm.SetInteger("Idle",0); }
			else { anm.SetInteger("Move",-1); anm.SetBool("Attack",false); anm.SetInteger("Idle",1); }
		}
		//Terrestrial hunt
		else if(!canSwim|(canSwim&&canWalk&&!isInWater))
		{
			if(objCOL==objTGT) { anm.SetBool("Attack",true); AnmTurn(Vect.zero); }
			else if(distTGT<actionDist) { anm.SetInteger("Move",0); anm.SetBool("Attack",false); AnmTurn(Vect.zero); }
			else if(distTGT<actionDist*1.25f) { anm.SetInteger("Move",rndMove<50 ? 1 : 2); anm.SetBool("Attack",true); AnmTurn(Vect.forward); }
			else { anm.SetInteger("Move",aim ? 0 : 2); anm.SetBool("Attack",false); AnmTurn(Vect.forward); }
		}
		//Water hunt
		else
		{
			if(other) body.MovePosition(Vector3.Lerp(transform.position,other.body.worldCenterOfMass+(transform.position-Head.GetChild(0).position),0.01f));
			else body.MovePosition(Vector3.Lerp(transform.position,objTGT.transform.position+(transform.position-Head.GetChild(0).position),0.01f));
			anm.SetInteger("Idle",0);
			anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);

			if(distTGT<actionDist) { anm.SetInteger("Move",0); anm.SetBool("Attack",false); AnmTurn(Vect.zero); }
			else if(distTGT<actionDist*1.25f|objCOL==objTGT) { anm.SetInteger("Move",rndMove<50 ? 1 : 2); anm.SetBool("Attack",true); AnmTurn(Vect.forward); }
			else { anm.SetInteger("Move",aim ? 0 : 2); anm.SetBool("Attack",false); AnmTurn(Vect.forward); }
		}
	}
	void AnmBattle(int rndMove,int idle1,int idle2,int idle3,int idle4,Creature other)
	{
		//Air battles
		if(canFly)
		{
			bool aim=false; if(delta<-25|delta>25) aim=true;
			AnmTurn(Vect.forward);
			anm.SetBool("OnGround",false); isOnGround=false;
			anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);

			if(rndMove<75)
			{
				if(other) body.MovePosition(Vector3.Lerp(transform.position,other.body.worldCenterOfMass+(transform.position-Head.GetChild(0).position)+transform.up,0.025f));
				else body.MovePosition(Vector3.Lerp(transform.position,objTGT.transform.position+(transform.position-Head.GetChild(0).position)+transform.up,0.025f));
				if(objCOL==objTGT|distTGT<actionDist*1.25f) anm.SetBool("Attack",true); else anm.SetBool("Attack",false);
				if(rndMove>40) anm.SetInteger("Move",aim ? 0 : 1);
				else if(rndMove>30) anm.SetInteger("Move",aim ? 0 : -1);
				else if(rndMove>20) anm.SetInteger("Move",aim ? 0 : 10);
				else if(rndMove>10) anm.SetInteger("Move",aim ? 0 : -10);
				else anm.SetInteger("Move",0);
			}
			else if(distTGT<actionDist*5.0f)
			{
				anm.SetBool("Attack",false); anm.SetInteger("Idle",Random.Range(0,100)==0 ? 1 : 0);
				if(rndMove>95) anm.SetInteger("Move",aim ? 0 : 1);
				else if(rndMove>90) anm.SetInteger("Move",aim ? 0 : -1);
				else if(rndMove>85) anm.SetInteger("Move",aim ? 0 : 10);
				else if(rndMove>80) anm.SetInteger("Move",aim ? 0 : -10);
				else anm.SetInteger("Move",0);
			}
			else { anm.SetInteger("Move",2); anm.SetInteger("Idle",Random.Range(0,100)==0 ? 1 : 0); }
		}
		//Terrestrial battles
		else if(!canSwim|(canSwim&&canWalk&&!isInWater))
		{
			if((other&&((rndMove<75|other.rndMove<75)&&distTGT<actionDist*2.0f))|(!other&&(rndMove<75&&distTGT<actionDist*2.0f)))
			{
				anm.SetInteger("Idle",0);
				if(distTGT<actionDist) { anm.SetInteger("Move",-1); AnmTurn(Vect.forward); anm.SetBool("Attack",false); }
				else if(distTGT<actionDist*1.25f) { anm.SetInteger("Move",rndMove<25 ? 0 : 1); AnmTurn(Vect.forward); anm.SetBool("Attack",true); }
				else { anm.SetInteger("Move",rndMove<25 ? 1 : 2); AnmTurn(Vect.forward); anm.SetBool("Attack",true); }
			}
			else if(distTGT<actionDist*5.0f)
			{
				anm.SetBool("Attack",false);
				if(other&&distTGT<actionDist*2.0f&&(rndMove>50&&other.rndMove<50)) { anm.SetInteger("Move",-1); AnmTurn(Vect.forward); anm.SetInteger("Idle",Random.Range(0,10)==0 ? 1 : 0); }
				else if(distTGT<actionDist*2.0f&&rndMove>50) { anm.SetInteger("Move",-1); AnmTurn(Vect.forward); anm.SetInteger("Idle",Random.Range(0,10)==0 ? 1 : 0); }
				else if(other&&rndMove<50&&other.rndMove<50) { anm.SetInteger("Move",2); AnmTurn(Vect.forward); anm.SetInteger("Idle",Random.Range(0,10)==0 ? 1 : 0); }
				else if(!other&&rndMove<50) { anm.SetInteger("Move",2); AnmTurn(Vect.forward); anm.SetInteger("Idle",Random.Range(0,10)==0 ? 1 : 0); }
				else if(!canSwim&&rndMove<75) { anm.SetInteger("Move",1); AnmTurn(Vect.strafe); anm.SetInteger("Idle",Random.Range(0,10)==0 ? 1 : 0); }
				else { anm.SetInteger("Move",0); AnmTurn(Vect.forward); AnmIdles(rndIdle,idle1,idle2,idle3,idle4); }
			}
			else { anm.SetInteger("Move",2); anm.SetBool("Attack",false); AnmTurn(Vect.forward); anm.SetInteger("Idle",Random.Range(0,10)==0 ? 1 : 0); }
		}
		//Water battles 
		else if(canSwim)
		{
			anm.SetFloat("Pitch",(Vector3.Angle(Vector3.up,(body.worldCenterOfMass-posTGT).normalized)-90f)/-90f);
			if(Mathf.Abs(delta)<25)
			{
				AnmTurn(Vect.forward);
				if(distTGT<actionDist*2.0f) { anm.SetInteger("Move",rndMove<50 ? 0 : 1); anm.SetBool("Attack",true); }
				else if(distTGT<actionDist*3.0f) { anm.SetInteger("Move",2); anm.SetBool("Attack",true); }
				else { anm.SetInteger("Move",2); anm.SetBool("Attack",false); }
			}
			else
			{
				if(rndMove<33) { AnmTurn(Vect.strafe); anm.SetInteger("Move",1); }
				else if(rndMove<66) { AnmTurn(Vect.forward); anm.SetInteger("Move",2); }
				else { AnmTurn(Vect.zero); }
			}
		}
	}
	
	#region INTEREST MANAGEMENT
    /// <summary>
    /// Actualiza el grupo de interés basado en la posición actual
    /// </summary>
void UpdateInterestGroup()
{
    if(!PhotonNetwork.InRoom || photonView == null) return;
    
    byte newGroup = CalculateInterestGroup(transform.position);
    
    if(newGroup != currentInterestGroup || currentInterestGroup == 0)
    {
        // PASO 1: CALCULAR grupos nuevos primero
        byte[] groupsToSubscribe = GetAdjacentGroups(newGroup);
        
        // PASO 2: SUSCRIBIRSE PRIMERO (sin desconectarse aún)
        foreach(byte group in groupsToSubscribe)
        {
            PhotonNetwork.SetInterestGroups(group, true);
        }
        
        // PASO 3: Asignar grupo al PhotonView
        photonView.Group = newGroup;
        
        // PASO 4: AHORA SÍ desuscribirse del anterior (después de suscribirse al nuevo)
        if(currentInterestGroup != 0)
        {
            // Solo desuscribirse de grupos que ya no están en la lista nueva
            byte[] oldGroups = GetAdjacentGroups(currentInterestGroup);
            foreach(byte oldGroup in oldGroups)
            {
                // Si el grupo viejo NO está en los nuevos, desuscribirse
                if(System.Array.IndexOf(groupsToSubscribe, oldGroup) < 0)
                {
                    PhotonNetwork.SetInterestGroups(oldGroup, false);
                }
            }
        }
        
        currentInterestGroup = newGroup;
        
        Debug.Log($"[Interest] {gameObject.name} cambió a grupo {newGroup}. Escuchando {groupsToSubscribe.Length} grupos");
    }
}
    /// <summary>
    /// Calcula el grupo de interés basado en posición
    /// </summary>
    byte CalculateInterestGroup(Vector3 position)
    {
        // Convertir posición a coordenadas de grid
        int gridX = Mathf.FloorToInt(position.x / gridCellSize);
        int gridZ = Mathf.FloorToInt(position.z / gridCellSize);
        
        // Convertir a byte (0-255)
        // Normalizar para evitar valores negativos
        int normalizedX = (gridX + 128) % 16; // 16x16 grid = 256 celdas
        int normalizedZ = (gridZ + 128) % 16;
        
        byte group = (byte)(normalizedZ * 16 + normalizedX + 1); // +1 porque 0 es especial en Photon
        
        return group;
    }

    /// <summary>
    /// Obtiene grupos adyacentes según el radio configurado
    /// </summary>
    byte[] GetAdjacentGroups(byte centerGroup)
    {
        List<byte> groups = new List<byte>();
        
        // Extraer coordenadas del grupo central
        int centerIndex = centerGroup - 1; // Restar el +1 que añadimos
        int centerX = centerIndex % 16;
        int centerZ = centerIndex / 16;
        
        // Calcular grupos en el radio especificado
        for(int z = -adjacentRadius; z <= adjacentRadius; z++)
        {
            for(int x = -adjacentRadius; x <= adjacentRadius; x++)
            {
                int newX = centerX + x;
                int newZ = centerZ + z;
                
                // Verificar límites (16x16 grid)
                if(newX >= 0 && newX < 16 && newZ >= 0 && newZ < 16)
                {
                    byte adjacentGroup = (byte)(newZ * 16 + newX + 1);
                    groups.Add(adjacentGroup);
                }
            }
        }
        
        return groups.ToArray();
    }

    /// <summary>
    /// Debug: Mostrar información de Interest Groups
    /// </summary>
    [ContextMenu("Debug Interest Groups")]
    void DebugInterestGroups()
    {
        if(!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[Interest] No estás en una sala");
            return;
        }
        
        Debug.Log($"=== INTEREST GROUPS DEBUG ===");
        Debug.Log($"Grupo actual: {currentInterestGroup}");
        Debug.Log($"PhotonView.Group: {photonView.Group}");
        Debug.Log($"Posición: {transform.position}");
        Debug.Log($"Grid Cell: ({Mathf.FloorToInt(transform.position.x / gridCellSize)}, {Mathf.FloorToInt(transform.position.z / gridCellSize)})");
        
        byte[] adjacent = GetAdjacentGroups(currentInterestGroup);
        Debug.Log($"Grupos suscritos: {string.Join(", ", adjacent)}");
        
        // Contar criaturas por grupo
        Dictionary<byte, int> creaturesPerGroup = new Dictionary<byte, int>();
        foreach(Creature c in allCreatures)
        {
            if(c != null && c.photonView != null)
            {
                byte group = c.photonView.Group;
                if(!creaturesPerGroup.ContainsKey(group))
                    creaturesPerGroup[group] = 0;
                creaturesPerGroup[group]++;
            }
        }
        
        Debug.Log($"Distribución de criaturas:");
        foreach(var kvp in creaturesPerGroup)
        {
            Debug.Log($"  Grupo {kvp.Key}: {kvp.Value} criaturas");
        }
        Debug.Log($"============================");
    }

    /// <summary>
    /// Calcular ahorro real de tráfico
    /// </summary>
    [ContextMenu("Calcular Ahorro de Tráfico")]
    void CalculateTrafficSavings()
    {
        int totalCreatures = allCreatures.Count;
        int visibleCreatures = 0;
        
        foreach(Creature c in allCreatures)
        {
            if(c != null && c != this)
            {
                byte[] myGroups = GetAdjacentGroups(currentInterestGroup);
                if(System.Array.IndexOf(myGroups, c.photonView.Group) >= 0)
                    visibleCreatures++;
            }
        }
        
        float reduction = (1f - (float)visibleCreatures / (totalCreatures - 1)) * 100f;
        
        Debug.Log($"=== AHORRO DE TRÁFICO ===");
        Debug.Log($"Criaturas totales: {totalCreatures}");
        Debug.Log($"Criaturas visibles: {visibleCreatures}");
        Debug.Log($"Reducción de tráfico: {reduction:F1}%");
        Debug.Log($"=========================");
    }
	
	#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if(!Application.isPlaying || photonView == null || !photonView.IsMine) return;
        
        // Dibujar grid
        Vector3 pos = transform.position;
        int gridX = Mathf.FloorToInt(pos.x / gridCellSize);
        int gridZ = Mathf.FloorToInt(pos.z / gridCellSize);
        
        // Celda actual (verde)
        Vector3 cellCenter = new Vector3(
            gridX * gridCellSize + gridCellSize * 0.5f,
            pos.y,
            gridZ * gridCellSize + gridCellSize * 0.5f
        );
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(cellCenter, new Vector3(gridCellSize, 10f, gridCellSize));
        
        // Celdas adyacentes (amarillo)
        Gizmos.color = Color.yellow;
        for(int z = -adjacentRadius; z <= adjacentRadius; z++)
        {
            for(int x = -adjacentRadius; x <= adjacentRadius; x++)
            {
                if(x == 0 && z == 0) continue; // Skip centro
                
                Vector3 adjCenter = new Vector3(
                    (gridX + x) * gridCellSize + gridCellSize * 0.5f,
                    pos.y,
                    (gridZ + z) * gridCellSize + gridCellSize * 0.5f
                );
                
                Gizmos.DrawWireCube(adjCenter, new Vector3(gridCellSize, 10f, gridCellSize));
            }
        }
    }
    #endif
	#endregion
	#endregion
	#endregion
	#endregion
}