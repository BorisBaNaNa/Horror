using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class DynamicBehaviour : MonoBehaviour
{
    public event Action OnStart;
    public event Action OnUpdate;
	private void Start()
    {
        OnStart?.Invoke();
	}
    private void Update()
    {
        OnUpdate?.Invoke();
	}
}
