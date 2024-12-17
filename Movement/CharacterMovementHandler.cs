using System.Collections;
using UnityEngine;
using Fusion;

public class CharacterMovementHandler : NetworkBehaviour
{
    public float sensitivity = 1.3f;
    private NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    private Camera localCamera;
    //private float currentRotation = 0f;
    private float mouseDeltaX = 0f;
    private float rotationChange = 0f;
    private bool isLeftAltPressed = false;
    

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        localCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (Object.HasInputAuthority && Cursor.lockState == CursorLockMode.Locked/* && Input.GetMouseButton(1)*/)
        {
            mouseDeltaX = Input.GetAxis("Mouse X");
            rotationChange = mouseDeltaX * sensitivity;
            
            // send local player rotate data to server
            if (rotationChange != 0)
            {
                RPC_UpdateRotation(rotationChange);
            }
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                isLeftAltPressed = !isLeftAltPressed;
                RPC_CursorLock(isLeftAltPressed);
            }
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateRotation(float rotationChange)
    {
        transform.Rotate(0, rotationChange, 0);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CursorLock(bool isLeftAltPressed)
    {
        if (isLeftAltPressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData))
        {
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
            moveDirection = transform.InverseTransformDirection(moveDirection);

            networkCharacterControllerPrototypeCustom.Move(moveDirection);

            if (networkInputData.isJumpPressed)
            {
                networkCharacterControllerPrototypeCustom.Jump();
            }

            CheckFallRespawn();
        }
    }

    private void CheckFallRespawn()
    {
        if (transform.position.y < -67)
        {
            transform.position = Utils.GetRandomSpawnPoint();
        }
    }
}
