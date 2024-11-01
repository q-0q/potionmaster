using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CustomerTarget : MonoBehaviour
{
    public string interest = "Item";
    
    private Canvas _canvas;
    private TextMeshProUGUI _interestTmp;
    
    // Start is called before the first frame update

    private void Awake()
    {
        _canvas = GetComponentInChildren<Canvas>();
        _interestTmp = _canvas.transform.Find("Interest").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        // Update debug
        {
            _interestTmp.text = interest;
            _canvas.transform.LookAt(Camera.main.transform);
        }
    }
}
