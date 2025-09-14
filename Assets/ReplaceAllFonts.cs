using UnityEngine;
using TMPro;

public class ReplaceAllFonts : MonoBehaviour
{
    public TMP_FontAsset newFont;

    [ContextMenu("Replace Fonts")]
    void ReplaceFonts()
    {
        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true); // true = include inactive
        foreach (var text in texts)
        {
            text.font = newFont;
        }
        Debug.Log($"Replaced {texts.Length} TMP_Text fonts with {newFont.name}");
    }
}
