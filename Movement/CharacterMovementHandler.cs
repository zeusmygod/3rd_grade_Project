using System.Collections;
using UnityEngine;
using Fusion;

public class CharacterMovementHandler : NetworkBehaviour
{
    public float sensitivity = 1.3f;
    private NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    private Camera localCamera;
    private float currentRotation = 0f;

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        localCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (Object.HasInputAuthority)
        {
            float mouseDeltaX = Input.GetAxis("Mouse X");
            float rotationChange = mouseDeltaX * sensitivity;
            transform.Rotate(0, rotationChange, 0);

            // send local player rotate data to server
            RPC_UpdateRotation(transform.rotation);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateRotation(Quaternion newRotation)
    {
        transform.rotation = newRotation;
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
