# 🎮 Guía de Configuración del Lobby y Spawn

## 📋 Tabla de Contenidos

1. [Scripts Creados](#scripts-creados)
2. [Configuración de Escenas](#configuración-de-escenas)
3. [Configuración del Lobby](#configuración-del-lobby)
4. [Configuración del NetworkGameManager](#configuración-del-networkgamemanager)
5. [Configuración de Prefabs](#configuración-de-prefabs)
6. [Configuración de UI Canvas](#configuración-de-ui-canvas)
7. [Flujo de Juego](#flujo-de-juego)
8. [Solución de Problemas](#solución-de-problemas)

---

## 📦 Scripts Creados

Los siguientes scripts han sido creados para el sistema de lobby y spawn:

1. **PlayerData.cs** - Estructura de datos para jugadores
2. **NetworkGameManager.cs** - Gestor de red y spawn
3. **LobbyManager.cs** - Gestor del lobby y selección de jugadores
4. **PlayerUIManager.cs** - Gestor de UI del jugador

---

## 🗺️ Configuración de Escenas

### Paso 1: Crear Escenas

Crea dos escenas en tu proyecto:

1. **Lobby** (Escena del lobby)
   - Archivo: `Assets/Scenes/Lobby.unity`
   - Aquí se seleccionan los jugadores y se crea la sesión

2. **GameMap** (Escena del juego)
   - Archivo: `Assets/Scenes/GameMap.unity`
   - El mapa donde se juega

### Paso 2: Agregar Escenas a Build Settings

1. Abre **File > Build Settings**
2. Arrastra ambas escenas a la lista
3. Asegúrate de que **Lobby** esté en el índice 0
4. **GameMap** debe estar en el índice 1

```
Build Settings:
0. Lobby
1. GameMap
```

---

## 🎮 Configuración del Lobby

### Paso 1: Crear GameObject del Lobby

En la escena **Lobby**, crea la siguiente jerarquía:

```
Lobby (GameObject vacío)
├── NetworkGameManager
├── LobbyManager
└── Canvas (UI del Lobby)
    ├── LobbyPanel
    │   ├── SessionNameInput (TMP_InputField)
    │   ├── CreateSessionButton (Button)
    │   ├── JoinSessionButton (Button)
    │   └── PlayerSlots
    │       ├── PlayerSlot1
    │       │   ├── ActiveToggle (Toggle)
    │       │   ├── NameInput (TMP_InputField)
    │       │   ├── PrefabDropdown (TMP_Dropdown)
    │       │   └── UIDropdown (TMP_Dropdown)
    │       ├── PlayerSlot2
    │       ├── PlayerSlot3
    │       └── PlayerSlot4
    └── ConnectedPanel (desactivado por defecto)
        ├── ConnectionStatusText (TMP_Text)
        └── StartGameButton (Button)
```

### Paso 2: Configurar NetworkGameManager

1. Selecciona el GameObject **NetworkGameManager**
2. Agrega el componente **NetworkGameManager**
3. Configura los parámetros:

```
NetworkGameManager:
- Session Name: "DinosaurGame"
- Game Mode: Shared
- Max Players: 4
- Lobby Scene Name: "Lobby"
- Game Scene Name: "GameMap"
- Spawn Points: (arrastra aquí los puntos de spawn del mapa)
- Random Spawn: ✅
```

### Paso 3: Configurar LobbyManager

1. Selecciona el GameObject **LobbyManager**
2. Agrega el componente **LobbyManager**
3. Configura los parámetros:

#### 🎮 Configuración de Jugadores

```
Player Slots: Size = 4
```

#### 🖼️ UI - Slots de Jugadores

Arrastra los GameObjects correspondientes:

```
Player Slot Panels:
- Element 0: PlayerSlot1
- Element 1: PlayerSlot2
- Element 2: PlayerSlot3
- Element 3: PlayerSlot4

Player Active Toggles:
- Element 0: PlayerSlot1/ActiveToggle
- Element 1: PlayerSlot2/ActiveToggle
- Element 2: PlayerSlot3/ActiveToggle
- Element 3: PlayerSlot4/ActiveToggle

Player Name Inputs:
- Element 0: PlayerSlot1/NameInput
- Element 1: PlayerSlot2/NameInput
- Element 2: PlayerSlot3/NameInput
- Element 3: PlayerSlot4/NameInput

Prefab Dropdowns:
- Element 0: PlayerSlot1/PrefabDropdown
- Element 1: PlayerSlot2/PrefabDropdown
- Element 2: PlayerSlot3/PrefabDropdown
- Element 3: PlayerSlot4/PrefabDropdown

UI Dropdowns:
- Element 0: PlayerSlot1/UIDropdown
- Element 1: PlayerSlot2/UIDropdown
- Element 2: PlayerSlot3/UIDropdown
- Element 3: PlayerSlot4/UIDropdown
```

#### 📦 Prefabs Disponibles

Arrastra tus prefabs de dinosaurios:

```
Available Dinosaur Prefabs: Size = (número de prefabs)
- Element 0: TRex_Prefab
- Element 1: Raptor_Prefab
- Element 2: Triceratops_Prefab
- etc.
```

#### 🖼️ UI Disponibles

Arrastra tus Canvas UI:

```
Available UI Canvases: Size = (número de UIs)
- Element 0: TRex_UI_Canvas
- Element 1: Raptor_UI_Canvas
- Element 2: Triceratops_UI_Canvas
- etc.
```

#### 🌐 UI - Botones de Red

```
Create Session Button: CreateSessionButton
Join Session Button: JoinSessionButton
Start Game Button: StartGameButton
Session Name Input: SessionNameInput
```

#### 📊 UI - Información

```
Connection Status Text: ConnectionStatusText
Lobby Panel: LobbyPanel
Connected Panel: ConnectedPanel
```

---

## 🦖 Configuración de Prefabs

Cada prefab de dinosaurio debe tener la siguiente estructura:

```
DinosaurPrefab
├── NetworkObject (componente)
├── NetworkTransform (componente)
├── SimpleDinosaurController (NetworkBehaviour)
├── HealthSystem (NetworkBehaviour)
├── CallSystem (NetworkBehaviour)
├── DinosaurSleepSystem (NetworkBehaviour)
├── CharacterController
└── Animator
```

### Configurar NetworkObject

```
NetworkObject:
- Allow State Authority Override: ✅
- Destroy When State Authority Leaves: ✅
```

### Importante: NO incluir Joysticks ni UI en el Prefab

Los joysticks y botones se asignarán automáticamente desde el Canvas UI del jugador.

---

## 🖼️ Configuración de UI Canvas

Cada Canvas UI debe tener:

```
PlayerUI_Canvas
├── PlayerUIManager (componente)
├── MovementJoystick (Joystick)
├── AttackJoystick (Joystick)
├── Buttons
│   ├── RunButton
│   ├── JumpButton
│   ├── CrouchButton
│   ├── EatButton
│   ├── DrinkButton
│   ├── SleepButton
│   └── CallButton
└── Stats
    ├── HealthBar (Slider)
    ├── HungerBar (Slider)
    ├── ThirstBar (Slider)
    └── StaminaBar (Slider)
```

### Configurar PlayerUIManager

```
PlayerUIManager:
- Movement Joystick: MovementJoystick
- Attack Joystick: AttackJoystick
- Run Button: RunButton
- Jump Button: JumpButton
- Crouch Button: CrouchButton
- Eat Button: EatButton
- Drink Button: DrinkButton
- Sleep Button: SleepButton
- Call Button: CallButton
- Health Bar: HealthBar
- Hunger Bar: HungerBar
- Thirst Bar: ThirstBar
- Stamina Bar: StaminaBar
```

### Importante: Convertir el Canvas a Prefab

1. Arrastra el Canvas a la carpeta **Assets/Prefabs/**
2. Elimina la instancia de la escena del lobby
3. Los Canvas se instanciarán automáticamente al spawnear

---

## 🗺️ Configuración del Mapa de Juego

### Paso 1: Crear Puntos de Spawn

En la escena **GameMap**, crea puntos de spawn:

```
SpawnPoints (GameObject vacío)
├── SpawnPoint1 (Transform)
├── SpawnPoint2 (Transform)
├── SpawnPoint3 (Transform)
└── SpawnPoint4 (Transform)
```

Coloca cada `SpawnPoint` en diferentes ubicaciones del mapa.

### Paso 2: Vincular con NetworkGameManager

Vuelve a la escena **Lobby** y configura:

```
NetworkGameManager:
- Spawn Points: Size = 4
  - Element 0: SpawnPoint1
  - Element 1: SpawnPoint2
  - Element 2: SpawnPoint3
  - Element 3: SpawnPoint4
```

**Nota:** Los puntos de spawn deben existir en la escena GameMap, pero se referencian desde el Lobby.

---

## 🎮 Flujo de Juego

### 1. Lobby

1. El jugador abre la escena **Lobby**
2. Selecciona qué slots de jugador activar (Toggle)
3. Para cada jugador activo:
   - Escribe un nombre (TMP_InputField)
   - Selecciona un prefab de dinosaurio (TMP_Dropdown)
   - Selecciona un Canvas UI (TMP_Dropdown)
4. Escribe el nombre de la sesión
5. Presiona **Create Session** (Host) o **Join Session** (Cliente)

### 2. Conexión

1. El NetworkGameManager crea/une a la sesión
2. Se muestra el panel **ConnectedPanel**
3. El botón **Start Game** se activa
4. El host puede presionar **Start Game** para cargar el mapa

### 3. Spawn en el Mapa

1. Se carga la escena **GameMap**
2. Para cada jugador conectado:
   - Se spawnea el prefab de dinosaurio seleccionado
   - Se instancia el Canvas UI seleccionado
   - El **PlayerUIManager** vincula automáticamente:
     - Joysticks al **SimpleDinosaurController**
     - Botones a los sistemas correspondientes
     - Barras de estadísticas

### 4. Juego

1. Cada jugador controla su dinosaurio con la UI asignada
2. Las animaciones se sincronizan por red
3. Los ataques funcionan entre jugadores
4. Los rugidos y sueño se ven por todos

---

## 🐛 Solución de Problemas

### Problema: El NetworkGameManager no se encuentra

**Solución:**
1. Asegúrate de que el GameObject tenga el script **NetworkGameManager**
2. Verifica que esté marcado como **DontDestroyOnLoad**
3. Debe existir en la escena del lobby antes de conectar

### Problema: Los jugadores no se spawnean

**Solución:**
1. Verifica que los prefabs tengan **NetworkObject**
2. Asegúrate de que los prefabs estén en **Assets/Prefabs/**
3. Los prefabs deben estar registrados en Fusion (Photon Fusion > Prefab Settings)

### Problema: La UI no se conecta al dinosaurio

**Solución:**
1. Verifica que el Canvas tenga el componente **PlayerUIManager**
2. Asegúrate de que los joysticks y botones estén asignados
3. El Canvas debe ser un prefab, no una instancia de escena

### Problema: Error "Scene not in Build Settings"

**Solución:**
1. Abre **File > Build Settings**
2. Arrastra **Lobby** y **GameMap** a la lista
3. Asegúrate de que los nombres coincidan exactamente

### Problema: Los spawn points no funcionan

**Solución:**
1. Los spawn points deben existir en la escena **GameMap**
2. Deben ser GameObjects con Transform
3. Deben estar referenciados en el NetworkGameManager de la escena Lobby

---

## ✅ Checklist de Configuración

Antes de probar, verifica:

- [ ] Ambas escenas (Lobby y GameMap) están en Build Settings
- [ ] NetworkGameManager existe en la escena Lobby
- [ ] LobbyManager está configurado con todos los dropdowns y botones
- [ ] Los prefabs de dinosaurios tienen NetworkObject
- [ ] Los prefabs de dinosaurios están registrados en Fusion
- [ ] Los Canvas UI tienen PlayerUIManager
- [ ] Los Canvas UI son prefabs (no instancias de escena)
- [ ] Los puntos de spawn existen en GameMap
- [ ] Los puntos de spawn están referenciados en NetworkGameManager

---

## 🚀 Ejemplo de Uso

### Configuración Rápida

1. **Escena Lobby:**
   ```
   - NetworkGameManager (GameObject)
   - LobbyManager (GameObject)
   - Canvas UI del Lobby
   ```

2. **Escena GameMap:**
   ```
   - Terreno/Mapa
   - SpawnPoints (4 puntos)
   - Cámara
   ```

3. **Prefabs:**
   ```
   Assets/Prefabs/
   ├── Dinosaurs/
   │   ├── TRex_Prefab.prefab
   │   └── Raptor_Prefab.prefab
   └── UI/
       ├── TRex_UI_Canvas.prefab
       └── Raptor_UI_Canvas.prefab
   ```

4. **Probar:**
   - Ejecuta desde la escena **Lobby**
   - Configura jugadores
   - Presiona **Create Session**
   - Presiona **Start Game**
   - ¡Los jugadores deberían spawnearse en el mapa!

---

## 📚 Referencias

- [Photon Fusion Documentation](https://doc.photonengine.com/fusion/current)
- [NetworkObject](https://doc.photonengine.com/fusion/current/manual/network-object)
- [Spawning Objects](https://doc.photonengine.com/fusion/current/manual/spawning)
- [Scene Management](https://doc.photonengine.com/fusion/current/manual/scenes)

---

## 💡 Consejos Avanzados

### Spawn Automático

Para spawn automático sin lobby, puedes:

1. Crear un script **AutoSpawner.cs**
2. Llamar a `NetworkGameManager.Instance.StartHost()` en `Start()`
3. Configurar jugadores por defecto

### Múltiples UI por Jugador

Si quieres diferentes UIs para diferentes dinosaurios:

1. Crea un Canvas UI por cada tipo de dinosaurio
2. Agrégalos a `availableUICanvases` en el LobbyManager
3. El dropdown permitirá seleccionar la UI específica

### Spawn en Posiciones Personalizadas

Para posiciones específicas por jugador:

1. Activa `useCustomSpawnPosition` en PlayerData
2. Configura `spawnPosition` manualmente
3. Cada jugador spawneará en su posición específica

---

¡Sistema de lobby completo y funcional! 🎮🦖
