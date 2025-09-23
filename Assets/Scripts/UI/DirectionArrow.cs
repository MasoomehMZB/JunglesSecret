using System;
using UnityEngine;
using UnityEngine.UI;



public class DirectionArrow : MonoBehaviour
{
    public Vector2Int direction;  
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.Log("button where???");
        }
    }
    public void Setup(Action onClick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick.Invoke());
    }
}


