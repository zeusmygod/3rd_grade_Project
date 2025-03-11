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
        // Save player nickname
        PlayerPrefs.SetString("PlayerNickname", inputField1.text);
        
        // Validate and save character selection
        int characterNumber;
        if (int.TryParse(inputField2.text, out characterNumber))
        {
            if (characterNumber >= 1 && characterNumber <= 86)
            {
                PlayerPrefs.SetInt("CustomNumber", characterNumber);
            }
            else
            {
                Debug.LogError("Character selection must be between 1 and 86");
                return; // Don't proceed if invalid selection
            }
        }
        else
        {
            Debug.LogError("Please enter a valid number for character selection");
            return; // Don't proceed if invalid input
        }
        
        PlayerPrefs.Save();
        
        // Load the game scene
        SceneManager.LoadScene("SampleScene");
    }
}
