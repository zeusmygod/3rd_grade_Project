using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class LocalCameraHandler : MonoBehaviour
{
    public Transform cameraAnchorPoint;

    // Input
    Vector2 viewInput;

    // Rotation
    float cameraRotationX = 0;
    float cameraRotationY = 0;

    // Other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    public Camera localCamera;
    CinemachineVirtualCamera cinemachineVirtualCamera;

    private void Awake()
    {
        // Get the camera component
        localCamera = GetComponent<Camera>();
        if (localCamera == null)
        {
            Debug.LogError("LocalCameraHandler: Camera component not found on this GameObject!");
            localCamera = gameObject.AddComponent<Camera>();
        }

        // Get the network character controller
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        if (networkCharacterControllerPrototypeCustom == null)
        {
            Debug.LogWarning("LocalCameraHandler: NetworkCharacterControllerPrototypeCustom not found in parent hierarchy!");
        }

        // Find CinemachineVirtualCamera and disable damping
        cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (cinemachineVirtualCamera != null)
        {
            DisableCinemachineDamping();
        }
    }

    void Start()
    {
        if (localCamera == null)
        {
            Debug.LogError("LocalCameraHandler: Camera is null in Start!");
            localCamera = GetComponent<Camera>();
            
            if (localCamera == null)
            {
                Debug.LogError("LocalCameraHandler: Failed to get Camera component in Start!");
                enabled = false;
                return;
            }
        }

        if (localCamera.enabled)
        {
            // Detach camera from parent to avoid movement issues
            localCamera.transform.parent = null;
            Debug.Log("LocalCameraHandler: Camera successfully detached from parent hierarchy");
        }
        else
        {
            Debug.LogWarning("LocalCameraHandler: Camera is disabled, not detaching from parent");
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ResetCameraReferences();
    }

    private void ResetCameraReferences()
    {
        cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (cinemachineVirtualCamera != null)
        {
            DisableCinemachineDamping();
        }

        cameraRotationX = 0;
        cameraRotationY = 0;

        if (networkCharacterControllerPrototypeCustom == null)
        {
            networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        }
        
        Debug.Log("LocalCameraHandler: Camera references reset");
    }

    // Disable all damping effects for instant camera follow
    private void DisableCinemachineDamping()
    {
        var transposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_XDamping = 0f;
            transposer.m_YDamping = 0.2f;
            transposer.m_ZDamping = 0f;
        }

        var composer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineComposer>();
        if (composer != null)
        {
            composer.m_HorizontalDamping = 0f;
            composer.m_VerticalDamping = 0f;
        }
    }

    void LateUpdate()
    {
        if (cameraAnchorPoint == null || !localCamera.enabled || NetworkPlayer.Local == null || NetworkPlayer.Local.playerModel == null)
        {
            return;
        }

        // Update Cinemachine target if needed
        if (cinemachineVirtualCamera == null)
        {
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (cinemachineVirtualCamera != null)
            {
                DisableCinemachineDamping();
            }
            else
            {
                return;
            }
        }

        // Force immediate follow (bypass damping)
        cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel;
        cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
        cinemachineVirtualCamera.enabled = true;

        // Directly update camera position and rotation
        localCamera.transform.position = cameraAnchorPoint.position;

        // Calculate rotation
        if (networkCharacterControllerPrototypeCustom == null)
        {
            networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
            if (networkCharacterControllerPrototypeCustom == null)
            {
                // Use default values if controller is missing
                float defaultSpeed = 150f;
                cameraRotationX += viewInput.y * Time.deltaTime * defaultSpeed;
                cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);
                cameraRotationY += viewInput.x * Time.deltaTime * defaultSpeed;
            }
        }
        else
        {
            // Use controller's values
            cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpdownRotationSpeed;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);
            cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCustom.rotationSpeed;
        }

        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
    }

    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }

    void Update()
    {
        // Optional: Add any per-frame checks here
        if (NetworkPlayer.Local != null && cinemachineVirtualCamera != null && 
            (cinemachineVirtualCamera.Follow != NetworkPlayer.Local.playerModel || 
             cinemachineVirtualCamera.LookAt != NetworkPlayer.Local.playerModel))
        {
            cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel;
            cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
        }
    }
}