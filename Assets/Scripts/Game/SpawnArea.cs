using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public static SpawnArea Instance;
    public Transform[] slots;

    private void Awake()
    {
        Instance = this;
    }
    public Vector3 GetSpawnPosition(int? playerIndex = null)
    {
        if (slots.Length == 0)
        {
            Debug.LogError("No spawn slots assigned in SpawnArea!");
            return transform.position;
        }

        int index;

        if (playerIndex.HasValue)
        {
            index = Mathf.Clamp(playerIndex.Value, 0, slots.Length - 1);
        }
        else
        {
            index = Random.Range(0, slots.Length);
        }

        return slots[index].position;
    }

}
