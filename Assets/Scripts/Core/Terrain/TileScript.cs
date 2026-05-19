using Units;
using UnityEngine;

public class TileScript : MonoBehaviour
{

    public int x;
    public int y;

    public int z;

    public UnitOwner tileOccupant;

    private int movementPoints;

    public bool buildingTile = false;
   

    

    [SerializeField]
    private TerrainType terrain = TerrainType.dirt;

    private string materialType = "";
    private Material ground;
    private int realMovement = 0;

    void Start()
    {

        ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
        
    }

    public void createHex(int x, int y, int z,TerrainType incomingTerrain){
        // this.row = row;
        // this.column = column;
        this.x = x;
        this.y = y;
        this.z = z;
        
        

        terrain = incomingTerrain;


        switch (terrain){
            case TerrainType.dirt:
                movementPoints = 1;
                if(materialType != "dirt"){
                    
                    ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
                    terrain = TerrainType.dirt;
                }
                break;
            case TerrainType.grass:
                movementPoints = 1;
                if(materialType != "grass"){
                    
                    ground = Resources.Load("Material/grass", typeof(Material)) as Material;
                    terrain = TerrainType.grass;
                }
                break;
            case TerrainType.forest:
                movementPoints = 2;
                if(materialType != "forest"){
                    ground = Resources.Load("Material/forest", typeof(Material)) as Material;
                    terrain = TerrainType.forest;
                }
                break;
            case TerrainType.mountain:
                movementPoints = 3;
                if(materialType != "mountain"){
                    ground = Resources.Load("Material/mountain", typeof(Material)) as Material;
                    terrain = TerrainType.mountain;
                }
                break;
            case TerrainType.water:
                movementPoints = -1;
                if(materialType != "water"){
                    ground = Resources.Load("Material/water", typeof(Material)) as Material;
                    terrain = TerrainType.water;
                }
                break;
            case TerrainType.desert:
                movementPoints = 1;
                if(materialType != "desert"){
                    ground = Resources.Load("Material/desert", typeof(Material)) as Material; 
                    terrain = TerrainType.desert;
                }
                break;
            case TerrainType.snow:
                movementPoints = 2;
                if(materialType != "snow"){
                    ground = Resources.Load("Material/snow", typeof(Material)) as Material;
                    terrain = TerrainType.snow;
                }
                break;
            case TerrainType.building:
                movementPoints = -1;
                if(materialType != "building"){
                    ground = Resources.Load("Material/building", typeof(Material)) as Material;
                    terrain = TerrainType.building;
                }

                

                break;   
        }
        realMovement = movementPoints;
        transform.Find("Hex").GetComponent<Renderer>().material = ground;

        
    }

    public void setTerrain(string type){
        if(type == "dirt"){
            ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.dirt;
        }else if(type == "grass"){
            ground = Resources.Load("Material/grass", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.grass;
        }else if(type == "forest"){
            ground = Resources.Load("Material/forest", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.forest;
        }else if(type == "mountain"){
            ground = Resources.Load("Material/mountain", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.mountain;
        }else if(type == "water"){
            ground = Resources.Load("Material/water", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.water;
        }else if(type == "desert"){
            ground = Resources.Load("Material/desert", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.desert;
        }else if(type == "snow"){
            ground = Resources.Load("Material/snow", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.snow;
        }else if(type == "building"){
            ground = Resources.Load("Material/building", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = TerrainType.building;

        }
        realMovement = movementPoints;
    }

    public GameObject createBuilding(string resource){// only to be used for initial creation of the buildings must be called after map is set up
        GameObject tempBuilding;
        if(!buildingTile){
            tempBuilding = Instantiate(Resources.Load("Prefabs/Building", typeof(GameObject)) as GameObject,transform.position ,transform.rotation,transform);
            tempBuilding.GetComponent<BuildingScript>().createBuilding(gameObject,resource);
            tempBuilding.name = "Building";
            buildingTile = true;
        }else{
            tempBuilding = transform.Find("Building").gameObject;
            tempBuilding.GetComponent<BuildingScript>().createBuilding(gameObject,resource);
        }
        return tempBuilding;
    }

    public TerrainType getTerrain(){
        return terrain;
    }

    
    

    public UnitOwner getOccupant(){
        return tileOccupant;
    }

    public UnitOwner removeOccupant(){
        tileOccupant = UnitOwner.World;
        UnitOwner r = tileOccupant;
        movementPoints = realMovement;
        return r;
    }

    public bool addOccupant(UnitOwner incomingOccupant){
        movementPoints = -1;
        if(tileOccupant != UnitOwner.World){
            return false;
        }else{
            tileOccupant = incomingOccupant;
            return true;
        }
        
    }

    
    
    //remove later, only used for testing
    public int getMovement()
    {
        return movementPoints;
    }
    //
}
