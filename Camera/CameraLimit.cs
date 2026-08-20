
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLimit : MonoBehaviour
{
    [SerializeField] Collider2D mapBorder;
    [SerializeField] Camera targetCamera;

    private void LateUpdate()
    {
        if (mapBorder == null || targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        Bounds bounds = mapBorder.bounds;

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        Vector3 camPos = targetCamera.transform.position;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        float clampedX = (minX > maxX) ? bounds.center.x : Mathf.Clamp(camPos.x, minX, maxX);
        float clampedY = (minY > maxY) ? bounds.center.y : Mathf.Clamp(camPos.y, minY, maxY);

        targetCamera.transform.position = new Vector3(clampedX, clampedY, camPos.z);
    }

    



}
