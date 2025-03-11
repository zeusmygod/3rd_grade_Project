using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Animations : MonoBehaviour
{
    private Animator animator;
    private NetworkPlayer networkPlayer;

    // 用於跟踪上一幀的輸入狀態
    private bool lastJumpPressed = false;
    private bool lastVerticalPressed = false;

    // 用於限制 RPC 調用頻率的計時器
    private float rpcCooldown = 0f;
    private const float RPC_COOLDOWN_TIME = 0.1f; // 每 0.1 秒最多發送一次 RPC

    void Start()
    {
        // 獲取 NetworkPlayer 組件
        networkPlayer = GetComponentInParent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogError("Could not find NetworkPlayer component in parent hierarchy");
        }

        InitializeAnimator();
    }

    private void InitializeAnimator()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on " + gameObject.name + ". Trying to find in children...");

            animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError("Could not find Animator component in " + gameObject.name + " or its children.");
                return;
            }
        }

        animator.enabled = true;

        // 設置初始動畫參數
        animator.SetBool("isJumpButtonPressed", false);
        animator.SetBool("isVerticalPressed", false);

        // 播放默認動畫
        try
        {
            animator.Play("Idle", 0, 0f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error playing Idle animation: {e.Message}");
        }

        Debug.Log("Animator initialized successfully on " + gameObject.name);
    }

    void Update()
    {
        if (networkPlayer == null)
        {
            networkPlayer = GetComponentInParent<NetworkPlayer>();
            if (networkPlayer == null) return;
        }

        if (animator == null)
        {
            InitializeAnimator();
            if (animator == null) return;
        }

        // 檢查是否有輸入權限
        bool hasInputAuthority = false;
        try
        {
            hasInputAuthority = networkPlayer.Object != null && networkPlayer.Object.HasInputAuthority;
        }
        catch (System.Exception)
        {
            hasInputAuthority = false;
        }

        // 如果有輸入權限，檢測輸入並發送 RPC
        if (hasInputAuthority)
        {
            // 更新 RPC 冷卻計時器
            rpcCooldown -= Time.deltaTime;

            bool jumpPressed = Input.GetButtonDown("Jump");
            bool verticalPressed = Input.GetButton("Vertical");

            // 只有當輸入狀態發生變化且 RPC 冷卻時間已過時才發送 RPC
            if ((jumpPressed != lastJumpPressed || verticalPressed != lastVerticalPressed) && rpcCooldown <= 0)
            {
                // 發送 RPC 更新動畫狀態
                networkPlayer.RPC_UpdateAnimationState(jumpPressed, verticalPressed);

                // 更新上一幀的輸入狀態
                lastJumpPressed = jumpPressed;
                lastVerticalPressed = verticalPressed;

                // 重置 RPC 冷卻計時器
                rpcCooldown = RPC_COOLDOWN_TIME;

                // 記錄狀態變化
                // Debug.Log($"Animation state changed: jump={jumpPressed}, vertical={verticalPressed}");
            }
        }

        // 更新動畫器
        UpdateAnimator();
    }

    // 由 NetworkPlayer 調用來更新動畫狀態
    public void UpdateAnimationState(bool jumpPressed, bool verticalPressed)
    {
        if (animator == null)
        {
            InitializeAnimator();
            if (animator == null) return;
        }

        try
        {
            animator.SetBool("isJumpButtonPressed", jumpPressed);
            animator.SetBool("isVerticalPressed", verticalPressed);

            if (!jumpPressed && !verticalPressed)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                {
                    animator.Play("Idle", 0, 0f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating animator: {e.Message}");
        }
    }

    private void UpdateAnimator()
    {
        // 這個方法現在只是確保動畫器存在
        if (animator == null)
        {
            InitializeAnimator();
        }
    }

    private void OnDestroy()
    {
        animator = null;
        networkPlayer = null;
    }
}
