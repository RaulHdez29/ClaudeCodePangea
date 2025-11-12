using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selector simple de personajes
/// Agrega prefabs directamente y selecciona uno antes de conectar al servidor
/// </summary>
public class SimpleCharacterSelector : MonoBehaviour
{
    [Header("Prefabs de Personajes")]
    [Tooltip("Lista de prefabs de personajes (deben estar en Resources folder con el mismo nombre)")]
    public GameObject[] characterPrefabs;

    [Header("Nombres de Personajes (opcional)")]
    [Tooltip("Nombres para mostrar en cada botón (si está vacío, usa el nombre del prefab)")]
    public string[] characterNames;

    [Header("UI - Botones")]
    [Tooltip("Asigna botones manualmente para cada personaje (mismo orden que los prefabs)")]
    public Button[] characterButtons;

    [Tooltip("Panel de selección de personajes")]
    public GameObject selectionPanel;

    [Tooltip("Panel de servidores (se activa después de seleccionar)")]
    public GameObject serverPanel;

    [Header("Feedback Visual")]
    [Tooltip("Texto que muestra el personaje seleccionado")]
    public Text selectedCharacterText;

    [Tooltip("Color del botón seleccionado")]
    public Color selectedColor = Color.green;

    [Tooltip("Color normal de los botones")]
    public Color normalColor = Color.white;

    // Variable interna
    private int selectedIndex = -1;

    void Start()
    {
        // Configurar listeners de botones
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // Capturar índice para el closure
            if (characterButtons[i] != null)
            {
                characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
            }
        }

        // Ocultar panel de servidores al inicio
        if (serverPanel != null)
        {
            serverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Selecciona un personaje
    /// </summary>
    public void SelectCharacter(int index)
    {
        if (index < 0 || index >= characterPrefabs.Length || characterPrefabs[index] == null)
        {
            Debug.LogError($"⚠️ Índice inválido o prefab nulo: {index}");
            return;
        }

        selectedIndex = index;
        Debug.Log($"🎮 Personaje seleccionado: {GetCharacterName(index)}");

        // Actualizar colores de botones
        UpdateButtonColors();

        // Actualizar texto
        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = $"Seleccionado: {GetCharacterName(index)}";
        }
    }

    /// <summary>
    /// Confirma la selección y muestra el panel de servidores
    /// </summary>
    public void ConfirmSelection()
    {
        if (selectedIndex == -1)
        {
            Debug.LogWarning("⚠️ Debes seleccionar un personaje primero");
            return;
        }

        // Guardar el nombre del prefab en PlayerPrefs
        string prefabName = characterPrefabs[selectedIndex].name;
        PlayerPrefs.SetString("SelectedCharacter", prefabName);
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.Save();

        Debug.Log($"✅ Confirmado: {prefabName}");

        // Ocultar panel de selección y mostrar panel de servidores
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        if (serverPanel != null)
        {
            serverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Obtiene el nombre del personaje (usa array de nombres o nombre del prefab)
    /// </summary>
    string GetCharacterName(int index)
    {
        if (characterNames != null && index < characterNames.Length && !string.IsNullOrEmpty(characterNames[index]))
        {
            return characterNames[index];
        }

        return characterPrefabs[index].name;
    }

    /// <summary>
    /// Actualiza los colores de los botones
    /// </summary>
    void UpdateButtonColors()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] != null)
            {
                ColorBlock colors = characterButtons[i].colors;
                colors.normalColor = (i == selectedIndex) ? selectedColor : normalColor;
                characterButtons[i].colors = colors;
            }
        }
    }

    /// <summary>
    /// Método público para resetear la selección
    /// </summary>
    public void ResetSelection()
    {
        selectedIndex = -1;
        UpdateButtonColors();

        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = "Ninguno";
        }
    }
}
