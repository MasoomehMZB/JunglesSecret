using TMPro;
using UnityEngine;

public class ShowHostIP : MonoBehaviour
{
    [SerializeField] private TMP_Text ipText;

    private void Start()
    {
        if (GameSession.Instance != null)
        {
            ipText.text = $"Host IP: {GameSession.Instance.hostIP}";
        }
        else
        {
            ipText.text = "";
        }
    }
}
