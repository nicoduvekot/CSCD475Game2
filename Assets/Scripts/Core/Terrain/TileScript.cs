using UnityEngine;

public class TileScript : MonoBehaviour
{

    public int x;
    public int y;

    public int z;

    public GameObject tileOccupant;
    // private int row;
    // private int column;

    public Material green;

    public Material brown;

    void Start()
    {
        
    }

    public void createHex(int x, int y, int z){
        // this.row = row;
        // this.column = column;
        this.x = x;
        this.y = y;
        this.z = z;
        if(x == 0 || y == 0 || z ==0){
            setGreen(true);
        }
    }

    public void setGreen(bool switchGreen){
        if(switchGreen){
            transform.Find("Hex").GetComponent<Renderer>().material = green;
        }else{
            //transform.Find("Hex").GetComponent<Renderer>().material = brown;
        }
    }

    public GameObject getOccupant(){
        return tileOccupant;
    }

    public GameObject removeOccupant(){
        GameObject r = tileOccupant;
        return r;
    }

    public bool addOccupant(GameObject incomingOccupant){
        if(tileOccupant != null){
            return false;
        }else{
            tileOccupant = incomingOccupant;
            return true;
        }
    }

    // Update is called once per frame
    

}
