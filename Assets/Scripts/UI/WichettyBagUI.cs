using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WichettyBagUI : MonoBehaviour
{
    public static WichettyBagUI Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private Transform listRoot;

    [SerializeField]
    private WichettyBagUIRow itemRowPrefab;

    [SerializeField]
    private Button bagButton;

    [SerializeField]
    private GameObject panel;

    private readonly Dictionary<WichettyItem, int> counts = new();
    private readonly Dictionary<WichettyItem, WichettyBagUIRow> rows = new();

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(true);
        if (bagButton != null)
            bagButton.onClick.AddListener(TogglePanel);
    }

    private void TogglePanel()
    {
        if (panel == null)
            return;
        bool show = !panel.activeSelf;
        panel.SetActive(show);
        if (show)
            RebuildAll();
    }

    public void Add(WichettyItem item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        if (lastAddedItem == item && Time.time - lastAddTime < 0.1f)
            return;

        lastAddedItem = item;
        lastAddTime = Time.time;

        Debug.Log(
            $"[BagUI] Adding {amount}x {item.DisplayName}. Current count: {(counts.ContainsKey(item) ? counts[item] : 0)}"
        );

    
        if (!counts.ContainsKey(item))
            counts[item] = 0;

        counts[item] += amount;

        Debug.Log($"[BagUI] New total count: {counts[item]}x {item.DisplayName}");

        if (panel != null && panel.activeSelf)
            CreateOrUpdateRow(item, counts[item]);
    }

    private WichettyItem lastAddedItem;
    private float lastAddTime;

    public void Remove(WichettyItem item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        Debug.Log(
            $"[BagUI] Removing {amount}x {item.DisplayName}. Current count: {(counts.ContainsKey(item) ? counts[item] : 0)}"
        );

        if (!counts.ContainsKey(item))
        {
            Debug.LogWarning($"[BagUI] Cannot remove {item.DisplayName} - not in inventory");
            return;
        }

        counts[item] -= amount;
     if (counts[item] <= 0)
        {
            counts.Remove(item);

            if (rows.TryGetValue(item, out var row) && row != null)
                Destroy(row.gameObject);

            rows.Remove(item);

            Debug.Log($"[BagUI] Removed {item.DisplayName} completely from inventory");
        }
        else
        {
            Debug.Log($"[BagUI] New total count: {counts[item]}x {item.DisplayName}");

            if (panel != null && panel.activeSelf && rows.TryGetValue(item, out var row))
                row.SetData(item, counts[item]);
        }
    }

    private void RebuildAll()
    {
        if (listRoot == null || itemRowPrefab == null)
            return;

        foreach (Transform child in listRoot)
            Destroy(child.gameObject);

        rows.Clear();

        foreach (var kv in counts)
            CreateOrUpdateRow(kv.Key, kv.Value);
    }

    private void CreateOrUpdateRow(WichettyItem item, int count)
    {
        if (!rows.TryGetValue(item, out var row) || row == null)
        {
            var rowInstance = Instantiate(itemRowPrefab, listRoot);
            row = rowInstance;
            rows[item] = row;
        }

        row.SetData(item, count);
    }

    public int GetCount(WichettyItem item)
    {
        return counts.ContainsKey(item) ? counts[item] : 0;
    }

    public bool HasItem(WichettyItem item, int amount = 1)
    {
        return counts.ContainsKey(item) && counts[item] >= amount;
    }
}
