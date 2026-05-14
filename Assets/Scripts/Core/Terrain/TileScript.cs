using UnityEngine;

public class TileScript : MonoBehaviour
{

    public int x;
    public int y;

    public int z;

    public GameObject tileOccupant;

    public Material green;

    public Material defaultDirt;

    private int movementPoints;

   

    public enum terrainType{
        dirt, grass, forest, mountain, water, desert, snow
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


        switch (terrain){
            case terrainType.dirt:
                movementPoints = 1;
                if(materialType != "dirt"){
                    
                    ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
                }
                break;
            case terrainType.grass:
                movementPoints = 1;
                if(materialType != "grass"){
                    
                    ground = Resources.Load("Material/grass", typeof(Material)) as Material;
                }
                break;
            case terrainType.forest:
                movementPoints = 2;
                if(materialType != "forest"){
                    ground = Resources.Load("Material/forest", typeof(Material)) as Material;
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
                }
                break;
            case terrainType.snow:
                movementPoints = 2;
                if(materialType != "snow"){
                    ground = Resources.Load("Material/snow", typeof(Material)) as Material;
                }
                break;    
        }
        
        transform.Find("Hex").GetComponent<Renderer>().material = ground;

        
    }

    public void setTerrain(string type){
        if(type == "dirt"){
            ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
        }else if(type == "grass"){
            ground = Resources.Load("Material/grass", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
        }else if(type == "forest"){
            ground = Resources.Load("Material/forest", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
        }else if(type == "mountain"){
            ground = Resources.Load("Material/mountain", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
        }else if(type == "water"){
            ground = Resources.Load("Material/water", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
        }else if(type == "desert"){
            ground = Resources.Load("Material/desert", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
        }else if(type == "snow"){
            ground = Resources.Load("Material/snow", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
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

    
    

}
