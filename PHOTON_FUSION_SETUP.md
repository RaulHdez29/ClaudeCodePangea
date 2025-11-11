# 🌐 Configuración de Photon Fusion para Sistema de Dinosaurios

## ✅ Scripts Adaptados

Todos los scripts han sido adaptados para funcionar con **Photon Fusion**:

1. **SimpleDinosaurController.cs** - Controlador principal con sincronización de red
2. **HealthSystem.cs** - Sistema de salud (local pero con RPCs)
3. **CallSystem.cs** - Sistema de llamados/rugidos sincronizados
4. **DinosaurSleepSystem.cs** - Sistema de sueño (solo local)

---

## 🎮 Características de Red

### ✅ Sincronizado (visible para todos los jugadores)
- **Movimiento y rotación** (posición/rotación del dinosaurio)
- **Animaciones** (idle, walk, run, swim, attack, death, etc.)
- **Ataques** (animación y daño)
- **Llamados/rugidos** (animación y sonido)
- **Estado de muerte** (animación de muerte)

### ❌ NO Sincronizado (solo visible para el jugador local)
- **Hambre** (barra de hambre)
- **Sed** (barra de sed)
- **Estamina** (barra de estamina)
- **Vida** (barra de vida, pero el daño se recibe por RPC)
- **Sueño** (estado de dormir/despertar)
- **UI local** (botones, paneles, etc.)

---

## 🛠️ Configuración Requerida en Unity

### 1. Agregar NetworkObject al Prefab del Dinosaurio

```
GameObject (Dinosaurio)
├── NetworkObject (componente)
├── SimpleDinosaurController (NetworkBehaviour)
├── HealthSystem (NetworkBehaviour)
├── CallSystem (NetworkBehaviour)
├── DinosaurSleepSystem (MonoBehaviour - local)
└── CharacterController
```

### 2. Configurar NetworkTransform

Agrega el componente **NetworkTransform** para sincronizar posición/rotación:

```
NetworkTransform:
- Synchronize Position: ✅
- Synchronize Rotation: ✅
- Interpolation Target: Transform
- Space: World
```

### 3. Configurar NetworkRigidbody (si usas física)

Si tu dinosaurio usa Rigidbody, agrega **NetworkRigidbody**:

```
NetworkRigidbody:
- Synchronize Position: ✅
- Synchronize Rotation: ✅
- Interpolation: Interpolate
```

### 4. Input Authority

El script detecta automáticamente si el jugador tiene autoridad sobre el dinosaurio usando:

```csharp
if (HasInputAuthority)
{
    // Solo el propietario ejecuta esta lógica
}
```

---

## ⚙️ Optimizaciones de Red

### 1. Sincronización de Animaciones Optimizada

Las animaciones se sincronizan solo cuando hay cambios significativos (umbral de 0.01):

```csharp
if (Mathf.Abs(NetworkSpeed - currentSpeed / runSpeed) > 0.01f)
{
    NetworkSpeed = currentSpeed / runSpeed;
}
```

### 2. RPCs para Eventos Puntuales

Los eventos puntuales (ataques, rugidos, muerte) usan RPCs en lugar de sincronización continua:

```csharp
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
void RPC_TriggerAttackAnimation() { ... }
```

### 3. Variables de Red Compactas

Se usa un `byte` (0-255) para el estado de animación en lugar de sincronizar múltiples booleanos:

```csharp
[Networked] public byte CurrentAnimationState { get; set; }
```

---

## 🎯 Sistema de Combate en Red

### Flujo de Ataque

1. **Jugador A** presiona botón de ataque
2. **SimpleDinosaurController** detecta enemigos en rango
3. Se envía **RPC_ApplyDamage** al jugador B
4. **HealthSystem** de jugador B recibe daño (local)
5. **RPC_TriggerAttackAnimation** sincroniza animación para todos

### Código del Ataque

```csharp
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
void RPC_ApplyDamage(NetworkObject target, float damage, Vector3 knockbackDirection)
{
    HealthSystem healthSystem = target.GetComponent<HealthSystem>();
    if (healthSystem != null)
    {
        healthSystem.TakeDamage(damage); // Daño local
    }
}
```

---

## 📊 Variables de Red

### SimpleDinosaurController

| Variable | Tipo | Descripción |
|----------|------|-------------|
| `IsAttackingNet` | NetworkBool | Estado de ataque |
| `IsDeadNet` | NetworkBool | Estado de muerte |
| `IsCallingNet` | NetworkBool | Estado de rugido |
| `NetworkSpeed` | float | Velocidad normalizada |
| `NetworkMoveX` | float | Dirección X |
| `NetworkMoveZ` | float | Dirección Z |
| `NetworkTurn` | float | Rotación de cámara |
| `NetworkLook` | float | Mirada vertical |
| `IsRunningNet` | NetworkBool | Estado de correr |
| `IsSwimmingNet` | NetworkBool | Estado de natación |
| `CurrentAnimationState` | byte | Estado de animación (0-255) |

---

## 🐛 Solución de Problemas

### Problema: Las animaciones no se sincronizan

**Solución:**
1. Verifica que el `NetworkObject` esté en el GameObject raíz
2. Asegúrate de que el dinosaurio tenga `HasInputAuthority` activo
3. Revisa que `NetworkTransform` esté configurado correctamente

### Problema: El daño no se aplica a otros jugadores

**Solución:**
1. Verifica que ambos dinosaurios tengan `NetworkObject`
2. Asegúrate de que el layer `enemyLayer` incluya a los jugadores
3. Revisa que el `HealthSystem` esté agregado al mismo GameObject

### Problema: Los rugidos no se escuchan

**Solución:**
1. Verifica que `CallSystem` tenga `AudioSource` asignado
2. Asegúrate de que los `AudioClip[]` tengan sonidos configurados
3. Revisa que el volumen del `AudioSource` no esté en 0

---

## 📝 Notas Importantes

1. **Hambre/Sed/Estamina** son solo locales. Cada jugador gestiona sus propias estadísticas.
2. **Vida** es local pero el daño se envía por RPC, así que todos pueden atacar a todos.
3. **Sueño** es completamente local. Los otros jugadores NO ven si estás durmiendo.
4. **Posición/Rotación** se sincronizan automáticamente con `NetworkTransform`.
5. **Animaciones** se sincronizan de forma optimizada (solo cuando cambian).

---

## 🚀 Próximos Pasos

1. Importa **Photon Fusion SDK** en Unity
2. Configura tu App ID de Photon
3. Agrega `NetworkObject` a tu prefab de dinosaurio
4. Agrega `NetworkTransform` para sincronizar posición
5. Configura el `enemyLayer` para incluir a los jugadores
6. ¡Prueba el combate en red!

---

## 📚 Referencias

- [Photon Fusion Documentation](https://doc.photonengine.com/fusion/current)
- [NetworkBehaviour API](https://doc.photonengine.com/fusion/current/manual/network-behaviour)
- [RPCs en Fusion](https://doc.photonengine.com/fusion/current/manual/rpc)
- [NetworkTransform Guide](https://doc.photonengine.com/fusion/current/manual/network-transform)

---

## ✨ Características Especiales

### Optimización de Tráfico de Red

- Solo se sincronizan cambios significativos (umbral de 0.01)
- Estados de animación comprimidos en 1 byte
- RPCs solo para eventos puntuales (no bucles)
- Variables locales para estadísticas personales

### Interpolación Suave

- Los clientes remotos interpolan valores de red suavemente
- Transiciones de animación suaves entre estados
- Movimiento fluido sin saltos bruscos

---

¡Listo para combate multijugador! 🦖⚔️🦕
