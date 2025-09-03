using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 lastMousePosition;
    private Vector3 mouseDelta;
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float smoothSpeed = 10f;

    void Awake()
    {
        mainCamera = Camera.main;
        // Initialize last mouse position
        lastMousePosition = Input.mousePosition;
    }
    
    void Update()
    {
        // Get mouse position in screen coordinates
        Vector3 screenPos = Input.mousePosition;
        
        // Calculate mouse delta for smooth movement
        mouseDelta = screenPos - lastMousePosition;
        lastMousePosition = screenPos;
        
        // Convert to world point using the camera's near clip plane distance
        // This ensures consistent behavior across different resolutions and DPI settings
        float z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        
        // Apply position with optional smoothing
        if (smoothMovement)
        {
            // Only smooth if we have significant movement to prevent jitter
            if (mouseDelta.magnitude < 10f)
            {
                transform.position = Vector3.Lerp(transform.position, worldPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = worldPosition;
            }
        }
        else
        {
            transform.position = worldPosition;
        }
    }
    
    // Helper method to get the current mouse position in world space
    public Vector3 GetMouseWorldPosition()
    {
        float z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, z));
    }
}
