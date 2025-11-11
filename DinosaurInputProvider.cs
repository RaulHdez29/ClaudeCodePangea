using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

/// <summary>
/// Provider de Input para Photon Fusion
/// ✅ Captura input del jugador local
/// ✅ Envía input al servidor de Fusion
/// ✅ Optimizado para minimizar latencia
/// </summary>
public class DinosaurInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Referencias")]
    public VariableJoystick movementJoystick;
    public Button runButton;
    public Button crouchButton;
    public Button jumpButton;
    public Button attackButton;
    public Button eatButton;
    public Button drinkButton;

    // Estados de botones (para detectar presión)
    private bool runButtonPressed = false;
    private bool crouchButtonPressed = false;
    private bool jumpButtonPressed = false;
    private bool attackButtonPressed = false;
    private bool eatButtonPressed = false;
    private bool drinkButtonPressed = false;

    void Start()
    {
        // Configurar listeners de botones
        if (runButton != null)
        {
            runButton.onClick.AddListener(() => runButtonPressed = true);
        }

        if (crouchButton != null)
        {
            crouchButton.onClick.AddListener(() => crouchButtonPressed = true);
        }

        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(() => jumpButtonPressed = true);
        }

        if (attackButton != null)
        {
            attackButton.onClick.AddListener(() => attackButtonPressed = true);
        }

        if (eatButton != null)
        {
            eatButton.onClick.AddListener(() => eatButtonPressed = true);
        }

        if (drinkButton != null)
        {
            drinkButton.onClick.AddListener(() => drinkButtonPressed = true);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 🎮 INPUT POLLING (Llamado por Fusion)
    // ═══════════════════════════════════════════════════════════

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        // Capturar input del joystick
        if (movementJoystick != null)
        {
            data.movementInput = new Vector3(
                movementJoystick.Horizontal,
                0,
                movementJoystick.Vertical
            );
        }
        else
        {
            // Fallback: teclado
            data.movementInput = new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")
            );
        }

        // Capturar botones (solo enviar si fueron presionados)
        data.runButton = runButtonPressed;
        data.crouchButton = crouchButtonPressed;
        data.jumpButton = jumpButtonPressed || Input.GetKeyDown(KeyCode.Space);
        data.attackButton = attackButtonPressed || Input.GetMouseButtonDown(0);
        data.eatButton = eatButtonPressed;
        data.drinkButton = drinkButtonPressed;

        // Resetear flags de botones
        runButtonPressed = false;
        crouchButtonPressed = false;
        jumpButtonPressed = false;
        attackButtonPressed = false;
        eatButtonPressed = false;
        drinkButtonPressed = false;

        // Enviar input a Fusion
        input.Set(data);
    }

    // ═══════════════════════════════════════════════════════════
    // 🌐 CALLBACKS DE FUSION (Implementación mínima requerida)
    // ═══════════════════════════════════════════════════════════

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Input faltante, no hacer nada
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"🔌 Fusion shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("✅ Conectado al servidor Fusion");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"❌ Desconectado del servidor: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Aceptar todas las conexiones
        request.Accept();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"❌ Conexión fallida: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // No usado
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // No usado
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // No usado
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("🔄 Host migration iniciada");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // No usado
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // No usado
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("✅ Escena cargada");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("⏳ Cargando escena...");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Jugador {player.PlayerId} se unió");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Jugador {player.PlayerId} se desconectó");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // No usado
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // No usado
    }
}
