using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestor de UI del jugador - Conecta la UI con el dinosaurio spawneado
/// Este script debe estar en el Canvas UI del jugador
/// </summary>
public class PlayerUIManager : MonoBehaviour
{
    [Header("🎮 Referencias del Jugador")]
    [Tooltip("GameObject del dinosaurio controlado (se asigna automáticamente)")]
    public GameObject playerDinosaur;

    [Header("🕹️ Joysticks (Asignación Automática)")]
    [Tooltip("Joystick de movimiento")]
    public Joystick movementJoystick;

    [Tooltip("Joystick de ataque")]
    public Joystick attackJoystick;

    [Header("🔘 Botones (Asignación Automática)")]
    [Tooltip("Botón de correr")]
    public Button runButton;

    [Tooltip("Botón de saltar")]
    public Button jumpButton;

    [Tooltip("Botón de agacharse")]
    public Button crouchButton;

    [Tooltip("Botón de comer")]
    public Button eatButton;

    [Tooltip("Botón de beber")]
    public Button drinkButton;

    [Tooltip("Botón de dormir")]
    public Button sleepButton;

    [Tooltip("Botón de llamar/rugir")]
    public Button callButton;

    [Header("📊 UI de Estadísticas")]
    [Tooltip("Barra de vida")]
    public Slider healthBar;

    [Tooltip("Barra de hambre")]
    public Slider hungerBar;

    [Tooltip("Barra de sed")]
    public Slider thirstBar;

    [Tooltip("Barra de estamina")]
    public Slider staminaBar;

    [Tooltip("Texto de nombre del jugador")]
    public TMP_Text playerNameText;

    // Referencias a sistemas del dinosaurio
    private SimpleDinosaurController dinosaurController;
    private HealthSystem healthSystem;
    private CallSystem callSystem;
    private DinosaurSleepSystem sleepSystem;

    void Awake()
    {
        // Asegurar que el Canvas persista entre escenas
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Configurar UI para un dinosaurio específico
    /// </summary>
    public void SetupUI(GameObject dinosaur)
    {
        playerDinosaur = dinosaur;

        // Obtener componentes del dinosaurio
        dinosaurController = dinosaur.GetComponent<SimpleDinosaurController>();
        healthSystem = dinosaur.GetComponent<HealthSystem>();
        callSystem = dinosaur.GetComponent<CallSystem>();
        sleepSystem = dinosaur.GetComponent<DinosaurSleepSystem>();

        // Vincular joysticks
        if (dinosaurController != null)
        {
            if (movementJoystick != null)
            {
                dinosaurController.movementJoystick = movementJoystick;
            }

            if (attackJoystick != null)
            {
                dinosaurController.attackJoystick = attackJoystick;
            }
        }

        // Vincular botones
        ConnectButtons();

        // Vincular barras de estadísticas
        ConnectStatBars();

        Debug.Log($"✅ UI vinculada con dinosaurio: {dinosaur.name}");
    }

    /// <summary>
    /// Conectar botones con sistemas del dinosaurio
    /// </summary>
    void ConnectButtons()
    {
        if (dinosaurController != null)
        {
            // Botón de correr
            if (runButton != null)
            {
                dinosaurController.runButton = runButton;
            }

            // Botón de saltar
            if (jumpButton != null)
            {
                dinosaurController.jumpButton = jumpButton;
            }

            // Botón de agacharse
            if (crouchButton != null)
            {
                dinosaurController.crouchButton = crouchButton;
            }

            // Botón de comer
            if (eatButton != null)
            {
                dinosaurController.eatButton = eatButton;
            }

            // Botón de beber
            if (drinkButton != null)
            {
                dinosaurController.drinkButton = drinkButton;
            }
        }

        // Botón de dormir
        if (sleepSystem != null && sleepButton != null)
        {
            sleepSystem.sleepButton = sleepButton;
        }

        // Botón de llamar
        if (callSystem != null && callButton != null)
        {
            callSystem.callPanelToggleButton = callButton;
        }
    }

    /// <summary>
    /// Conectar barras de estadísticas con sistemas del dinosaurio
    /// </summary>
    void ConnectStatBars()
    {
        if (dinosaurController != null)
        {
            // Barras de estadísticas
            if (healthBar != null)
            {
                dinosaurController.healthBar = healthBar;
            }

            if (hungerBar != null)
            {
                dinosaurController.hungerBar = hungerBar;
            }

            if (thirstBar != null)
            {
                dinosaurController.thirstBar = thirstBar;
            }

            if (staminaBar != null)
            {
                dinosaurController.staminaBar = staminaBar;
            }
        }
    }

    void Update()
    {
        // Actualizar nombre del jugador si está disponible
        if (playerNameText != null && playerDinosaur != null)
        {
            // Puedes obtener el nombre desde un componente personalizado
            // playerNameText.text = playerDinosaur.GetComponent<PlayerInfo>()?.playerName ?? "Player";
        }

        // Verificar si el dinosaurio todavía existe
        if (playerDinosaur == null)
        {
            // El dinosaurio fue destruido, ocultar UI o mostrar mensaje
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Desconectar UI (al cambiar de dinosaurio o salir)
    /// </summary>
    public void DisconnectUI()
    {
        playerDinosaur = null;
        dinosaurController = null;
        healthSystem = null;
        callSystem = null;
        sleepSystem = null;

        Debug.Log("UI desconectada del dinosaurio");
    }

    /// <summary>
    /// Actualizar nombre del jugador
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }
}
