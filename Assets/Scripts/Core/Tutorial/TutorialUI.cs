using UnityEngine;
using TMPro;                // Used for buttons
using UnityEngine.UI;       // Also used for buttons

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    [SerializeField] private TMP_Text tutorialPopupText;
    [SerializeField] private TMP_Text pageNumberText;

    private int page = 0;

    private string[] book = new string[]
    {
        "My liege you need to practice battle field control and get ready to lead us to victory! As this is practice and we are trying to help you get ready for combat the enemies are not going to attack us so feel free to read and get used to the controls.",
        "Frequently you get resources from the places we control. Since you have the capital we have a small stipend on resources we can use for battle.",
        "My liege you can select the fort to recruit units. Horseman are fast and can take points very well. Archers can shoot at things from range. And Soldiers are tough and great at holding places.",
        "My liege you can choose a unit by left clicking it. You can right click the unit to then order it. You can order it to move to a point or attack a unit.",
        "My liege you should try to take a point. If you move a unit next to a building you can capture it. To capture you must have more units then the enemy to capture a point.", 
        "Different points give us different rewards. Some give us more resources while we hold them while if we get another fort we can recruit units there.",
        "My liege we can research to make our units and gathering more effective. Click on the research button and see all of the options.",
        "To win the game you need to either take the opponents capital or have more points then the enemy. But that shouldn’t be a problem for you my liege."
    };

    void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(next);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(prev);
        }


        tutorialPopupText.text = book[0];
        pageNumber();
    }

    // Used to move to the next page
    private void next()
    {
        if (page < book.Length - 1)
        {
            page++;
            tutorialPopupText.text = book[page];
            pageNumber();
        }
    }

    // Used to move ot the previous page
    private void prev()
    {
        if(page > 0)
        {
            page--;
            tutorialPopupText.text = book[page];
            pageNumber();
        }
    }

    // Used to set the page number
    private void pageNumber()
    {
        int temp = page + 1;
        pageNumberText.text = temp.ToString();
    }
}
