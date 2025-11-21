using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WichettyFoodResource : MonoBehaviour
{
    [SerializeField]
    private WichettyItem item;

    [SerializeField, Min(1)]
    private int amount = 1;

    private bool isBeingEaten = false;

    public WichettyItem Item
    {
        get => item;
        set => item = value;
    }

    public int Amount
    {
        get => amount;
        set => amount = Mathf.Max(1, value);
    }

    public bool IsBeingEaten
    {
        get => isBeingEaten;
        set => isBeingEaten = value;
    }

}