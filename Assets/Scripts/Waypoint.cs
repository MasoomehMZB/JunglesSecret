using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


public enum DirectionName
{
    Up,
    Down,
    Left,
    Right
}
public class Waypoint : MonoBehaviour
{
    private List<Vector2Int> allowedDirVectors;  // Set in Inspector (e.g. [left, right])
    public GameObject choiceUIPrefab;     // A prefab with buttons for direction choices
    private GameObject currentUI;

    public List<DirectionName> allowedDirections;
    private HashSet<Vector2Int> allowedDirSet;

    void Awake()
    {
        // Convert enum list to Vector2Int list on load
        allowedDirVectors = allowedDirections.Select(d => DirectionUtils.ToVector(d)).ToList();
        allowedDirSet = new HashSet<Vector2Int>(allowedDirVectors);
    }


    public void ShowChoicesUI(Action<Vector2Int> onDirectionChosen, Vector2Int currentDir)
    {
        if (choiceUIPrefab == null)
        {
            Debug.LogError("Choice UI Prefab not set!");
            return;
        }

        if (allowedDirSet.Count == 2)
        {
            var autoDir = allowedDirSet.First(d => d != -currentDir);
            onDirectionChosen(autoDir);
            return;
        }

        currentUI = Instantiate(choiceUIPrefab, transform.position, Quaternion.identity, transform);
        Canvas canvas = currentUI.GetComponent<Canvas>();

        if (canvas != null)
            canvas.worldCamera = Camera.main;

        // Assume prefab has a script "DirectionButton" on each button with setup method
        DirectionArrow[] buttons = currentUI.GetComponentsInChildren<DirectionArrow>();


        foreach (DirectionArrow btn in buttons)
        {

            if (allowedDirSet.Contains(btn.direction) && btn.direction != -currentDir)
            {

                btn.Setup(() => {
                    onDirectionChosen(btn.direction);
                    Destroy(currentUI); // remove UI after choice
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

}

public static class DirectionUtils
{
    public static Vector2Int ToVector(DirectionName dir)
    {
        return dir switch
        {
            DirectionName.Up => Vector2Int.up,
            DirectionName.Down => Vector2Int.down,
            DirectionName.Left => Vector2Int.left,
            DirectionName.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }
}