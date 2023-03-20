using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public GameObject cutsceneStart;
    public GameObject cutsceneEnding;
    private GameObject cutscene;

    public GameObject player;
    public GameObject cameraBrain;
    public GameObject playerCutsceneCamera;

    void Start()
    {
        cutscene = cutsceneStart;
    }

    public void endCutscene()
    {
        cutscene.SetActive(false);
        cameraBrain.SetActive(false);
        player.GetComponent<PlayerController>().enabled = true;
        playerCutsceneCamera.SetActive(false);

    }

    public void startCutscene()
    {
        cutscene = cutsceneEnding;
        cutscene.SetActive(true);
        cameraBrain.SetActive(true);
        player.GetComponent<PlayerController>().enabled = false;
        playerCutsceneCamera.SetActive(true);
    }
}
