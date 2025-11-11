# 🦖 Sistema de Dinosaurios para Photon Fusion

## 📋 Descripción

Sistema completo de dinosaurios multiplayer optimizado para **Photon Fusion**, con combate PvP, sincronización de animaciones, sistema de hambre/sed/estamina y sueño.

## ✨ Características

- ✅ **Combate PvP**: Los jugadores pueden atacarse entre sí
- ✅ **Sincronización optimizada**: Solo ~20 bytes por tick
- ✅ **Animaciones sincronizadas**: Idle, caminar, correr, nadar, atacar, dormir, etc.
- ✅ **Sistema de hambre/sed/estamina**: Completamente sincronizado
- ✅ **Sistema de sueño**: Visible para todos los jugadores
- ✅ **Sistema de llamados**: Rugidos sincronizados con audio
- ✅ **Bajo tráfico de red**: Optimizado para conexiones lentas

## 📦 Scripts Incluidos

### Scripts Principales

1. **NetworkDinosaurController.cs** - Controlador principal del dinosaurio
2. **NetworkHealthSystem.cs** - Sistema de vida y daño en red
3. **NetworkCallSystem.cs** - Sistema de rugidos/llamados
4. **NetworkSleepSystem.cs** - Sistema de sueño
5. **DinosaurInputProvider.cs** - Provider de input para Fusion

### Migración desde Scripts Locales

| Script Local | Script de Red | Cambios Principales |
|---|---|---|
| `SimpleDinosaurController.cs` | `NetworkDinosaurController.cs` | NetworkBehaviour, variables [Networked] |
| `HealthSystem.cs` | `NetworkHealthSystem.cs` | RPCs para daño, sincronización de vida |
| `CallSystem.cs` | `NetworkCallSystem.cs` | RPCs para animaciones/audio |
| `DinosaurSleepSystem.cs` | `NetworkSleepSystem.cs` | Estado sincronizado |

## 🚀 Instalación

### 1. Instalar Photon Fusion

1. Abre Unity
2. Ve a **Window > Package Manager**
3. Agrega Photon Fusion desde el Unity Asset Store o Package Manager
4. Importa Photon Fusion

### 2. Copiar Scripts

Copia todos los scripts de red a tu carpeta `Assets/Scripts/Network/`:

```
Assets/
  Scripts/
    Network/
      NetworkDinosaurController.cs
      NetworkHealthSystem.cs
      NetworkCallSystem.cs
      NetworkSleepSystem.cs
      DinosaurInputProvider.cs
```

### 3. Configurar el Prefab del Dinosaurio

1. **Reemplazar Scripts Locales por Scripts de Red:**
   - Elimina `SimpleDinosaurController` → Agrega `NetworkDinosaurController`
   - Elimina `HealthSystem` → Agrega `NetworkHealthSystem`
   - Elimina `CallSystem` → Agrega `NetworkCallSystem`
   - Elimina `DinosaurSleepSystem` → Agrega `NetworkSleepSystem`

2. **Agregar Componentes de Fusion:**
   ```
   GameObject Dinosaurio
   ├─ NetworkObject (Fusion)
   ├─ NetworkTransform (Fusion) - Para sincronizar posición/rotación
   ├─ NetworkDinosaurController
   ├─ NetworkHealthSystem
   ├─ NetworkCallSystem
   ├─ NetworkSleepSystem
   ├─ CharacterController
   └─ Animator
   ```

3. **Configurar NetworkObject:**
   - **Network Object Id**: Auto-generado
   - **Allow State Authority Override**: ✅ Activado
   - **Object Interest**: Always

4. **Configurar NetworkTransform:**
   - **Interpolation Target**: Transform
   - **Interpolate Error Correction**: ✅ Activado
   - **Teleport Enabled**: ✅ Activado

### 4. Configurar el GameManager/NetworkRunner

Crea un script `NetworkGameManager.cs` para manejar la conexión:

```csharp
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;

public class NetworkGameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runnerPrefab;
    public NetworkObject playerPrefab;

    private NetworkRunner _runner;

    async void Start()
    {
        await StartGame(GameMode.AutoHostOrClient);
    }

    async Task StartGame(GameMode mode)
    {
        _runner = Instantiate(runnerPrefab);
        _runner.ProvideInput = true;

        // Agregar DinosaurInputProvider
        _runner.gameObject.AddComponent<DinosaurInputProvider>();

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "DinosaurWorld",
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Spawn del jugador
            Vector3 spawnPosition = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            Debug.Log($"Jugador {player.PlayerId} spawneado en {spawnPosition}");
        }
    }

    // Implementar otros callbacks requeridos...
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    // ... etc
}
```

## ⚔️ Sistema de Combate PvP

### Cómo Funciona

1. **Detección de Enemigos:**
   - Usa `Physics.OverlapSphere` con el `enemyLayer`
   - Verifica ángulo de ataque (`attackAngle`)
   - Verifica distancia (`attackRange`)

2. **Aplicar Daño:**
   ```csharp
   // En NetworkDinosaurController
   NetworkHealthSystem targetHealth = hit.GetComponent<NetworkHealthSystem>();
   if (targetHealth != null && targetHealth.IsAlive())
   {
       targetHealth.TakeDamage(attackDamage, Object.InputAuthority);
   }
   ```

3. **Sincronización:**
   - El daño se aplica en el servidor via RPC
   - Efectos visuales/sonoros se replican en todos los clientes
   - La vida se sincroniza automáticamente con `[Networked]`

### Configuración de Layers

Asegúrate de configurar correctamente los layers:

1. Ve a **Edit > Project Settings > Tags and Layers**
2. Agrega un layer llamado `Player`
3. En el prefab del dinosaurio, asigna el layer `Player`
4. En `NetworkDinosaurController`, configura `enemyLayer` = `Player`

## 📊 Optimizaciones de Red

### Tráfico de Red Reducido

| Componente | Datos por Tick | Optimización |
|---|---|---|
| MovementState | 1 byte | Estado comprimido (0-8) |
| NormalizedSpeed | 1 byte | Velocidad 0-255 |
| StateFlags | 1 byte | 8 bools en 1 byte |
| Hambre/Sed/Estamina | 12 bytes | 3 floats |
| NetworkTransform | ~20 bytes | Posición/rotación comprimida |
| **TOTAL** | **~35 bytes/tick** | **30 ticks/seg = ~1 KB/s** |

### Comparación con Sincronización "Normal"

| Método | Bytes/Tick | Tráfico/Segundo |
|---|---|---|
| **Optimizado (Este)** | 35 bytes | ~1 KB/s |
| Sin Optimizar | 200+ bytes | ~6 KB/s |
| **Ahorro** | **82%** | **83%** |

### Técnicas de Optimización Usadas

1. **Compresión de Estados:**
   - 8 bools → 1 byte (StateFlags)
   - Velocidad float → byte (0-255)
   - Estado de movimiento → byte (0-8)

2. **Sincronización Selectiva:**
   - Solo se sincroniza lo esencial en FixedUpdateNetwork
   - Animaciones se calculan localmente basadas en estado
   - UI solo se actualiza para el jugador local

3. **RPCs Eficientes:**
   - Solo se usan para eventos (ataque, rugido, dormir)
   - Parámetros mínimos (1 byte cuando es posible)
   - No se envían RPCs cada frame

4. **Predicción del Cliente:**
   - Movimiento se predice localmente
   - Corrección de errores con interpolación
   - Reduce "jittering" visual

## 🎮 Uso en Juego

### Controles

| Acción | Control |
|---|---|
| Mover | Joystick / WASD |
| Correr | Botón Run |
| Agacharse | Botón Crouch |
| Saltar | Botón Jump / Espacio |
| Atacar | Botón Attack / Click Izquierdo |
| Comer | Botón Eat (cerca de comida) |
| Beber | Botón Drink (cerca de agua) |
| Dormir | Botón Sleep |
| Rugir | Botón Call |

### Mecánicas

1. **Hambre/Sed:**
   - Se reduce automáticamente con el tiempo
   - Causa daño si llega a 0
   - Come/bebe para recuperar

2. **Estamina:**
   - Se consume al correr
   - Se regenera al caminar/estar quieto
   - Se regenera más rápido al dormir

3. **Combate:**
   - Cooldown de 0.5 segundos entre ataques
   - Daño solo si el enemigo está en rango y ángulo
   - Visible para todos los jugadores

4. **Sueño:**
   - Solo se puede dormir si está completamente quieto
   - No se puede dormir en agua
   - Regenera estamina y vida (si tiene hambre/sed)

## 🐛 Solución de Problemas

### El jugador no se mueve

- ✅ Verifica que `DinosaurInputProvider` esté en el `NetworkRunner`
- ✅ Verifica que `Object.HasInputAuthority` sea true
- ✅ Verifica que el CharacterController esté habilitado

### Las animaciones no se sincronizan

- ✅ Verifica que el Animator esté asignado en todos los scripts
- ✅ Verifica que los parámetros del Animator coincidan con el código
- ✅ Las animaciones se basan en estado local, no se sincronizan directamente

### El combate no funciona

- ✅ Verifica que `enemyLayer` esté configurado correctamente
- ✅ Verifica que ambos jugadores tengan `NetworkHealthSystem`
- ✅ Verifica que el `attackPoint` esté asignado

### Lag o tráfico alto

- ✅ Reduce `Runner.SimulationConfig.TickRate` a 30 (default)
- ✅ Usa NetworkTransform con interpolación
- ✅ Reduce `enemyLayer` para evitar demasiados OverlapSphere

## 📚 Documentación Adicional

- [Photon Fusion Documentation](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro)
- [Fusion API Reference](https://doc-api.photonengine.com/en/fusion/current/index.html)
- [Fusion Best Practices](https://doc.photonengine.com/fusion/current/manual/optimization/network-optimization)

## 🎯 Próximos Pasos

1. **Testing Multiplayer:**
   - Prueba con 2-4 jugadores simultáneos
   - Mide el tráfico de red real
   - Ajusta tick rate según necesidad

2. **Añadir Más Features:**
   - Sistema de inventario sincronizado
   - Chat de texto/voz
   - Sistema de clanes/grupos
   - Spawn de NPCs sincronizados

3. **Optimización Avanzada:**
   - Areas of Interest (AOI) para grandes mundos
   - Lag compensation para combate
   - State synchronization customizada

## 📝 Notas Importantes

- **Estado de Autoridad**: Solo el servidor (`HasStateAuthority`) puede modificar variables `[Networked]`
- **Input Authority**: Solo el dueño (`HasInputAuthority`) puede enviar input
- **RPCs**: Úsalos con moderación, solo para eventos importantes
- **Tick Rate**: 30 ticks/segundo es óptimo para balance latencia/tráfico

## 🤝 Contribuciones

Si encuentras bugs o tienes sugerencias, por favor reporta en el repositorio.

---

**¡Feliz desarrollo multiplayer! 🦖🎮**
