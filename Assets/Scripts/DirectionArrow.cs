using UnityEngine;
using UnityEngine.UI;
using System;



public class DirectionArrow : MonoBehaviour
{
    public Vector2Int direction;  // Set in Inspector or dynamically
    private Button button;

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


