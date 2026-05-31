using Units;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCapture : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    UnitOwner owner = UnitOwner.World;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void setFill(float capturePercent, UnitOwner unitOwner){

            print("set fill with " + capturePercent + " percent");
            
            if(owner != unitOwner){
                owner = unitOwner;
                if(owner == UnitOwner.Player){
                    transform.Find("Canvas").Find("Fill").GetComponent<Image>().color = Color.blue;
                }else if(owner == UnitOwner.Enemy){
                    transform.Find("Canvas").Find("Fill").GetComponent<Image>().color = Color.red;
                }else{
                    transform.Find("Canvas").Find("Fill").GetComponent<Image>().color = Color.grey;
                }
            }
            

            float value = Mathf.InverseLerp(0f, 100f, capturePercent);
            fillImage.fillAmount = value;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
