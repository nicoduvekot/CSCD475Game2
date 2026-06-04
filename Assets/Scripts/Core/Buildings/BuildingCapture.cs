using Units;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCapture : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    
    private bool _hasLazyInit;
    
    private void LazyInit()
    {
        if (fillImage == null)
            fillImage = transform.Find("Canvas/Fill").GetComponent<Image>();
        
        if (backgroundImage == null)
            backgroundImage = transform.Find("Canvas/Background").GetComponent<Image>();
        
        if (fillImage == null || backgroundImage == null)
        {
            Debug.LogError("BuildingCapture: Missing Fill or Background image. Check prefab hierarchy.");
            return;
        }

        _hasLazyInit = true;
    }

    public void setFill(float controlPercent, UnitOwner owner)
    {
        if (!_hasLazyInit)
            LazyInit();
        
        // set background to be the current Owner
        if (owner == UnitOwner.Player)
            backgroundImage.color = Color.blue;
        else if (owner == UnitOwner.Enemy)
            backgroundImage.color = Color.red;
        else
            backgroundImage.color = Color.grey;
        
        float fillAmount = 0f;
        Color fillColor = Color.grey;
        
        switch (owner)
        {
            case UnitOwner.Player:
                // Enemy progress from +100 → -100
                // +100 => 0, 0 => 0.5, -100 => 1
                fillAmount = (100f - controlPercent) / 200f;
                if (fillAmount > 0f)
                    fillColor = Color.red; // enemy capturing
                break;

            case UnitOwner.Enemy:
                // Player progress from -100 → +100
                // -100 => 0, 0 => 0.5, +100 => 1
                fillAmount = (controlPercent + 100f) / 200f;
                if (fillAmount > 0f)
                    fillColor = Color.blue; // player capturing
                break;

            case UnitOwner.World:
                // Neutral: progress is just distance from 0 toward a side
                if (controlPercent > 0f)
                {
                    fillAmount = controlPercent / 100f;   // 0→100
                    fillColor = Color.blue;
                }
                else if (controlPercent < 0f)
                {
                    fillAmount = -controlPercent / 100f;  // 0→100
                    fillColor = Color.red;
                }
                else
                {
                    fillAmount = 0f;
                    fillColor = Color.clear;
                }
                break;
        }
        
        fillAmount = Mathf.Clamp01(fillAmount);
        fillImage.color = fillColor;
        fillImage.fillAmount = fillAmount;
    }
}
