using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{

    public void PlayIntroScene()
    {
        SceneManager.LoadScene("IntroScene", LoadSceneMode.Single);
    }

    public void PlayMainScene()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    public void PlayMenuScene()
    {
        SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }

    void OnTriggerEnter(Collider other)
    {
        SceneManager.LoadScene("OutroScene", LoadSceneMode.Single);
    }
}
