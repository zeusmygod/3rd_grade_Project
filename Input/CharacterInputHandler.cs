using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector2 moveInputVector = Vector2.zero;
    Vector2 viewInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;
    bool isLeftAltPressed = false;
    //other component
    // CharacterMovementHandler characterMovementHandler;
    LocalCameraHandler localCameraHandler;

    private void Awake()
    {
        //characterMovementHandler = GetComponent<CharacterMovementHandler>();
        localCameraHandler = GetComponentInChildren<LocalCameraHandler>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Screen.lockCursor = false;
    }

    // Update is called once per frame
    void Update()
    {
        //view
        /*if(Input.GetMouseButton(1))
        {
            viewInputVector.x = Input.GetAxis("Mouse X");
            viewInputVector.y = Input.GetAxis("Mouse Y") * -1 ;

            moveInputVector.x = Input.GetAxis("Mouse X");

            characterMovementHandler.SetViewInputVector(viewInputVector);
            characterMovementHandler.SetViewInputVector(moveInputVector);
        }*/
        //鼠標切換
        if(Input.GetKeyDown(KeyCode.LeftAlt))
        {
            isLeftAltPressed = !isLeftAltPressed;
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

        //Move
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        //Jump
        if(Input.GetButtonDown("Jump"))
        {
            isJumpButtonPressed = true;
        }

        
        //Set view
        localCameraHandler.SetViewInputVector(viewInputVector);
    }

    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        /*//view data
        networkInputData.rotationInput = viewInputVector.x;*/

        //Aim data
        networkInputData.aimForwardVector = localCameraHandler.transform.forward;

        //left alt data
        networkInputData.isLeftAltPressed = isLeftAltPressed;

        //move data
        networkInputData.movementInput = moveInputVector;

        //jump data
        networkInputData.isJumpPressed = isJumpButtonPressed;

        isJumpButtonPressed = false;

        return networkInputData;
    }
}
