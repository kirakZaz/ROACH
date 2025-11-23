using TMPro;
using UnityEngine;

public class TutorialText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI instructionText;

    [SerializeField]
    private string message = "Press W to Jump!";

    [SerializeField]
    private float displayTime = 3f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (instructionText)
            {
                instructionText.text = message;
                instructionText.gameObject.SetActive(true);
                Invoke("HideText", displayTime);
            }
        }
    }

    private void HideText()
    {
        if (instructionText)
            instructionText.gameObject.SetActive(false);
    }
}
