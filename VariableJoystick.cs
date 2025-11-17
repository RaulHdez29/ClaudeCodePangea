using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Configuración Básica")]
    public float handleRange = 1f;
    public float deadZone = 0.1f;
    public AxisOptions axisOptions = AxisOptions.Both;
    public bool snapX = false;
    public bool snapY = false;
    
    [Header("Retorno Suave (del FP_Joystick)")]
    public float returnRate = 15.0f;              // Velocidad de retorno suave
    
    [Header("Feedback Visual (del FP_Joystick)")]
    public AlphaControll alphaControl;            // Control de transparencia
    
    [Header("Componentes")]
    public RectTransform background = null;
    public RectTransform handle = null;
    
    // Eventos del FP_Joystick
    public event Action<VariableJoystick, Vector2> OnStartJoystickMovement;
    public event Action<VariableJoystick, Vector2> OnJoystickMovement;
    public event Action<VariableJoystick> OnEndJoystickMovement;
    
    private RectTransform baseRect = null;
    private Canvas canvas;
    private Camera cam;
    private Vector2 input = Vector2.zero;
    
    // Variables del FP_Joystick
    private bool isPressed = false;
    private bool shouldReturn = false;
    private CanvasGroup canvasGroup;
    
    public float Horizontal => snapX ? SnapFloat(input.x, AxisOptions.Horizontal) : input.x;
    public float Vertical => snapY ? SnapFloat(input.y, AxisOptions.Vertical) : input.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
    
    public enum AxisOptions { Both, Horizontal, Vertical }
    
    protected virtual void Start()
    {
        HandleRange = handleRange;
        DeadZone = deadZone;
        baseRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
            
        // Configurar CanvasGroup para alpha (del FP_Joystick)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        shouldReturn = true;
        
        // Configurar alpha inicial
        if (alphaControl != null)
            canvasGroup.alpha = alphaControl.idleAlpha;
    }
    
    void Update()
    {
        // Retorno suave cuando no se está presionando (del FP_Joystick)
        if (shouldReturn && !isPressed)
        {
            if (handle.anchoredPosition.magnitude > Mathf.Epsilon)
            {
                Vector2 returnVector = new Vector2(
                    handle.anchoredPosition.x * returnRate, 
                    handle.anchoredPosition.y * returnRate
                ) * Time.deltaTime;
                
                handle.anchoredPosition -= returnVector;
                
                // Actualizar input durante el retorno
                Vector2 radius = background.sizeDelta / 2;
                input = handle.anchoredPosition / (radius * handleRange);
                FormatInput();
                HandleInput(input.magnitude, input.normalized);
                
                // Disparar evento de movimiento durante retorno
                OnJoystickMovement?.Invoke(this, Direction);
            }
            else
            {
                handle.anchoredPosition = Vector2.zero;
                input = Vector2.zero;
            }
        }
        
        // Control de alpha (del FP_Joystick)
        if (alphaControl != null && canvasGroup != null)
        {
            canvasGroup.alpha = isPressed ? alphaControl.pressedAlpha : alphaControl.idleAlpha;
        }
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        shouldReturn = false;
        OnDrag(eventData);
        
        // Evento de inicio (del FP_Joystick)
        OnStartJoystickMovement?.Invoke(this, Direction);
    }
    
    public virtual void OnDrag(PointerEventData eventData)
    {
        Vector2 position = RectTransformUtility.WorldToScreenPoint(cam, background.position);
        Vector2 radius = background.sizeDelta / 2;
        input = (eventData.position - position) / (radius * canvas.scaleFactor);
        FormatInput();
        HandleInput(input.magnitude, input.normalized);
        handle.anchoredPosition = input * radius * handleRange;
        
        // Evento de movimiento (del FP_Joystick)
        OnJoystickMovement?.Invoke(this, Direction);
    }
    
    protected virtual void HandleInput(float magnitude, Vector2 normalised)
    {
        if (magnitude > deadZone)
        {
            if (magnitude > 1)
                input = normalised;
        }
        else
            input = Vector2.zero;
    }
    
    private void FormatInput()
    {
        if (axisOptions == AxisOptions.Horizontal)
            input = new Vector2(input.x, 0f);
        else if (axisOptions == AxisOptions.Vertical)
            input = new Vector2(0f, input.y);
    }
    
    private float SnapFloat(float value, AxisOptions snapAxis)
    {
        if (value == 0)
            return value;
        
        if (axisOptions == AxisOptions.Both)
        {
            float angle = Vector2.Angle(input, Vector2.up);
            if (snapAxis == AxisOptions.Horizontal)
            {
                if (angle < 22.5f || angle > 157.5f)
                    return 0;
                else
                    return (value > 0) ? 1 : -1;
            }
            else if (snapAxis == AxisOptions.Vertical)
            {
                if (angle > 67.5f && angle < 112.5f)
                    return 0;
                else
                    return (value > 0) ? 1 : -1;
            }
            return value;
        }
        else
        {
            if (value > 0)
                return 1;
            if (value < 0)
                return -1;
        }
        return 0;
    }
    
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        shouldReturn = true;
        
        // Evento de fin (del FP_Joystick)
        OnEndJoystickMovement?.Invoke(this);
    }
    
    // Métodos helper del FP_Joystick
    public Vector3 MoveInput()
    {
        return new Vector3(Horizontal, 0, Vertical);
    }
    
    public void Rotate(Transform transformToRotate, float speed)
    {
        if (Direction != Vector2.zero)
            transformToRotate.rotation = Quaternion.Slerp(
                transformToRotate.rotation,
                Quaternion.LookRotation(new Vector3(Direction.x, 0, Direction.y)),
                speed * Time.deltaTime
            );
    }
    
    public bool IsPressed()
    {
        return isPressed;
    }
    
    // Propiedades
    public float HandleRange
    {
        get => handleRange;
        set => handleRange = Mathf.Abs(value);
    }
    
    public float DeadZone
    {
        get => deadZone;
        set => deadZone = Mathf.Abs(value);
    }
}

// Nota: Usa la clase AlphaControll existente del FP_Joystick