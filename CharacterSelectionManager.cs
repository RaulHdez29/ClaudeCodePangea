using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gestiona la selección de personajes en la escena de selección
/// Permite elegir entre diferentes dinosaurios antes de entrar al juego
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Lista de Personajes Disponibles")]
    [Tooltip("Arrastra aquí todos los PlayerSelectionData (ScriptableObjects) disponibles")]
    public List<PlayerSelectionData> availableCharacters = new List<PlayerSelectionData>();

    [Header("UI - Referencias")]
    [Tooltip("Panel principal de selección de personajes")]
    public GameObject selectionPanel;

    [Tooltip("Panel de información del personaje seleccionado")]
    public GameObject characterInfoPanel;

    [Tooltip("Texto del nombre del personaje")]
    public Text characterNameText;

    [Tooltip("Texto de la descripción del personaje")]
    public Text characterDescriptionText;

    [Tooltip("Imagen del personaje seleccionado")]
    public Image characterIconImage;

    [Tooltip("Texto de stats del personaje")]
    public Text characterStatsText;

    [Header("UI - Botones de Personajes")]
    [Tooltip("Prefab de botón para cada personaje (debe tener un componente Button e Image)")]
    public GameObject characterButtonPrefab;

    [Tooltip("Contenedor donde se crearán los botones (GridLayoutGroup recomendado)")]
    public Transform characterButtonsContainer;

    [Header("UI - Botón de Confirmación")]
    [Tooltip("Botón para confirmar la selección y continuar")]
    public Button confirmButton;

    [Tooltip("Texto del botón de confirmación")]
    public Text confirmButtonText;

    [Header("Configuración")]
    [Tooltip("Color del botón seleccionado")]
    public Color selectedColor = Color.green;

    [Tooltip("Color normal de los botones")]
    public Color normalColor = Color.white;

    // Variables internas
    private PlayerSelectionData selectedCharacter;
    private List<Button> characterButtons = new List<Button>();
    private int selectedIndex = -1;

    void Start()
    {
        // Crear botones para cada personaje
        CreateCharacterButtons();

        // Configurar botón de confirmación
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
            confirmButton.interactable = false; // Desactivado hasta que se seleccione un personaje
        }

        // Ocultar panel de información al inicio
        if (characterInfoPanel != null)
        {
            characterInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Crea botones UI para cada personaje disponible
    /// </summary>
    void CreateCharacterButtons()
    {
        if (characterButtonPrefab == null || characterButtonsContainer == null)
        {
            Debug.LogError("⚠️ Falta asignar CharacterButtonPrefab o CharacterButtonsContainer");
            return;
        }

        // Limpiar botones existentes
        foreach (Transform child in characterButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        characterButtons.Clear();

        // Crear un botón por cada personaje
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            PlayerSelectionData character = availableCharacters[i];
            if (character == null) continue;

            // Instanciar botón
            GameObject buttonObj = Instantiate(characterButtonPrefab, characterButtonsContainer);
            Button button = buttonObj.GetComponent<Button>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (button != null)
            {
                // Configurar icono del personaje
                if (buttonImage != null && character.characterIcon != null)
                {
                    buttonImage.sprite = character.characterIcon;
                }

                // Configurar nombre del botón (opcional)
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = character.characterName;
                }

                // Agregar listener para seleccionar este personaje
                int index = i; // Capturar índice para el closure
                button.onClick.AddListener(() => SelectCharacter(index));

                characterButtons.Add(button);
            }
        }

        Debug.Log($"✅ Creados {characterButtons.Count} botones de selección");
    }

    /// <summary>
    /// Selecciona un personaje y actualiza la UI
    /// </summary>
    void SelectCharacter(int index)
    {
        if (index < 0 || index >= availableCharacters.Count)
        {
            Debug.LogError($"⚠️ Índice inválido: {index}");
            return;
        }

        selectedIndex = index;
        selectedCharacter = availableCharacters[index];

        Debug.Log($"🎮 Personaje seleccionado: {selectedCharacter.characterName}");

        // Actualizar colores de botones
        UpdateButtonColors();

        // Actualizar panel de información
        UpdateCharacterInfo();

        // Activar botón de confirmación
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }

        // Mostrar panel de información
        if (characterInfoPanel != null)
        {
            characterInfoPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Actualiza los colores de los botones (resaltar el seleccionado)
    /// </summary>
    void UpdateButtonColors()
    {
        for (int i = 0; i < characterButtons.Count; i++)
        {
            Button btn = characterButtons[i];
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = (i == selectedIndex) ? selectedColor : normalColor;
                }
            }
        }
    }

    /// <summary>
    /// Actualiza el panel de información con los datos del personaje seleccionado
    /// </summary>
    void UpdateCharacterInfo()
    {
        if (selectedCharacter == null) return;

        // Actualizar nombre
        if (characterNameText != null)
        {
            characterNameText.text = selectedCharacter.characterName;
        }

        // Actualizar descripción
        if (characterDescriptionText != null)
        {
            characterDescriptionText.text = selectedCharacter.description;
        }

        // Actualizar icono
        if (characterIconImage != null && selectedCharacter.characterIcon != null)
        {
            characterIconImage.sprite = selectedCharacter.characterIcon;
        }

        // Actualizar stats
        if (characterStatsText != null)
        {
            characterStatsText.text = $"Velocidad: {selectedCharacter.speed:F1}\n" +
                                     $"Vida: {selectedCharacter.health:F0}\n" +
                                     $"Daño: {selectedCharacter.attackDamage:F0}";
        }
    }

    /// <summary>
    /// Confirma la selección y guarda el personaje elegido
    /// </summary>
    void ConfirmSelection()
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("⚠️ No hay personaje seleccionado");
            return;
        }

        Debug.Log($"✅ Confirmado: {selectedCharacter.characterName}");
        Debug.Log($"📂 Prefab Resource Path: {selectedCharacter.prefabResourcePath}");

        // Guardar selección en PlayerPrefs para usar después
        PlayerPrefs.SetString("SelectedCharacterPrefab", selectedCharacter.prefabResourcePath);
        PlayerPrefs.SetString("SelectedCharacterName", selectedCharacter.characterName);
        PlayerPrefs.Save();

        // Ocultar panel de selección
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        // IMPORTANTE: No cargar escena aquí, el GameNetworkManager lo hará después de conectar
        Debug.Log("🌐 Personaje guardado. Esperando conexión al servidor...");
    }

    /// <summary>
    /// Método público para obtener el personaje seleccionado
    /// </summary>
    public PlayerSelectionData GetSelectedCharacter()
    {
        return selectedCharacter;
    }

    /// <summary>
    /// Método público para verificar si hay un personaje seleccionado
    /// </summary>
    public bool HasSelectedCharacter()
    {
        return selectedCharacter != null;
    }

    /// <summary>
    /// Método público para resetear la selección
    /// </summary>
    public void ResetSelection()
    {
        selectedIndex = -1;
        selectedCharacter = null;

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        if (characterInfoPanel != null)
        {
            characterInfoPanel.SetActive(false);
        }

        UpdateButtonColors();
    }
}
