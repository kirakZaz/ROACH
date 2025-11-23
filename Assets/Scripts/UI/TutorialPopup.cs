using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject tutorialPanel;

    [SerializeField]
    private Button openButton;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private TextMeshProUGUI tutorialText;

    private void Start()
    {
        // Set tutorial text
        if (tutorialText)
        {
            tutorialText.text =
                "<size=32><b>HOW TO PLAY</b></size>\n\n"
                + "<b>CONTROLS:</b>\n"
                + "• <b>W A S D</b> - Move and Jump\n"
                + "• <b>Mouse Scroll</b> - Zoom Camera\n\n"
                + "<b>OBJECTIVE:</b>\n"
                + "• Jump on enemies to kill them\n"
                + "• Collect <b>10 Resources</b> from defeated enemies\n"
                + "• Find the <b>Witchetty Hole</b> (wall exit)\n"
                + "• Enter the hole to complete the level\n\n"
                + "<b>TIME LIMIT:</b>\n"
                + "• You have <b>5 minutes</b> to complete the level!\n\n"
                + "<color=yellow>Good luck!</color>";
        }

        // Hide panel at start
        if (tutorialPanel)
            tutorialPanel.SetActive(false);

        // Setup button listeners
        if (openButton)
            openButton.onClick.AddListener(OpenTutorial);

        if (closeButton)
            closeButton.onClick.AddListener(CloseTutorial);
    }

    public void OpenTutorial()
    {
        if (tutorialPanel)
        {
            tutorialPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }
    }

    public void CloseTutorial()
    {
        if (tutorialPanel)
        {
            tutorialPanel.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }
    }
}
