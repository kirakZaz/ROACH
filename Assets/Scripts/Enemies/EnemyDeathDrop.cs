using UnityEngine;

public class EnemyDeathDrop : MonoBehaviour
{
    [SerializeField] private GameObject ediblePrefab;    // assign Edible_Food prefab
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, 0.1f);

    // Call this from your enemy's death method/animation event.
    public void SpawnDrop()
    {
        if (!ediblePrefab) return;
        Instantiate(ediblePrefab, (Vector2)transform.position + dropOffset, Quaternion.identity);
    }
}
