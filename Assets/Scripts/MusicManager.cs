using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    public AudioSource audioSource;
    public bool IsMusicOn = true;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void PlayMusic()
    {
        IsMusicOn = true;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StopMusic()
    {
        IsMusicOn = false;
        audioSource.Stop();
    }
}
