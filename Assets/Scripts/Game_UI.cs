using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Game_UI : MonoBehaviour
{
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI highScoreText;
    public GameObject inGameFrame;
    public GameObject startButton;
    public GameObject gameOverUI;
    public GameObject gameScale4x4Button;
    public GameObject gameScale8x8Button;
    public GameObject titleUI;
    public GameObject settingUI;

    // Start is called before the first frame update
    void Start()
    {
        settingUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SettingUI()
    {
        if(settingUI.activeSelf)
        {
            settingUI.SetActive(false);
        }
        else
        {
            settingUI.SetActive(true);
        }
    }
}
