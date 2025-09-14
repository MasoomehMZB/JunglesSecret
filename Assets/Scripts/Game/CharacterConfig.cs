using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Config")]
public class CharacterConfig : ScriptableObject
{
    [Header("Sprites")]
    public Sprite idle;
    public Sprite[] walkUp;
    public Sprite[] walkDown;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;

    // helper to get frames by direction
    public Sprite[] GetWalkFrames(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return walkUp;
        if (dir == Vector2Int.down) return walkDown;
        if (dir == Vector2Int.left) return walkLeft;
        if (dir == Vector2Int.right) return walkRight;
        return walkDown; // default
    }
}
