using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSensitivity = 0.5f;//缩放灵敏度
    public float zoomLimitMin = -1f;//缩小限制
    public float zoomLimitMax = 1f;//放大限制

    private float currentCameraSize = 5f;

    public InputReader inputReader;
    public Camera zoomCamera;

    private void Awake()
    {
        inputReader.ScrollWheel += OnScroll;

        currentCameraSize = zoomCamera.orthographicSize;
    }

    void OnScroll(float scrollValue)
    {
        if (zoomCamera != null)
        {
            float zoomAmount = scrollValue * zoomSensitivity;//计算缩放量

            float newSize = zoomCamera.orthographicSize - zoomAmount;
            // 限制缩放范围
            newSize = Mathf.Clamp(newSize, currentCameraSize + zoomLimitMin, currentCameraSize + zoomLimitMax);

            zoomCamera.orthographicSize = newSize;
        }
    }

}
