using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryButton : MonoBehaviour
{

    public string interest;
    public static event Action<string> OnPlayerSelectGhostItem;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ButtonClicked()
    {
        OnPlayerSelectGhostItem?.Invoke(interest);
    }
}