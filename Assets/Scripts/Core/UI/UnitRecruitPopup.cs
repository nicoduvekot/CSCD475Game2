using UnityEngine;
using TMPro;                // Used for buttons
using UnityEngine.UI;       // Also used for buttons
using Unity.Collections;
using System.Collections.Generic;

public class UnitRecruitPopup : MonoBehaviour
{
    public static UnitRecruitPopup Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Button returnButton;
    [SerializeField] private Button horsemanButton;
    [SerializeField] private Button soldierButton;
    [SerializeField] private Button archerButton;

    [SerializeField] private TMP_Text QueueDisplay;

    // [SerializeField] private Animator horseman;
    // [SerializeField] private Animator soldier;
    // [SerializeField] private Animator archer;

    private BuildingScript building;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null) canvasGroup = transform.Find("RecruitmentCanvas").GetComponent<CanvasGroup>();

        // runtime hookup so OnClick works even if Inspector won't show the method
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(onReturnClick);
        }

        if(horsemanButton != null)
        {
            horsemanButton.onClick.RemoveAllListeners();
            horsemanButton.onClick.AddListener(onHorsemanClick);
        }

        if(soldierButton != null)
        {
            soldierButton.onClick.RemoveAllListeners();
            soldierButton.onClick.AddListener(onSoldierClick);
        }

        if (archerButton != null)
        {
            archerButton.onClick.RemoveAllListeners();
            archerButton.onClick.AddListener(onArcherClick);
        }

        hide();
    }

    void Update()
    {
        // Checks to see what the UI for the queue is
        if (canvasGroup.interactable == true && building != null)
        {
            string temp = "";
            Queue<int> queue = building.getUnitProductionQueue();

            if (queue.Count > 0) {
                while (queue.Count > 0)
                {
                    if (queue.Dequeue() == 0)
                    {
                        temp += "S, ";
                    }
                    else if (queue.Dequeue() == 1)
                    {
                        temp += "A, ";
                    }
                    else if (queue.Dequeue() == 2)
                    {
                        temp += "H, ";
                    }
                }

                QueueDisplay.text = temp.Substring(0, temp.Length - 2);
            }
        }
    }

    private void hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void show(BuildingScript build)
    {
        building = build;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.SetActive(true);

        // Play the code for the animators

    }

    public void onReturnClick()
    {
        hide();
    }

    private void onHorsemanClick()
    {
        building.recruitUnit(2);
    }

    private void onSoldierClick()
    {
        building.recruitUnit(0);
    }

    private void onArcherClick()
    {
        building.recruitUnit(1);
    }
}
