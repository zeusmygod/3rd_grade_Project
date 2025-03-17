using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUIHandler : MonoBehaviour
{
    public TMP_InputField inputField1;
    public TMP_InputField inputField2;

    // Start is called before the first frame update
    void Start()
    {
        /*
        if(PlayerPrefs.HasKey("PlayerNickname"))
        {
            inputField.text = PlayerPrefs.GetString("PlayerNickname");
        }
        */
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
