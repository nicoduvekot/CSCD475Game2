using UnityEngine;
using UnityEngine.UI;
public class BuildingRecruitment : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    private float timeToRecruit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    
    public void startRecruit(float timeToRecruit){
        this.timeToRecruit = timeToRecruit;
    }
    public void setFill(float timePassed){
            
            float value = Mathf.InverseLerp(0f, timeToRecruit, timePassed);
            fillImage.fillAmount = value;
    }

    // Update is called once per frame
    void Update()
    {

        
    }
}
