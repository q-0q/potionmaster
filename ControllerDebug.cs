using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ControllerDebug : MonoBehaviour
{
    private TextMeshProUGUI _tmp;
    private Transform _canvas;

    private void Start()
    {
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
        _canvas = transform.Find("Canvas");
    }

    private void Update()
    {
        _canvas.LookAt(Camera.main.transform);
    }

    public void SetText(string text)
    {
        _tmp.text = text;
    }
}
