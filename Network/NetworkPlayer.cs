using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using System.Linq;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer Local { get; set; }
    public Transform playerModel;
    
    // Static collection to track all active players
    private static Dictionary<string, NetworkPlayer> activePlayersByName = new Dictionary<string, NetworkPlayer>();

    [Networked(OnChanged = nameof(OnNickNameChanged))]
    public NetworkString<_16> nickName { get; set; }
    
    [Networked(OnChanged = nameof(OnCharacterSelectionChanged))]
    public int characterSelection { get; set; }
    
    // Add network animation state variables
    [Networked(OnChanged = nameof(OnAnimationStateChanged))]
    public NetworkBool isJumpButtonPressed { get; set; }
    
    [Networked(OnChanged = nameof(OnAnimationStateChanged))]
    public NetworkBool isVerticalPressed { get; set; }
    
    // Reference to the character model prefabs
    public GameObject[] characterPrefabs;
    
    // Reference to the current character model
    private GameObject currentCharacterModel;

    // Start is called before the first frame update
    void Start()
    {

    }
    
    // Clean up static data in Awake to ensure the game starts with a clean state
    void Awake()
    {
        // If this is the first NetworkPlayer instance created, clean up static data
        if (activePlayersByName == null || activePlayersByName.Count == 0)
        {
            Debug.Log("First NetworkPlayer instance created, resetting static variables");
            ResetStaticVariables();
        }
    }
    
    // Reset all static variables
    public static void ResetStaticVariables()
    {
        Debug.Log("Resetting NetworkPlayer static variables");
        
        // Clear the active players dictionary
        if (activePlayersByName != null)
        {
            // Log the current number of active players
            int count = activePlayersByName.Count;
            if (count > 0)
            {
                Debug.Log($"Clearing {count} players from activePlayersByName dictionary");
                foreach (var playerName in activePlayersByName.Keys.ToList())
                {
                    Debug.Log($"Removing player {playerName} from activePlayersByName dictionary");
                }
            }
            
            activePlayersByName.Clear();
        }
        else
        {
            activePlayersByName = new Dictionary<string, NetworkPlayer>();
        }
        
        // Reset the local player reference
        if (Local != null)
        {
            Debug.Log("Resetting NetworkPlayer.Local reference");
            Local = null;
        }
        
        // Force garbage collection
        System.GC.Collect();
        
        Debug.Log("NetworkPlayer static variables have been reset");
    }
    
    // Reset static variables when the application quits
    private void OnApplicationQuit()
    {
        ResetStaticVariables();
    }
    
    // Reset static variables when the scene unloads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnDomainReload()
    {
        ResetStaticVariables();
    }
    
    // Add a static constructor to ensure proper reset in the editor
    static NetworkPlayer()
    {
        #if UNITY_EDITOR
        // Subscribe to play mode state changes in the editor
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        #endif
    }
    
    #if UNITY_EDITOR
    // Called when the editor play mode changes
    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        // Reset static variables when exiting play mode
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("Exiting play mode, resetting NetworkPlayer static variables");
            ResetStaticVariables();
        }
    }
    #endif

    public override void Spawned()
    {
        // Clean up any existing character models first
        CleanupCharacterModel();
        
        if (Object.HasInputAuthority)
        {
            // Clean up any existing old references
            if (Local != null && Local != this)
            {
                Debug.LogWarning("Found existing Local reference when spawning new local player. Cleaning up...");
                Local = null;
            }
            
            Local = this;

            //sets the layer of the local players model
            Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));

            //disable main camera
            Camera.main.gameObject.SetActive(false);

            // Get player name and character selection
            string playerName = PlayerPrefs.GetString("PlayerNickname");
            int selectedCharacter = PlayerPrefs.GetInt("CustomNumber", 1);
            
            // Check if the name is already in use
            if (IsPlayerNameInUse(playerName))
            {
                // Generate a unique name
                playerName = GenerateUniquePlayerName(playerName);
                // Update PlayerPrefs for next time
                PlayerPrefs.SetString("PlayerNickname", playerName);
                PlayerPrefs.Save();
                
                // Notify the player
                Debug.Log($"Your name was already in use. You've been assigned: {playerName}");
            }
            
            // Clean up any invalid players in the dictionary
            CleanupInvalidPlayers();
            
            RPC_SetNickName(playerName);
            RPC_SetCharacterSelection(selectedCharacter);

            Debug.Log($"Spawned local player with name {playerName} and character {selectedCharacter}");
        }
        else
        {
            Camera localCamera = GetComponentInChildren<Camera>();
            if (localCamera != null)
            {
                localCamera.enabled = false;
            }

            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = false;
            }

            Debug.Log("Spawned remote player");
            
            // For remote players, we don't immediately update the character model
            // because we need to wait for their character selection to sync over the network
            // This will be handled in OnCharacterSelectionChanged
        }
        
        // Check if any default models are loaded
        foreach (Transform child in playerModel)
        {
            Debug.Log($"Child in playerModel after spawn: {child.name}");
            
            // If we find Sample01 or other models that shouldn't exist at this point, log a warning
            if (child.name.Contains("Sample") || child.name.Contains("Custom simple human"))
            {
                Debug.LogWarning($"Found unexpected model after spawn: {child.name}");
            }
        }
    }
    
    // Clean up invalid players from the dictionary
    private void CleanupInvalidPlayers()
    {
        if (activePlayersByName == null || activePlayersByName.Count == 0)
        {
            return;
        }
        
        List<string> keysToRemove = new List<string>();
        
        foreach (var kvp in activePlayersByName)
        {
            if (kvp.Value == null || kvp.Value.Object == null || !kvp.Value.Object.IsValid)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in keysToRemove)
        {
            Debug.Log($"Removing invalid player with name {key} from dictionary");
            activePlayersByName.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            Debug.Log($"Cleaned up {keysToRemove.Count} invalid players from dictionary");
        }
    }

    // Check if a player name is already in use
    private bool IsPlayerNameInUse(string name)
    {
        // If the dictionary is empty, the name is definitely not in use
        if (activePlayersByName == null || activePlayersByName.Count == 0)
        {
            return false;
        }
        
        // Check if the name is in the dictionary
        if (activePlayersByName.TryGetValue(name, out NetworkPlayer existingPlayer))
        {
            // If we found a player with the same name, check if it's still alive
            if (existingPlayer != null && existingPlayer.Object != null && !existingPlayer.Object.IsValid)
            {
                // Player no longer exists, remove from dictionary
                Debug.Log($"Found invalid player with name {name}, removing from dictionary");
                activePlayersByName.Remove(name);
                return false;
            }
            
            // If the player exists and is valid, the name is in use
            return true;
        }
        
        // Name is not in the dictionary
        return false;
    }
    
    // Generate a unique player name
    private string GenerateUniquePlayerName(string baseName)
    {
        int counter = 1;
        string newName = baseName + "_" + counter;
        
        while (IsPlayerNameInUse(newName))
        {
            counter++;
            newName = baseName + "_" + counter;
        }
        
        return newName;
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
        {
            Debug.Log($"Player {nickName} with ID {player} left the game");
            
            // Remove from active players dictionary
            string playerNameStr = nickName.ToString();
            if (!string.IsNullOrEmpty(playerNameStr))
            {
                if (activePlayersByName.ContainsKey(playerNameStr))
                {
                    Debug.Log($"Removing player {playerNameStr} from activePlayersByName dictionary");
                    activePlayersByName.Remove(playerNameStr);
                }
                else
                {
                    Debug.LogWarning($"Player {playerNameStr} not found in activePlayersByName dictionary");
                }
            }
            
            // If this is the local player, reset the Local reference
            if (Object.HasInputAuthority)
            {
                Debug.Log("Resetting NetworkPlayer.Local reference");
                Local = null;
            }
            
            // Clean up character model
            CleanupCharacterModel();
            
            // Force garbage collection
            System.GC.Collect();
            
            Runner.Despawn(Object);
        }
    }
    
    // Clean up when the object is destroyed
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        
        Debug.Log($"Player {nickName} despawned");
        
        // Remove from active players dictionary
        string playerNameStr = nickName.ToString();
        if (!string.IsNullOrEmpty(playerNameStr))
        {
            if (activePlayersByName.ContainsKey(playerNameStr))
            {
                Debug.Log($"Removing player {playerNameStr} from activePlayersByName dictionary");
                activePlayersByName.Remove(playerNameStr);
            }
        }
        
        // If this is the local player, reset the Local reference
        if (Object.HasInputAuthority)
        {
            Debug.Log("Resetting NetworkPlayer.Local reference");
            Local = null;
        }
        
        // Clean up character model
        CleanupCharacterModel();
        
        // Force garbage collection
        System.GC.Collect();
    }
    
    // Clean up character model
    private void CleanupCharacterModel()
    {
        if (currentCharacterModel != null)
        {
            Debug.Log($"Destroying current character model: {currentCharacterModel.name}");
            Destroy(currentCharacterModel);
            currentCharacterModel = null;
        }
        
        // Additional check for Sample01 or other model leftovers
        Transform sample01 = playerModel.Find("Sample01(Clone)");
        if (sample01 != null)
        {
            Debug.Log("Found and destroying Sample01(Clone)");
            Destroy(sample01.gameObject);
        }
        
        // Check for any objects starting with "Sample"
        foreach (Transform child in playerModel)
        {
            if (child.name.StartsWith("Sample"))
            {
                Debug.Log($"Found and destroying leftover sample: {child.name}");
                Destroy(child.gameObject);
            }
        }
    }

    static void OnNickNameChanged(Changed<NetworkPlayer> changed)
    {
        Debug.Log($"{Time.time} OnHPChanged value {changed.Behaviour.nickName}");
        changed.Behaviour.OnNickNameChanged();
    }
    
    private void OnNickNameChanged()
    {
        string name = nickName.ToString();
        Debug.Log($"Nickname changed for player to {name} for player {gameObject.name}");
        
        // Update player name dictionary
        if (!string.IsNullOrEmpty(name))
        {
            // If this name is already in use, we need to handle the conflict
            if (activePlayersByName.TryGetValue(name, out NetworkPlayer existingPlayer) && existingPlayer != this)
            {
                // Check if the existing player is valid
                if (existingPlayer != null && existingPlayer.Object != null && existingPlayer.Object.IsValid)
                {
                    Debug.LogWarning($"Player name conflict: {name} is already in use by a valid player!");
                    
                    // If we have input authority, generate a new unique name
                    if (Object.HasInputAuthority)
                    {
                        string uniqueName = GenerateUniquePlayerName(name);
                        Debug.Log($"Generating unique name: {uniqueName}");
                        
                        // Update PlayerPrefs
                        PlayerPrefs.SetString("PlayerNickname", uniqueName);
                        PlayerPrefs.Save();
                        
                        // Set the new name
                        RPC_SetNickName(uniqueName);
                        return;
                    }
                }
                else
                {
                    // Existing player is invalid, remove from dictionary
                    Debug.Log($"Found invalid player with name {name}, removing from dictionary");
                    activePlayersByName.Remove(name);
                }
            }
            
            // Update dictionary
            activePlayersByName[name] = this;
            Debug.Log($"Added player {name} to activePlayersByName dictionary");
        }
        
        playerNickNameTM.text = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"[RPC] SetNickName {nickName}");
        this.nickName = nickName;
    }
    
    static void OnCharacterSelectionChanged(Changed<NetworkPlayer> changed)
    {
        Debug.Log($"{Time.time} OnCharacterSelectionChanged value {changed.Behaviour.characterSelection}");
        changed.Behaviour.OnCharacterSelectionChanged();
    }
    
    private void OnCharacterSelectionChanged()
    {
        Debug.Log($"Character selection changed to {characterSelection} for player {gameObject.name}");
        UpdateCharacterModel();
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetCharacterSelection(int selection, RpcInfo info = default)
    {
        Debug.Log($"[RPC] SetCharacterSelection {selection}");
        this.characterSelection = selection;
    }
    
    // Add RPC method to update animation state
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UpdateAnimationState(bool jumpPressed, bool verticalPressed, RpcInfo info = default)
    {
        // Only update network variables when the state actually changes
        if (this.isJumpButtonPressed != jumpPressed || this.isVerticalPressed != verticalPressed)
        {
            // Debug.Log($"[RPC] UpdateAnimationState jump={jumpPressed}, vertical={verticalPressed}");
            this.isJumpButtonPressed = jumpPressed;
            this.isVerticalPressed = verticalPressed;
        }
    }
    
    // Called when animation state changes
    static void OnAnimationStateChanged(Changed<NetworkPlayer> changed)
    {
        // Only output logs in debug mode
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Debug.Log($"{Time.time} OnAnimationStateChanged jump={changed.Behaviour.isJumpButtonPressed}, vertical={changed.Behaviour.isVerticalPressed}");
        #endif
        
        changed.Behaviour.OnAnimationStateChanged();
    }
    
    // Update animation state
    private void OnAnimationStateChanged()
    {
        // Find the Animations component on the character model
        if (currentCharacterModel != null)
        {
            Animations animationsComponent = currentCharacterModel.GetComponent<Animations>();
            if (animationsComponent != null)
            {
                // Update animation state
                animationsComponent.UpdateAnimationState(isJumpButtonPressed, isVerticalPressed);
            }
        }
    }
    
    private void UpdateCharacterModel()
    {
        // Clean up existing character model
        CleanupCharacterModel();
        
        // Thoroughly clean up all character models under playerModel
        // Not just looking for "Custom simple human", but cleaning up all possible character models
        foreach (Transform child in playerModel)
        {
            // Check if it's a character model (can be determined by name or tag)
            if (child.name.Contains("Custom simple human") || 
                child.name.Contains("Sample") || 
                (child.GetComponent<Animator>() != null && child != playerModel))
            {
                Debug.Log($"Cleaning up existing model: {child.name}");
                Destroy(child.gameObject);
            }
        }
        
        // Load the selected character prefab
        GameObject prefabToLoad;
        string prefabPath;
        
        if (characterSelection < 10)
        {
            prefabPath = "Sample0" + characterSelection;
        }
        else
        {
            prefabPath = "Sample" + characterSelection;
        }
        
        // Load prefab from Resources folder
        prefabToLoad = Resources.Load<GameObject>(prefabPath);
        
        if (prefabToLoad == null)
        {
            Debug.LogError($"Failed to load character prefab: {prefabPath}");
            return;
        }
        
        // Instantiate new character model
        GameObject newChild = Instantiate(prefabToLoad, playerModel);
        newChild.name = "Custom simple human";
        newChild.transform.localScale = new Vector3(7, 7, 7);
        newChild.transform.localPosition = new Vector3(0, -2.05f, 0);
        newChild.transform.localRotation = Quaternion.Euler(0, 0, 0);
        
        // Save reference to current character model
        currentCharacterModel = newChild;
        
        // Ensure animator component is properly set up
        Animator animator = newChild.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Character model does not have an Animator component. Adding one...");
            animator = newChild.AddComponent<Animator>();
            
            // Try to get RuntimeAnimatorController from prefab
            RuntimeAnimatorController controller = prefabToLoad.GetComponent<Animator>()?.runtimeAnimatorController;
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                Debug.LogError("Could not find RuntimeAnimatorController on prefab. Animations may not work.");
            }
        }
        
        // Ensure Animations script is properly set up (now a MonoBehaviour)
        Animations animationsComponent = newChild.GetComponent<Animations>();
        if (animationsComponent == null)
        {
            Debug.Log("Adding Animations component to character model");
            animationsComponent = newChild.AddComponent<Animations>();
        }
        
        // Ensure animation parameters are properly set
        if (animator != null)
        {
            // Set default animation parameters
            animator.SetBool("isJumpButtonPressed", false);
            animator.SetBool("isVerticalPressed", false);
            
            // Ensure animator is enabled
            animator.enabled = true;
            
            // Play default animation (usually Idle)
            try
            {
                animator.Play("Idle", 0, 0f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error playing Idle animation: {e.Message}");
            }
        }
        
        // Check if there are any other character models left
        int childCount = 0;
        foreach (Transform child in playerModel)
        {
            childCount++;
            Debug.Log($"After instantiation - Child {childCount}: {child.name}");
        }
        
        if (childCount > 1)
        {
            Debug.LogWarning($"There are {childCount} children under playerModel after instantiation. Expected only 1.");
        }
        
        // Immediately update animation state
        if (animationsComponent != null)
        {
            animationsComponent.UpdateAnimationState(isJumpButtonPressed, isVerticalPressed);
        }
        
        Debug.Log("Successfully updated character model.");
    }
}
