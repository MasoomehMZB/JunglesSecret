using System.Collections.Generic;
using UnityEngine;

public class CharacterDatabase : MonoBehaviour
{
    public static CharacterDatabase Instance { get; private set; }
    public List<CharacterConfig> characters = new List<CharacterConfig>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public CharacterConfig Get(int index)
    {
        if (index < 0 || index >= characters.Count) return null;
        return characters[index];
    }

    public Sprite GetIdle(int index)
    {
        var c = Get(index); return c == null ? null : c.idle;
    }
}
