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

    public Material defaultDirt;

    private int movementPoints;

   

    public enum terrainType{
        dirt, grass, hill, mountain, water, desert
    }

    [SerializeField]
    private terrainType terrain = terrainType.dirt;

    private string materialType = "";
    private Material ground;

    void Start()
    {

        ground = defaultDirt;

        

        
    }

    public void createHex(int x, int y, int z,terrainType incomingTerrain){
        // this.row = row;
        // this.column = column;
        this.x = x;
        this.y = y;
        this.z = z;
        
        incomingTerrain = terrainType.dirt;

        terrain = incomingTerrain;

        

        print("terrain is " + terrain);

        switch (terrain){
            case terrainType.dirt:
                movementPoints = 1;
                if(materialType != "dirt"){
                    //print(Resources.Load("Material/dirt", typeof(Material)) as Material);
                    ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
                }
                break;
            case terrainType.grass:
                movementPoints = 1;
                if(materialType != "grass"){
                    //print("grass created");
                    ground = Resources.Load("Material/grass", typeof(Material)) as Material;
                }
                break;
            case terrainType.hill:
                movementPoints = 2;
                if(materialType != "hill"){
                    ground = Resources.Load("Material/hill", typeof(Material)) as Material;
                }
                break;
            case terrainType.mountain:
                movementPoints = 3;
                if(materialType != "mountain"){
                    ground = Resources.Load("Material/mountain", typeof(Material)) as Material;
                }
                break;
            case terrainType.water:
                movementPoints = -1;
                if(materialType != "water"){
                    ground = Resources.Load("Material/water", typeof(Material)) as Material;
                }
                break;
            case terrainType.desert:
                movementPoints = 1;
                if(materialType != "desert"){
                    ground = Resources.Load("Material/desert", typeof(Material)) as Material;
                    print(Resources.Load("Material/desert", typeof(Material)) as Material);
                }
                break;
        }
        
        transform.Find("Hex").GetComponent<Renderer>().material = ground;

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

    public void setTerrain(terrainType t){

    }

    // Update is called once per frame
    
    //remove later, only used for testing
    public int getMovement()
    {
        return movementPoints;
    }
    //
}
