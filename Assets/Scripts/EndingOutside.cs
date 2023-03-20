using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingOutside : MonoBehaviour
{

    public GameObject giantEntity;
    private CutsceneManager cutsceneManager;

    private void Start()
    {
        cutsceneManager = FindObjectOfType<CutsceneManager>();

    }

    private void OnTriggerEnter(Collider other)
    {
        giantEntity.SetActive(true);
        cutsceneManager.startCutscene();
    }
}
