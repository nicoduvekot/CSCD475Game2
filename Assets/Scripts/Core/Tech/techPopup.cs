using UnityEngine;
using TMPro;       //used for buttons
using UnityEngine.UI;
using Units;       // Imports the name space for the enum types. 

public class techPopup : MonoBehaviour
{
    // Singleton Instance
    public static techPopup Instance { get; private set; }

    // Canvas Objects
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button unit0UpgradeButton;
    [SerializeField] private Button unit1UpgradeButton;
    [SerializeField] private Button unit2UpgradeButton;
    [SerializeField] private Button resourceUpgradeButton;

    [SerializeField] private TMP_Text unit0UpgradeText;
    [SerializeField] private TMP_Text unit1UpgradeText;
    [SerializeField] private TMP_Text unit2UpgradeText;
    [SerializeField] private TMP_Text resourceUpgradeText;

    // Setup for the singleton instatiation
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null) canvasGroup = transform.Find("Canvas Tech Popup").GetComponent<CanvasGroup>(); ;

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup for the buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(close);

        if (unit0UpgradeButton != null)
            unit0UpgradeButton.onClick.AddListener(() => upgradeUnit(0));

        if (unit1UpgradeButton != null)
            unit1UpgradeButton.onClick.AddListener(() => upgradeUnit(1));

        if (unit2UpgradeButton != null)
            unit2UpgradeButton.onClick.AddListener(() => upgradeUnit(2));

        if (resourceUpgradeButton != null)
            resourceUpgradeButton.onClick.AddListener(upgradeResource);

        Hide();
    }

    void Update()
    {
        updateTexts();
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void Show(string text = null)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    private void upgradeUnit(int type)
    {
        TechController.Instance.upgradeUnit(UnitOwner.Player, type);
    }

    private void upgradeResource()
    {
        TechController.Instance.upgradeResource(UnitOwner.Player);
    }

    private void close()
    {
        Hide();
    }

    private void updateTexts()
    {
        unit0UpgradeText.text    = "Food: " + TechController.Instance.getUnitCost(UnitOwner.Player, 0);
        unit1UpgradeText.text    = "Iron: " + TechController.Instance.getUnitCost(UnitOwner.Player, 1);
        unit2UpgradeText.text    = "Wood: " + TechController.Instance.getUnitCost(UnitOwner.Player, 2);
        resourceUpgradeText.text = "All Resources: " + TechController.Instance.getResourceCost(UnitOwner.Player);
    }
}
