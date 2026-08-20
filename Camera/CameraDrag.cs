using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDrag : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Camera Targetcamera;

    Vector2 mouseAnchorPoint = Vector2.zero;//鼠标锚点
    Vector2 cameraAnchorPoint = Vector2.zero;//相机锚点

    Vector2 MouseCurrentLocation = Vector2.zero;//当前鼠标位置

    [SerializeField] private float dragSensitivity = 0.02f;//拖动灵敏度
    [SerializeField, ReadOnly] private bool isDragging = false;//拖动状态

    public float DragSensitivity => dragSensitivity;//拖动灵敏度对外接口
    public bool IsDragging => isDragging;//拖动状态对外接口


    private void Awake()
    {
        inputReader.MiddlePress += OnMiddlePress;
        inputReader.MiddleCancele += OnMiddleCancele;
    }

    private void OnMiddlePress()
    {
        isDragging = true;

        if (Mouse.current != null)
        {
            MouseCurrentLocation = Mouse.current.position.ReadValue();
        }

        //锚定鼠标位置和相机位置
        mouseAnchorPoint = MouseCurrentLocation;
        cameraAnchorPoint = Targetcamera.transform.position;
    }

    private void OnMiddleCancele()
    {
        isDragging = false;

        mouseAnchorPoint = Vector2.zero;
        cameraAnchorPoint = Vector2.zero;
    }


    private void Update()
    {
        if (isDragging && Targetcamera != null)
        {
            if (Mouse.current != null)
            {
                MouseCurrentLocation = Mouse.current.position.ReadValue();
            }

            Vector2 MoveVector = CalculateVector() * dragSensitivity;//计算插值向量，并乘以灵敏度

            Vector3 targetPos = cameraAnchorPoint + MoveVector;

            Targetcamera.transform.position = new Vector3(targetPos.x, targetPos.y, Targetcamera.transform.position.z);
        }
    }

    private Vector2 CalculateVector()
    {
        // 计算鼠标位置和MiddlePressLocation的差值（屏幕空间像素）
        if (Mouse.current == null)
        {
            return Vector2.zero;
        }
        return mouseAnchorPoint - MouseCurrentLocation;
    }
}
