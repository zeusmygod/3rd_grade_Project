using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine; 

public class LocalCameraHandler : MonoBehaviour
{
    public Transform cameraAnchorPoint;

    //Input
    Vector2 viewInput;

    //Rotation
    float cameraRotationX = 0;
    float cameraRotationY = 0;

    //other component
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
        
        // Try to find the CinemachineVirtualCamera early
        cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
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
            
            // Log successful camera setup
            Debug.Log("LocalCameraHandler: Camera successfully detached from parent hierarchy");
        }
        else
        {
            Debug.LogWarning("LocalCameraHandler: Camera is disabled, not detaching from parent");
        }
    }

    private void OnEnable()
    {
        // Subscribe to scene events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from scene events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Reset camera references when a new scene is loaded
        ResetCameraReferences();
    }

    private void ResetCameraReferences()
    {
        // Find the CinemachineVirtualCamera again
        cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        
        // Reset rotation values
        cameraRotationX = 0;
        cameraRotationY = 0;
        
        // Try to get the controller again
        if (networkCharacterControllerPrototypeCustom == null)
        {
            networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        }
        
        Debug.Log("LocalCameraHandler: Camera references reset");
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(cameraAnchorPoint == null)
        {
            return;
        }
        if(!localCamera.enabled)
        {
            return;
        }
        
        // Check if NetworkPlayer.Local is null before accessing it
        if(NetworkPlayer.Local == null)
        {
            // Debug.LogWarning("NetworkPlayer.Local is null in LocalCameraHandler");
            return;
        }
        
        // Check if playerModel is null
        if(NetworkPlayer.Local.playerModel == null)
        {
            // Debug.LogWarning("NetworkPlayer.Local.playerModel is null in LocalCameraHandler");
            return;
        }
        
        if(cinemachineVirtualCamera==null)
        {
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            
            // Check if cinemachineVirtualCamera is still null after trying to find it
            if(cinemachineVirtualCamera == null)
            {
                // Debug.LogWarning("Could not find CinemachineVirtualCamera in the scene");
                return;
            }
        }
        
        cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel; 
        cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
        cinemachineVirtualCamera.enabled = true;
        Utils.SetRenderLayerInChildren(NetworkPlayer.Local.playerModel,LayerMask.NameToLayer("Default"));

        //Move the camera to the position of the player
        localCamera.transform.position = cameraAnchorPoint.position;

        // Check if networkCharacterControllerPrototypeCustom is null
        if(networkCharacterControllerPrototypeCustom == null)
        {
            // Try to get the component again
            networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
            
            // If still null, use default values
            if(networkCharacterControllerPrototypeCustom == null)
            {
                // Use default rotation speeds
                float defaultViewUpdownRotationSpeed = 150f;
                float defaultRotationSpeed = 150f;
                
                //calculate the rotation with default values
                cameraRotationX += viewInput.y * Time.deltaTime * defaultViewUpdownRotationSpeed;
                cameraRotationX = Mathf.Clamp(cameraRotationX,-90,90);

                cameraRotationY += viewInput.x * Time.deltaTime * defaultRotationSpeed;
                localCamera.transform.rotation = Quaternion.Euler(cameraRotationX,cameraRotationY,0);
                return;
            }
        }

        //calculate the rotation
        cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpdownRotationSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX,-90,90);

        cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCustom.rotationSpeed;
        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX,cameraRotationY,0);
        //localCamera.transform.rotation = Quaternion.Euler(cameraRotationX,0,0);
    }

    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if NetworkPlayer.Local has changed
        if (NetworkPlayer.Local != null && cinemachineVirtualCamera != null)
        {
            // Update camera target if needed
            if (cinemachineVirtualCamera.Follow != NetworkPlayer.Local.playerModel)
            {
                cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel;
                cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
                Debug.Log("LocalCameraHandler: Updated camera target to new local player");
            }
        }
    }
}
