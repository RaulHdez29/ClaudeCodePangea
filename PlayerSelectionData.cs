using UnityEngine;

/// <summary>
/// ScriptableObject que almacena la información de cada personaje/dinosaurio seleccionable
/// Crear en: Assets → Create → Pangea → Player Selection Data
/// </summary>
[CreateAssetMenu(fileName = "New Player Selection", menuName = "Pangea/Player Selection Data")]
public class PlayerSelectionData : ScriptableObject
{
    [Header("Información del Personaje")]
    [Tooltip("Nombre del personaje/dinosaurio")]
    public string characterName = "Velociraptor";

    [Tooltip("Descripción del personaje")]
    [TextArea(3, 5)]
    public string description = "Rápido y ágil carnívoro";

    [Tooltip("Icono/imagen del personaje para el botón de selección")]
    public Sprite characterIcon;

    [Header("Prefab del Personaje")]
    [Tooltip("Prefab del personaje que se spawneará en el juego (debe tener PhotonView)")]
    public GameObject characterPrefab;

    [Tooltip("Nombre del prefab en Resources para Photon (debe coincidir con el nombre en Resources folder)")]
    public string prefabResourcePath = "DinosaurPlayer";

    [Header("Stats del Personaje (Opcional - Solo Info)")]
    [Tooltip("Velocidad base del personaje")]
    public float speed = 5f;

    [Tooltip("Vida máxima")]
    public float health = 200f;

    [Tooltip("Daño de ataque")]
    public float attackDamage = 25f;
}
