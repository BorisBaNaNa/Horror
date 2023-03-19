using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public GameObject cutsceneElevator;
    public GameObject cutsceneEnding;
    private GameObject cutscene;

    public GameObject player;
    public GameObject cameraBrain;
    public GameObject playerCutsceneCamera;

    void Start()
    {
        cutscene = cutsceneElevator;
    }

    public void endCutscene()
    {
        cutscene.SetActive(false);
        cameraBrain.SetActive(false);
        player.SetActive(true);
        playerCutsceneCamera.SetActive(false);
        cutscene = cutsceneEnding;
    }

    public void startCutscene()
    {
        cutscene.SetActive(true);
        cameraBrain.SetActive(true);
        player.SetActive(false);
        playerCutsceneCamera.SetActive(true);
    }
}
