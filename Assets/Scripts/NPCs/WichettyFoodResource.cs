using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WichettyFoodResource : MonoBehaviour
{
    [SerializeField]
    private WichettyItem item;

    [SerializeField]
    private int amount = 1;

    [SerializeField]
    private float lifetime = 10f;

    public WichettyItem Item => item;
    public int Amount => amount;

    private bool isBeingEaten = false;
    public bool IsBeingEaten
    {
        get => isBeingEaten;
        set => isBeingEaten = value;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }
}
