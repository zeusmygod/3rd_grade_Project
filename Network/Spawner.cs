using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkPlayer playerPrefab;
    
    // Dictionary to keep track of spawned players
    private Dictionary<PlayerRef, NetworkPlayer> spawnedPlayers = new Dictionary<PlayerRef, NetworkPlayer>();

    //other components
    CharacterInputHandler characterInputHandler;
    
    // Clean up when the application quits
    private void OnApplicationQuit()
    {
        ClearAllPlayers();
    }
    
    // Clear all players
    private void ClearAllPlayers()
    {
        foreach (var playerEntry in spawnedPlayers)
        {
            if (playerEntry.Value != null)
            {
                Destroy(playerEntry.Value.gameObject);
            }
        }
        
        spawnedPlayers.Clear();
        characterInputHandler = null;
    }
    
    // Clean up when the scene unloads
    private void OnDestroy()
    {
        ClearAllPlayers();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Debug.Log($"OnPlayerJoined we are server. Spawning player with ID: {player}");
            
            // Check if this player is already spawned
            if (spawnedPlayers.ContainsKey(player))
            {
                NetworkPlayer existingPlayer = spawnedPlayers[player];
                
                // Check if the existing player is valid
                if (existingPlayer != null && existingPlayer.Object != null && existingPlayer.Object.IsValid)
                {
                    Debug.Log($"Player {player} already exists in the world and is valid.");
                    return;
                }
                else
                {
                    // Player reference exists but is invalid, clean it up
                    Debug.Log($"Player {player} exists in dictionary but is invalid. Cleaning up...");
                    if (existingPlayer != null)
                    {
                        // Try to destroy the game object
                        try
                        {
                            Destroy(existingPlayer.gameObject);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error destroying invalid player: {e.Message}");
                        }
                    }
                    
                    // Remove from dictionary
                    spawnedPlayers.Remove(player);
                }
            }
            
            // Before spawning a new player, ensure NetworkPlayer static data is clean
            if (player.PlayerId == 0) // Usually the first player has ID 0
            {
                Debug.Log("First player joining, ensuring static data is clean");
                NetworkPlayer.ResetStaticVariables();
            }
            
            // Spawn a new player
            try
            {
                NetworkPlayer networkPlayer = runner.Spawn(playerPrefab, Utils.GetRandomSpawnPoint(), Quaternion.identity, player);
                
                // Add to our dictionary
                spawnedPlayers[player] = networkPlayer;
                
                Debug.Log($"Successfully spawned new player with ID: {player}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning player: {e.Message}");
            }
        }
        else
        {
            Debug.Log($"OnPlayerJoined as client for player {player}");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (characterInputHandler == null && NetworkPlayer.Local != null)
        {
            characterInputHandler = NetworkPlayer.Local.GetComponent<CharacterInputHandler>();
        }
        if (characterInputHandler != null)
        {
            input.Set(characterInputHandler.GetNetworkInput());
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        Debug.Log($"Player {player} left the game");
        
        // When a player leaves, remove them from our dictionary
        if (spawnedPlayers.ContainsKey(player))
        {
            NetworkPlayer networkPlayer = spawnedPlayers[player];
            
            // Ensure player is fully cleaned up
            if (networkPlayer != null)
            {
                // Call PlayerLeft method before destroying
                networkPlayer.PlayerLeft(player);
            }
            
            spawnedPlayers.Remove(player);
            Debug.Log($"Player {player} left and was removed from tracked players.");
            
            // Check if there are any other players
            if (spawnedPlayers.Count == 0)
            {
                Debug.Log("No players left in the game. Resetting all player data.");
                // Reset NetworkPlayer static variables
                NetworkPlayer.ResetStaticVariables();
            }
        }
        else
        {
            Debug.LogWarning($"Player {player} left but was not found in tracked players.");
        }
        
        // Force garbage collection
        System.GC.Collect();
    }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    { 
        Debug.Log($"OnShutdown with reason: {shutdownReason}");
        
        // Clean up all players
        ClearAllPlayers();
        
        // Reset NetworkPlayer static variables
        NetworkPlayer.ResetStaticVariables();
        
        // Clear player name from PlayerPrefs (optional, depends on your needs)
        // If you want players to keep the same name when restarting the game, comment out this line
        // PlayerPrefs.DeleteKey("PlayerNickname");
        
        // Force garbage collection
        System.GC.Collect();
    }
    
    // Add an editor method for manual reset
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Reset Network Player Data")]
    private static void ResetNetworkPlayerData()
    {
        NetworkPlayer.ResetStaticVariables();
        Debug.Log("Network player data has been manually reset");
    }
    #endif
    
    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("OnConnectedToServer"); }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) 
    { 
        Debug.Log("OnDisconnectedFromServer - Cleaning up all players and resetting data");
        
        // Clean up all players
        ClearAllPlayers();
        
        // Reset NetworkPlayer static variables
        NetworkPlayer.ResetStaticVariables();
        
        // Force garbage collection
        System.GC.Collect();
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { Debug.Log("OnConnectRequest"); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.Log("OnConnectFailed"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
