using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUIHandler : MonoBehaviour
{
    public TMP_InputField inputField1;
    public TMP_InputField inputField2;
    
    [Header("Character Preview")]
    public Transform previewSpawnPoint; // 用於放置預覽模型的位置
    public Vector3 previewRotation = new Vector3(0, 180, 0); // 預覽模型的旋轉角度
    public Vector3 previewScale = new Vector3(1, 1, 1); // 預覽模型的縮放大小
    [Tooltip("是否對所有角色使用統一的縮放大小")]
    public bool useUniformScale = true; // 是否使用統一縮放
    [Tooltip("每個角色的自定義縮放大小")]
    public Vector3[] characterCustomScales; // 每個角色的自定義縮放
    private GameObject currentPreviewModel; // 當前預覽的模型
    
    [Header("Character Prefabs")]
    public GameObject[] characterPrefabs; // 角色預製件陣列

    // Start is called before the first frame update
    void Start()
    {
        /*
        if(PlayerPrefs.HasKey("PlayerNickname"))
        {
            inputField.text = PlayerPrefs.GetString("PlayerNickname");
        }
        */

        // 添加輸入變更監聽
        inputField2.onValueChanged.AddListener(OnCharacterNumberChanged);
        
        // 初始化自定義縮放陣列
        if (characterCustomScales == null || characterCustomScales.Length != characterPrefabs.Length)
        {
            characterCustomScales = new Vector3[characterPrefabs.Length];
            for (int i = 0; i < characterCustomScales.Length; i++)
            {
                characterCustomScales[i] = previewScale;
            }
        }
    }

    void OnCharacterNumberChanged(string value)
    {
        // 如果輸入為空，清除預覽模型
        if (string.IsNullOrEmpty(value))
        {
            if (currentPreviewModel != null)
            {
                Destroy(currentPreviewModel);
                currentPreviewModel = null;
            }
            return;
        }

        // 檢查是否為有效數字且在範圍內
        if (int.TryParse(value, out int characterNumber) && characterNumber >= 1 && characterNumber <= 86)
        {
            UpdateCharacterPreview(characterNumber);
        }
        else
        {
            // 如果數字無效或超出範圍，清除預覽模型
            if (currentPreviewModel != null)
            {
                Destroy(currentPreviewModel);
                currentPreviewModel = null;
            }
        }
    }

    void UpdateCharacterPreview(int characterNumber)
    {
        // 清除現有的預覽模型
        if (currentPreviewModel != null)
        {
            Destroy(currentPreviewModel);
        }

        // 確保角色編號在有效範圍內且prefabs陣列已設置
        if (characterPrefabs != null && characterNumber > 0 && characterNumber <= characterPrefabs.Length)
        {
            // 生成新的預覽模型
            GameObject prefab = characterPrefabs[characterNumber - 1];
            if (prefab != null)
            {
                currentPreviewModel = Instantiate(prefab, previewSpawnPoint.position, Quaternion.Euler(previewRotation));
                currentPreviewModel.transform.SetParent(previewSpawnPoint);
                
                // 設置縮放大小
                if (useUniformScale)
                {
                    currentPreviewModel.transform.localScale = previewScale;
                }
                else
                {
                    currentPreviewModel.transform.localScale = characterCustomScales[characterNumber - 1];
                }
            }
        }
    }

    // 提供一個方法來動態調整當前預覽模型的大小
    public void AdjustCurrentPreviewScale(Vector3 newScale)
    {
        if (currentPreviewModel != null)
        {
            currentPreviewModel.transform.localScale = newScale;
            
            // 如果不使用統一縮放，則保存這個角色的自定義縮放
            if (!useUniformScale && !string.IsNullOrEmpty(inputField2.text))
            {
                if (int.TryParse(inputField2.text, out int characterNumber) && 
                    characterNumber >= 1 && characterNumber <= characterPrefabs.Length)
                {
                    characterCustomScales[characterNumber - 1] = newScale;
                }
            }
            else
            {
                previewScale = newScale;
            }
        }
    }

    public void OnJoinGameClicked()
    {
        // 角色選擇
        int characterNumber;
        if (int.TryParse(inputField2.text, out characterNumber))
        {
            if (characterNumber >= 1 && characterNumber <= 86)
            {
                // 將玩家信息存儲到TempPlayerInfo中
                TempPlayerInfo.Name = inputField1.text;
                TempPlayerInfo.CharacterSelection = characterNumber;
                
                // 加載遊戲場景
                SceneManager.LoadScene("SampleScene");
            }
            else
            {
                Debug.LogError("Character selection must be between 1 and 86");
                return;
            }
        }
        else
        {
            Debug.LogError("Please enter a valid number for character selection");
            return;
        }
    }
}
