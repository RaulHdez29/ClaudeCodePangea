using UnityEngine;
using System;

/// <summary>
/// Estructura de datos para configuración de jugadores
/// Se usa en el lobby para seleccionar prefabs y UI
/// </summary>
[Serializable]
public class PlayerData
{
    [Header("Información del Jugador")]
    [Tooltip("Nombre del jugador")]
    public string playerName = "Player";

    [Tooltip("Prefab del dinosaurio (debe tener NetworkObject)")]
    public GameObject dinosaurPrefab;

    [Tooltip("Canvas UI del jugador (botones de control)")]
    public GameObject playerUICanvas;

    [Tooltip("Color del jugador (opcional, para identificación)")]
    public Color playerColor = Color.white;

    [Header("Estado")]
    [Tooltip("¿Está este slot activo?")]
    public bool isActive = false;

    [Tooltip("¿Es un jugador local o bot?")]
    public bool isLocalPlayer = true;

    [Header("Spawn")]
    [Tooltip("Posición de spawn personalizada (opcional)")]
    public Vector3 spawnPosition = Vector3.zero;

    [Tooltip("Usar posición de spawn personalizada")]
    public bool useCustomSpawnPosition = false;
}
