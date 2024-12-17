using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Animations : NetworkBehaviour
{
    private Animator animator;

    [Networked(OnChanged = nameof(OnAnimationStateChanged))]
    private bool isJumpButtonPressed { get; set; }

    [Networked(OnChanged = nameof(OnAnimationStateChanged))]
    private bool isVerticalPressed { get; set; }

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Object.HasInputAuthority)
        {
            // Update animation states based on input locally
            bool jumpPressed = Input.GetButtonDown("Jump");
            bool verticalPressed = Input.GetButton("Vertical");

            // If states changed, send RPC to update on server
            if (jumpPressed != isJumpButtonPressed || verticalPressed != isVerticalPressed)
            {
                RPC_UpdateAnimationStates(jumpPressed, verticalPressed);
            }
        }
    }

    private static void OnAnimationStateChanged(Changed<Animations> changed)
    {
        changed.Behaviour.UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("isJumpButtonPressed", isJumpButtonPressed);
        animator.SetBool("isVerticalPressed", isVerticalPressed);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateAnimationStates(bool jumpPressed, bool verticalPressed)
    {
        isJumpButtonPressed = jumpPressed;
        isVerticalPressed = verticalPressed;
    }
}
