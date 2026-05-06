using UnityEngine;

public class TileScript : MonoBehaviour
{

    private int x;
    private int y;

    private int z;

    private int row;
    private int column;

    public Material green;

    public Material brown;

    void Start()
    {
        
    }

    public void createHex(int row, int column){
        this.row = row;
        this.column = column;
    }

    public void setGreen(bool switchGreen){
        if(switchGreen){
            transform.Find("Hex").GetComponent<Renderer>().material = green;
        }else{
            transform.Find("Hex").GetComponent<Renderer>().material = brown;
        }
    }

    // Update is called once per frame
    

}
