using UnityEngine;

[CreateAssetMenu(fileName = "New Wichetty Item", menuName = "Wichetty/Item")]
public class WichettyItem : ScriptableObject
{
    [SerializeField]
    private string displayName = "Small Rock";

    [SerializeField]
    private Sprite icon;

    [SerializeField]
    private int baseAmount = 1;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int BaseAmount => baseAmount;
}
