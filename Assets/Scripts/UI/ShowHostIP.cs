using Mirror;
using TMPro;
using UnityEngine;

public class ShowHostIP : MonoBehaviour
{
    [SerializeField] private TMP_Text ipText;

    //private void Start()
    //{
    //    if (GameSession.Instance.hostIP != null)
    //    {
    //        ipText.text = $"Host IP: {GameSession.Instance.hostIP}";
    //    }
    //}
    private void Start()
{
    if (NetworkServer.active) 
    {
        ipText.text = $"Host IP: {GameSession.Instance.hostIP}";
    }
    else
    {
        ipText.text = "";
    }
    }
}


