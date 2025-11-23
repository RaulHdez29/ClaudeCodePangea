using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchThirdPersonCamera : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;
    public Image touchArea;
    
    [Header("Sensibilidad")]
    [Range(0.5f, 5f)]
    public float sensitivity = 2f;
    
    [Header("Suavizado")]
    [Range(0.01f, 0.3f)]
    public float rotationSmoothTime = 0.1f;
    [Range(0.5f, 0.99f)]
    public float inertiaDamping = 0.9f;
    
    [Header("Configuración de Cámara")]
    public float distanceFromTarget = 6f;
    public float heightOffset = 2f;
    public float minDistance = 2f;
    public float maxDistance = 12f;
    
    [Header("Límites Verticales")]
    public float minVerticalAngle = -35f;
    public float maxVerticalAngle = 70f;
    
    [Header("Colisión")]
    public LayerMask collisionLayers = 1;
    public float collisionOffset = 0.3f;
    
    // Variables de rotación
    private float targetRotationX = 0f;
    private float targetRotationY = 0f;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    
    // Variables de suavizado
    private Vector2 rotationVelocity = Vector2.zero;
    private Vector2 smoothVelocityX = Vector2.zero;
    private Vector2 smoothVelocityY = Vector2.zero;
    
    // Variables de input táctil
    private Vector2 currentTouchInput = Vector2.zero;
    private bool isTouching = false;
    
    // Variables de distancia
    private float currentDistance;
    
    void Start()
    {
        SetupCamera();
        SetupTouchArea();
    }
    
    void SetupCamera()
    {
        currentDistance = distanceFromTarget;
        
        if (target != null)
        {
            Vector3 targetPosition = target.position + Vector3.up * heightOffset;
            Vector3 offset = -target.forward * currentDistance;
            transform.position = targetPosition + offset;
            transform.LookAt(targetPosition);
            
            Vector3 direction = transform.position - targetPosition;
            targetRotationY = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            targetRotationX = Mathf.Asin(direction.y / direction.magnitude) * Mathf.Rad2Deg;
            
            currentRotationX = targetRotationX;
            currentRotationY = targetRotationY;
        }
    }
    
    void SetupTouchArea()
    {
        if (touchArea != null)
        {
            // Hacer el área táctil transparente
            Color color = touchArea.color;
            color.a = 0.01f;
            touchArea.color = color;
            
            // Configurar TouchHandler
            TouchHandler touchHandler = touchArea.GetComponent<TouchHandler>();
            if (touchHandler == null)
            {
                touchHandler = touchArea.gameObject.AddComponent<TouchHandler>();
            }
            touchHandler.Initialize(this);
        }
    }
    
    // Método llamado por el TouchHandler para actualizar el input
    public void UpdateTouchInput(Vector2 touchInput, bool isPressed)
    {
        currentTouchInput = touchInput;
        isTouching = isPressed;
    }
    
    // Método llamado cuando inicia el toque
    public void OnTouchStart()
    {
        rotationVelocity = Vector2.zero; // Detener inercia
    }
    
    // Método llamado cuando termina el toque
    public void OnTouchEnd()
    {
        // Capturar velocidad para inercia (simplificado)
        rotationVelocity = currentTouchInput * sensitivity * 0.5f;
    }
    
    void Update()
    {
        UpdateRotation();
    }
    
    void UpdateRotation()
    {
        Vector2 rotationInput = Vector2.zero;
        
        if (isTouching)
        {
            // Usar input táctil directo cuando se está tocando
            rotationInput = currentTouchInput * Time.deltaTime * sensitivity;
        }
        else
        {
            // Aplicar inercia cuando no se está tocando
            rotationVelocity *= inertiaDamping;
            
            // Solo aplicar inercia si es significativa
            if (rotationVelocity.magnitude > 0.01f)
            {
                rotationInput = rotationVelocity * Time.deltaTime;
            }
        }
        
        // Aplicar rotación
        targetRotationY += rotationInput.x;
        targetRotationX -= rotationInput.y; // Invertido para movimiento natural
        
        // Limitar rotación vertical
        targetRotationX = Mathf.Clamp(targetRotationX, minVerticalAngle, maxVerticalAngle);
        
        // Suavizado
        currentRotationX = Mathf.SmoothDampAngle(currentRotationX, targetRotationX, 
                                               ref smoothVelocityX.x, rotationSmoothTime);
        currentRotationY = Mathf.SmoothDampAngle(currentRotationY, targetRotationY, 
                                               ref smoothVelocityY.x, rotationSmoothTime);
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        PositionCamera();
    }
    
    void PositionCamera()
    {
        Vector3 focusPoint = target.position + Vector3.up * heightOffset;
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 desiredPosition = focusPoint + rotation * Vector3.back * currentDistance;
        Vector3 finalPosition = CheckCameraCollision(focusPoint, desiredPosition);
        
        transform.position = finalPosition;
        transform.LookAt(focusPoint);
    }
    
    Vector3 CheckCameraCollision(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 direction = (desiredPosition - focusPoint).normalized;
        float distance = Vector3.Distance(focusPoint, desiredPosition);
        
        RaycastHit hit;
        if (Physics.Raycast(focusPoint, direction, out hit, distance, collisionLayers))
        {
            return focusPoint + direction * (hit.distance - collisionOffset);
        }
        
        return desiredPosition;
    }
    
    public void ZoomCamera(float zoomAmount)
    {
        currentDistance = Mathf.Clamp(currentDistance + zoomAmount, minDistance, maxDistance);
    }
}
