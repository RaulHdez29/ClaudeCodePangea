# 🎮 CONFIGURACIÓN SIMPLE - 3 Scripts

Sistema simplificado de selección de personajes y multijugador.

---

## 📦 ARCHIVOS

1. **SimpleCharacterSelector.cs** - Selección de personajes
2. **SimpleServerManager.cs** - Conexión a servidor
3. **SimplePlayerSpawner.cs** - Spawn en el mapa

---

## 🚀 CONFIGURACIÓN RÁPIDA

### PASO 1: Preparar Prefabs

1. **Mover prefabs a Resources**:
   ```
   Assets/Resources/Velociraptor.prefab
   Assets/Resources/TRex.prefab
   Assets/Resources/Triceratops.prefab
   ```

2. **Cada prefab debe tener**:
   - PhotonView
   - PhotonTransformView
   - PhotonAnimatorView
   - Scripts adaptados (SimpleDinosaurController, etc.)

---

### PASO 2: Escena de Selección

1. **Crear escena**: `CharacterSelection`

2. **Crear UI básica**:
   ```
   Canvas
   ├── SelectionPanel
   │   ├── Title (Text) - "Selecciona tu Dinosaurio"
   │   ├── Button_Velociraptor (Button) - "Velociraptor"
   │   ├── Button_TRex (Button) - "T-Rex"
   │   ├── Button_Triceratops (Button) - "Triceratops"
   │   ├── SelectedText (Text) - "Seleccionado: Ninguno"
   │   └── ConfirmButton (Button) - "Continuar"
   └── ServerPanel (desactivado al inicio)
       ├── Title (Text) - "Conectar al Servidor"
       ├── RoomNameInput (InputField) - "Nombre de Sala"
       ├── ConnectButton (Button) - "Conectar"
       └── StatusText (Text) - "Estado: ..."
   ```

3. **Agregar scripts**:

   **SimpleCharacterSelector**:
   ```
   Character Prefabs: [Arrastra Velociraptor, TRex, Triceratops]
   Character Names: ["Velociraptor", "T-Rex", "Triceratops"] (opcional)
   Character Buttons: [Arrastra Button_Velociraptor, Button_TRex, Button_Triceratops]
   Selection Panel: [SelectionPanel]
   Server Panel: [ServerPanel]
   Selected Character Text: [SelectedText]
   Selected Color: Verde
   Normal Color: Blanco
   ```

   **SimpleServerManager**:
   ```
   Game Version: "1.0"
   Room Name: "PangeaRoom"
   Max Players: 4
   Map Scene Name: "GameMap"
   Room Name Input: [RoomNameInput]
   Status Text: [StatusText]
   Server Panel: [ServerPanel]
   ```

4. **Configurar botones**:
   - Botones de personajes: Ya están configurados por SimpleCharacterSelector
   - ConfirmButton → SimpleCharacterSelector.ConfirmSelection()
   - ConnectButton → SimpleServerManager.ConnectToServer()

---

### PASO 3: Escena del Mapa

1. **Crear escena**: `GameMap`

2. **Crear GameObject vacío**: `PlayerSpawner`

3. **Agregar SimplePlayerSpawner**:
   ```
   Character Prefabs: [Arrastra Velociraptor, TRex, Triceratops] (mismo orden que selector)
   Spawn Points: [Arrastra GameObjects vacíos como spawn points] (opcional)
   Random Spawn Radius: 10
   Spawn Height: 1
   ```

4. **Crear spawn points** (opcional):
   - Crear GameObjects vacíos en el mapa
   - Nombrarlos: `SpawnPoint1`, `SpawnPoint2`, etc.
   - Arrastrarlos al array de Spawn Points

---

### PASO 4: Build Settings

```
File → Build Settings → Scenes In Build:
0. CharacterSelection
1. GameMap
```

---

## ✅ FLUJO DEL SISTEMA

```
1. CharacterSelection Scene
   ↓
2. Hago clic en un botón de dinosaurio
   ↓
3. Hago clic en "Continuar"
   ↓
4. Se oculta SelectionPanel, aparece ServerPanel
   ↓
5. Hago clic en "Conectar"
   ↓
6. Se conecta a Photon y crea/une a sala
   ↓
7. Carga GameMap scene
   ↓
8. SimplePlayerSpawner lee selección
   ↓
9. Spawnea el dinosaurio
   ↓
10. ¡Listo para jugar!
```

---

## 🎯 EJEMPLO DE USO

### Agregar un nuevo personaje:

1. **Crear prefab en Resources**:
   ```
   Assets/Resources/Spinosaurus.prefab
   ```

2. **En CharacterSelection scene**:
   - Agregar nuevo botón en SelectionPanel
   - En SimpleCharacterSelector:
     - Agregar Spinosaurus a Character Prefabs (al final)
     - Agregar "Spinosaurus" a Character Names
     - Agregar el nuevo botón a Character Buttons

3. **En GameMap scene**:
   - En SimplePlayerSpawner:
     - Agregar Spinosaurus a Character Prefabs (mismo orden)

4. **¡Listo!**

---

## 🐛 TROUBLESHOOTING

| Problema | Solución |
|----------|----------|
| No spawnea | Verifica que el prefab esté en Resources/ |
| Error "Prefab not found" | El nombre debe coincidir exactamente |
| No conecta | Verifica App ID en Photon Settings |
| No carga el mapa | Verifica que GameMap esté en Build Settings |
| Botones no funcionan | Verifica que estén asignados en el Inspector |

---

## 📝 NOTAS IMPORTANTES

1. **Los prefabs deben estar en `Assets/Resources/`** - Photon solo puede instanciar desde ahí
2. **El orden de los prefabs debe ser el mismo** en SimpleCharacterSelector y SimplePlayerSpawner
3. **El nombre del prefab debe coincidir** con el nombre del archivo en Resources
4. **Los prefabs deben tener PhotonView** configurado correctamente

---

¡Configuración completa! Sistema super simple con solo 3 scripts. 🎉
