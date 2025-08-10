using UnityEngine;

public class Key : MonoBehaviour
{
    public void OnPlayerLanded(Player player)
    {
        GameManager.Instance.StartGuessMode(player);
    }
}
