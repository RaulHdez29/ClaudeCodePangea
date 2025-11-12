# 🎮 GUÍA DE CONFIGURACIÓN - Sistema de Selección y Multijugador

Esta guía te enseñará cómo configurar el sistema completo de selección de personajes y multijugador con Photon PUN2.

---

## 📋 REQUISITOS PREVIOS

1. ✅ Photon PUN2 instalado en Unity
2. ✅ Cuenta de Photon y App ID configurado
3. ✅ Scripts adaptados a Photon PUN2 (SimpleDinosaurController, HealthSystem, etc.)

---

## 🏗️ PASO 1: PREPARAR LOS PREFABS

### 1.1 Configurar el prefab del dinosaurio

1. **Agregar componentes de Photon** a tu prefab:
   - `PhotonView`
   - `PhotonTransformView`
   - `PhotonAnimatorView`

2. **Configurar PhotonView**:
   - Owner: `Takeover`
   - Observed Components:
     - PhotonTransformView
     - PhotonAnimatorView
     - SimpleDinosaurController

3. **Mover el prefab a Resources**:
   ```
   Assets/Resources/DinosaurPlayer.prefab
   ```
   **IMPORTANTE**: El nombre debe coincidir con `prefabResourcePath` en PlayerSelectionData

---

## 🗂️ PASO 2: CREAR DATOS DE PERSONAJES (ScriptableObjects)

1. **Crear PlayerSelectionData**:
   - En Unity: `Assets → Create → Pangea → Player Selection Data`
   - Nombrar: `VelociraptorData`, `TRexData`, etc.

2. **Configurar cada ScriptableObject**:
   ```
   Character Name: Velociraptor
   Description: Rápido y ágil carnívoro
   Character Icon: [Arrastra sprite del personaje]
   Character Prefab: [Arrastra el prefab]
   Prefab Resource Path: "DinosaurPlayer" (DEBE coincidir con el nombre en Resources)
   Speed: 5
   Health: 200
   Attack Damage: 25
   ```

3. **Repetir para cada personaje disponible**

---

## 🎨 PASO 3: CREAR ESCENA DE SELECCIÓN

### 3.1 Crear nueva escena

1. `File → New Scene`
2. Guardar como: `CharacterSelection`
3. Agregar a Build Settings (índice 0)

### 3.2 Crear UI de Selección

#### Panel Principal
```
Canvas
└── SelectionPanel (Panel)
    ├── Title (Text) - "Selecciona tu Dinosaurio"
    ├── CharacterButtonsContainer (Empty GameObject + GridLayoutGroup)
    │   └── [Los botones se crearán automáticamente]
    └── CharacterInfoPanel (Panel)
        ├── CharacterIcon (Image)
        ├── CharacterName (Text)
        ├── CharacterDescription (Text)
        └── CharacterStats (Text)
```

#### Panel de Servidor
```
Canvas
└── ServerSelectionPanel (Panel)
    ├── Title (Text) - "Conectar al Servidor"
    ├── RoomNameInput (InputField) - "Nombre de Sala"
    ├── HostButton (Button) - "Crear Servidor"
    └── JoinButton (Button) - "Unirse a Servidor"
```

#### Panel de Estado
```
Canvas
└── StatusPanel (Panel)
    └── StatusText (Text) - "Conectando..."
```

### 3.3 Crear Prefab de Botón de Personaje

1. Crear botón UI
2. Estructura:
   ```
   CharacterButton (Button + Image)
   └── CharacterName (Text)
   ```
3. Guardar como prefab: `CharacterButtonPrefab`

---

## 🔧 PASO 4: CONFIGURAR MANAGERS EN LA ESCENA DE SELECCIÓN

### 4.1 CharacterSelectionManager

1. **Crear GameObject vacío**: `CharacterSelectionManager`
2. **Agregar script**: `CharacterSelectionManager.cs`
3. **Configurar**:
   ```
   Available Characters: [Arrastra todos los ScriptableObjects]
   Selection Panel: [SelectionPanel]
   Character Info Panel: [CharacterInfoPanel]
   Character Name Text: [CharacterName]
   Character Description Text: [CharacterDescription]
   Character Icon Image: [CharacterIcon]
   Character Stats Text: [CharacterStats]
   Character Button Prefab: [CharacterButtonPrefab]
   Character Buttons Container: [CharacterButtonsContainer]
   Confirm Button: [ConfirmButton]
   Confirm Button Text: [Text del botón]
   Selected Color: Verde
   Normal Color: Blanco
   ```

### 4.2 GameNetworkManager

1. **Crear GameObject vacío**: `GameNetworkManager`
2. **Agregar script**: `GameNetworkManager.cs`
3. **Configurar**:
   ```
   Game Version: "1.0"
   Preferred Region: "" (vacío para auto)
   Default Room Name: "PangeaRoom"
   Max Players Per Room: 4
   Server Selection Panel: [ServerSelectionPanel]
   Host Button: [HostButton]
   Join Button: [JoinButton]
   Room Name Input: [RoomNameInput]
   Status Panel: [StatusPanel]
   Status Text: [StatusText]
   Game Scene Name: "GameMap"
   Character Selection Manager: [CharacterSelectionManager GameObject]
   ```

---

## 🗺️ PASO 5: CONFIGURAR ESCENA DEL MAPA

### 5.1 Crear escena del juego

1. `File → New Scene`
2. Guardar como: `GameMap`
3. Agregar a Build Settings (índice 1)

### 5.2 Configurar PlayerSpawner

1. **Crear GameObject vacío**: `PlayerSpawner`
2. **Agregar script**: `PlayerSpawner.cs`
3. **Configurar**:
   ```
   Spawn Points: [Arrastra GameObjects vacíos como spawn points]
   Use Random Spawn: true (si no hay spawn points)
   Random Spawn Radius: 20
   Spawn Height: 1
   Default Prefab Path: "DinosaurPlayer"
   Show Debug Logs: true
   ```

### 5.3 Crear Spawn Points (Opcional)

1. Crear GameObjects vacíos en el mapa
2. Nombrarlos: `SpawnPoint1`, `SpawnPoint2`, etc.
3. Arrastrarlos al array de Spawn Points en PlayerSpawner

---

## 📝 PASO 6: CONFIGURAR BUILD SETTINGS

1. `File → Build Settings`
2. Agregar escenas en orden:
   - **0**: CharacterSelection
   - **1**: GameMap

---

## 🎯 PASO 7: CONFIGURAR PHOTON

### 7.1 Photon App Settings

1. `Window → Photon Unity Networking → Highlight Server Settings`
2. Verificar:
   - App Id Realtime: [Tu App ID de Photon]
   - Fixed Region: (vacío para auto)
   - Protocol: UDP

### 7.2 Verificar Resources

1. Verificar que todos los prefabs de dinosaurios estén en:
   ```
   Assets/Resources/
   ```
2. Los nombres deben coincidir EXACTAMENTE con `prefabResourcePath`

---

## ✅ PASO 8: PROBAR EL SISTEMA

### Prueba Local (Build)

1. **Build del juego**:
   - `File → Build Settings → Build`
   - Crear 2 copias

2. **Ejecutar primera copia (HOST)**:
   - Seleccionar personaje
   - Confirmar
   - Crear Servidor
   - Esperar carga del mapa

3. **Ejecutar segunda copia (CLIENT)**:
   - Seleccionar personaje
   - Confirmar
   - Unirse a Servidor (usar mismo nombre de sala)
   - Esperar carga del mapa

4. **Verificar**:
   - ✅ Ambos jugadores aparecen en el mapa
   - ✅ Se ven las animaciones de ambos
   - ✅ Los ataques funcionan entre jugadores
   - ✅ La vida baja al recibir daño

---

## 🐛 TROUBLESHOOTING

### Problema: "Prefab not found"
**Solución**: Verifica que el prefab esté en `Assets/Resources/` y que el nombre coincida exactamente con `prefabResourcePath`

### Problema: "Cannot instantiate object"
**Solución**: Asegúrate de que el prefab tenga `PhotonView` configurado correctamente

### Problema: No se conecta a Photon
**Solución**:
- Verifica el App ID en Photon Settings
- Verifica tu conexión a internet
- Revisa la región configurada

### Problema: Jugadores no se ven
**Solución**: Verifica que `PhotonTransformView` y `PhotonAnimatorView` estén en Observed Components

### Problema: UI aparece en jugadores remotos
**Solución**: Verifica que los scripts tengan las validaciones `if (photonView.IsMine)` correctamente

---

## 📊 FLUJO DEL SISTEMA

```
1. CharacterSelection Scene
   ↓
2. Usuario selecciona personaje
   ↓
3. Usuario hace clic en "Confirmar"
   ↓
4. CharacterSelectionManager guarda selección en PlayerPrefs
   ↓
5. Usuario selecciona "Crear Servidor" o "Unirse"
   ↓
6. GameNetworkManager conecta a Photon
   ↓
7. GameNetworkManager crea/une a sala
   ↓
8. GameNetworkManager carga GameMap scene
   ↓
9. PlayerSpawner lee PlayerPrefs
   ↓
10. PlayerSpawner spawnea el personaje seleccionado
    ↓
11. ¡Jugador en el mapa!
```

---

## 🎨 PERSONALIZACIÓN

### Cambiar máximo de jugadores
```csharp
// En GameNetworkManager
maxPlayersPerRoom = 8; // Cambiar a lo que necesites
```

### Cambiar región de Photon
```csharp
// En GameNetworkManager
preferredRegion = "us"; // us, eu, asia, etc.
```

### Agregar más personajes
1. Crear nuevo prefab en Resources
2. Crear nuevo PlayerSelectionData ScriptableObject
3. Agregarlo al array de CharacterSelectionManager

---

## 📖 SCRIPTS CREADOS

1. **PlayerSelectionData.cs** - ScriptableObject con datos de personajes
2. **CharacterSelectionManager.cs** - Maneja UI de selección
3. **GameNetworkManager.cs** - Maneja conexión a Photon
4. **PlayerSpawner.cs** - Spawnea jugadores en el mapa

---

¡Listo! Ahora tienes un sistema completo de selección de personajes y multijugador funcionando. 🎉
