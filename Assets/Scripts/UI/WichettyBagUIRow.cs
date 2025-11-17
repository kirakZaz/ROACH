using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WichettyBagUIRow : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text countText;

    [SerializeField]
    private Image backgroundImage;

    [Header("Colors")]
    [SerializeField]
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [SerializeField]
    private Color textColor = Color.white;

    [SerializeField]
    private Color nameTextColor = Color.white;

    private int total = 0;

    private void Start()
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        if (nameText != null)
            nameText.color = nameTextColor;

        if (countText != null)
            countText.color = textColor;
    }

    public void SetData(WichettyItem item, int amount)
    {
        if (item == null)
            return;

        if (iconImage != null)
            iconImage.sprite = item.Icon;
        if (nameText != null)
            nameText.text = item.DisplayName;

        total = Mathf.Max(0, amount);
        UpdateCountText();
    }

    public void AddCount(int amount)
    {
        total += amount;
        if (total < 0)
            total = 0;
        UpdateCountText();
    }

    private void UpdateCountText()
    {
        if (countText != null)
            countText.text = $"x{total}";
    }
}
