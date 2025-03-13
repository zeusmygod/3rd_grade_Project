using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SphereMovementHandler : NetworkBehaviour
{
    // 網絡同步屬性
    [Networked] private Vector3 NetworkedVelocity { get; set; }
    [Networked] private Vector3 NetworkedAngularVelocity { get; set; }
    [Networked] private NetworkBool IsFalling { get; set; }
    
    // 組件引用
    private Rigidbody rb;
    private NetworkRigidbody networkRigidbody;
    
    // 調試選項
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        
        if (networkRigidbody == null)
        {
            Debug.LogError("NetworkRigidbody component is missing on the sphere! Please add it.");
        }
    }

    public override void Spawned()
    {
        // 當物體生成時初始化
        if (Object.HasStateAuthority)
        {
            // 確保初始狀態正確
            NetworkedVelocity = rb.velocity;
            NetworkedAngularVelocity = rb.angularVelocity;
            IsFalling = false;
            
            if (debugMode) Debug.Log("Sphere spawned with state authority");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // 權威端：更新網絡同步的速度
            NetworkedVelocity = rb.velocity;
            NetworkedAngularVelocity = rb.angularVelocity;
            
            // 檢查是否掉落
            CheckFallRespawn();
        }
        else
        {
            // 非權威端：根據網絡數據更新物理
            rb.velocity = NetworkedVelocity;
            rb.angularVelocity = NetworkedAngularVelocity;
        }
    }

    void CheckFallRespawn()
    {
        if (transform.position.y < -67 && !IsFalling)
        {
            if (debugMode) Debug.Log("Sphere fell below threshold, resetting position");
            IsFalling = true;
            RPC_ResetSphere();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetSphere()
    {
        if (debugMode) Debug.Log("RPC_ResetSphere called on " + (Object.HasStateAuthority ? "authority" : "non-authority"));
        
        // 重置球體位置和速度
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = Utils.GetSpawnPointSphere();
        
        // 延遲重置掉落標記，確保所有客戶端都完成重置
        StartCoroutine(ResetFallingFlag());
    }
    
    private IEnumerator ResetFallingFlag()
    {
        // 等待一小段時間，確保重置完成
        yield return new WaitForSeconds(0.5f);
        
        if (Object.HasStateAuthority)
        {
            IsFalling = false;
            if (debugMode) Debug.Log("Reset falling flag");
        }
    }

    // 提供一個公共方法來應用外部力
    public void ApplyForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        if (Object.HasStateAuthority)
        {
            rb.AddForce(force, forceMode);
            if (debugMode) Debug.Log($"Applied force: {force}");
        }
        else
        {
            // 非權威端可以請求應用力
            RPC_RequestApplyForce(force, (int)forceMode);
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestApplyForce(Vector3 force, int forceModeInt)
    {
        if (Object.HasStateAuthority)
        {
            ForceMode forceMode = (ForceMode)forceModeInt;
            rb.AddForce(force, forceMode);
            if (debugMode) Debug.Log($"RPC applied force: {force}");
        }
    }

    // 提供一個公共方法來應用扭矩
    public void ApplyTorque(Vector3 torque, ForceMode forceMode = ForceMode.Force)
    {
        if (Object.HasStateAuthority)
        {
            rb.AddTorque(torque, forceMode);
            if (debugMode) Debug.Log($"Applied torque: {torque}");
        }
        else
        {
            // 非權威端可以請求應用扭矩
            RPC_RequestApplyTorque(torque, (int)forceMode);
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestApplyTorque(Vector3 torque, int forceModeInt)
    {
        if (Object.HasStateAuthority)
        {
            ForceMode forceMode = (ForceMode)forceModeInt;
            rb.AddTorque(torque, forceMode);
            if (debugMode) Debug.Log($"RPC applied torque: {torque}");
        }
    }
    
    // 允許其他玩家推動球體
    public void OnCollisionEnter(Collision collision)
    {
        // 檢查是否與玩家碰撞
        NetworkPlayer player = collision.gameObject.GetComponent<NetworkPlayer>();
        if (player != null && !Object.HasStateAuthority)
        {
            // 計算碰撞力
            Vector3 force = collision.impulse / Time.fixedDeltaTime;
            // 請求應用力
            RPC_RequestApplyForce(force * 0.5f, (int)ForceMode.Impulse);
            
            if (debugMode) Debug.Log($"Collision with player, requesting force: {force * 0.5f}");
        }
    }
}
