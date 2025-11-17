using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class WichettyCollector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private WichettyBagUI wichettyBagUI;

    [Header("Filter")]
    [SerializeField]
    private LayerMask pickupLayers = ~0; // all by default

    [Header("Audio (Optional)")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip collectSound;

    [Range(0f, 1f)]
    [SerializeField]
    private float collectVolume = 0.5f;

    private Collider2D triggerCol;

    private void Awake()
    {
        if (wichettyBagUI == null)
        {
            wichettyBagUI = WichettyBagUI.Instance 
                            ?? FindAnyObjectByType<WichettyBagUI>(FindObjectsInactive.Include);
        }

        if (wichettyBagUI == null)
        {
            Debug.LogWarning("[WichettyCollector] WichettyBagUI not found in scene.");
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        triggerCol = GetComponent<Collider2D>();
        if (triggerCol != null) triggerCol.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // layer filter
        if (((1 << other.gameObject.layer) & pickupLayers) == 0)
            return;

        if (!other.TryGetComponent(out WichettyFoodResource resource))
            return;
        if (!resource || !resource.gameObject.activeInHierarchy)
            return;

        // НЕ подбираем если пет ест!
        Debug.Log($"[Collector] Found resource, IsBeingEaten = {resource.IsBeingEaten}");
        if (resource.IsBeingEaten)
        {
            Debug.Log($"[Collector] Skipping - пет уже ест!");
            return;
        }

        Collect(resource);
    }

    private void Collect(WichettyFoodResource resource)
    {
        var item = resource.Item;
        var amount = resource.Amount > 0 ? resource.Amount : 1;

        if (item == null)
        {
            Debug.LogWarning("[Collector] Item is null on resource.");
            return;
        }

        if (wichettyBagUI != null)
        {
            Debug.Log($"[Collector] +{amount} x {item.DisplayName}");
            wichettyBagUI.Add(item, amount);
        }
        else
        {
            Debug.LogWarning("[Collector] BagUI is null");
        }

        if (audioSource && collectSound)
            audioSource.PlayOneShot(collectSound, collectVolume);

        Destroy(resource.gameObject);
    }
}