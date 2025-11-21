using System;
using System.Collections.Generic;
using UnityEngine;

public class WichettyInventory : MonoBehaviour
{
    private readonly Dictionary<WichettyItem, int> items = new Dictionary<WichettyItem, int>();

    public event Action<WichettyItem, int> OnItemChanged;

    public void Add(WichettyItem item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        if (!items.ContainsKey(item))
            items[item] = 0;

        items[item] += amount;
        OnItemChanged?.Invoke(item, items[item]);
    }

    public IReadOnlyDictionary<WichettyItem, int> GetAll() => items;
}
