using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{

    public float t;

    private Image _image;

    private void Awake()
    {
        _image = transform.Find("Loader").GetComponent<Image>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _image.transform.localScale = new Vector3(t, _image.transform.localScale.y, _image.transform.localScale.z);
    }
}
