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
        PlayerPrefs.SetString("PlayerNickname", inputField1.text);
        PlayerPrefs.SetInt("CustomNumber", int.Parse(inputField2.text));
        if (int.Parse(inputField2.text) > 86 || int.Parse(inputField2.text) < 1)
        {
            Debug.LogError("Select from 1 to 86");
        }
        PlayerPrefs.Save();

        PlayerPrefs.SetInt("isSpawned", 0);

        SceneManager.LoadScene("SampleScene");
    }
}
