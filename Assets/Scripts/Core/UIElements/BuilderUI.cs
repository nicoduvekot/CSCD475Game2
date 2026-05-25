using System;
using Selection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DataDefinitions;

namespace Core.UIElements
{
    [Obsolete("Builder is obsolete, so it's UI is as well", true)]
    public class BuilderUI : MonoBehaviour
    {
        public static BuilderUI Instance { get; private set; }
        
        [SerializeField] private GameObject panel;
        
        [Header("Header Label")]
        [SerializeField] private TextMeshProUGUI headerLabel;
        
        [Header("Buttons")]
        [SerializeField] private Button buildFarmButton;
        [SerializeField] private TextMeshProUGUI buildFarmLabel;

        [SerializeField] private Button buildSawmillButton;
        [SerializeField] private TextMeshProUGUI buildSawmillLabel;
        
        [Header("Demolish")]
        [SerializeField] private Button demolishButton;

        [Header("Building Data Options")]
        [SerializeField] private BuildingData farmData;
        [SerializeField] private BuildingData sawmillData;
        
        private TestSelectableHex _currentHex;
        private TestSelectableBuilder _currentBuilder;
        
        private void Awake()
        {
            Instance = this;
            
            buildFarmButton.onClick.AddListener(() => Build(farmData));
            buildSawmillButton.onClick.AddListener(() => Build(sawmillData));
            demolishButton.onClick.AddListener(Demolish);
            
            buildFarmLabel.text = farmData.buildingName;
            buildSawmillLabel.text = sawmillData.buildingName;
        }
        
        public void ShowOptionsFor(TestSelectableHex hex, TestSelectableBuilder builder)
        {
            _currentHex = hex;
            _currentBuilder = builder;

            panel.SetActive(true);

            if (hex.AttachedBuilding == null)
            {
                headerLabel.text = "Buildable Options";

                buildFarmButton.gameObject.SetActive(hex.ResourceType == ResourceType.Grain);
                buildSawmillButton.gameObject.SetActive(hex.ResourceType == ResourceType.Wood);

                demolishButton.gameObject.SetActive(false);
            }
            else
            {
                headerLabel.text = $"Current Building:\n{hex.AttachedBuilding.buildingName}";

                buildFarmButton.gameObject.SetActive(false);
                buildSawmillButton.gameObject.SetActive(false);

                demolishButton.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            panel.SetActive(false);
            _currentHex = null;
            _currentBuilder = null;
        }

        private void Build(BuildingData data)
        {
            if (_currentHex == null || _currentBuilder == null)
                return;

            _currentBuilder.StartBuilding(data, false);
            Hide();
        }
        
        private void Demolish()
        {
            if (_currentHex == null || _currentBuilder == null)
                return;

            _currentBuilder.StartBuilding(_currentHex.AttachedBuilding, true);
            Hide();
        }
    }
}