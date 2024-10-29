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
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
    }
    void Start()
    {
        if(localCamera.enabled)
        {
            localCamera.transform.parent = null;
        }
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
        if(cinemachineVirtualCamera==null)
        {
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }
        cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel; 
        cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
        cinemachineVirtualCamera.enabled = true;
        Utils.SetRenderLayerInChildren(NetworkPlayer.Local.playerModel,LayerMask.NameToLayer("Default"));

        //Move the camera to the position of the player
        localCamera.transform.position = cameraAnchorPoint.position;

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
}
