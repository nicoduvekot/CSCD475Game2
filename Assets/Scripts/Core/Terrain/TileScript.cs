using Units;
using UnityEngine;

public class TileScript : MonoBehaviour
{

    public int x;
    public int y;

    public int z;

    private UnitOwner tileOccupant = UnitOwner.World;
    private GameObject OwnerOutline;
    private BuildingScript attachedBuilding;
  

    public bool buildingTile = false;
   

    

    [SerializeField]
    private TerrainType terrain = TerrainType.dirt;

    private string materialType = "";
    private Material ground;

    //static value, will always be the cost to move on this terrain type
    private int realMovement = 0;

    // dynamic value, will be changed based on what is on the tile
    private int movementPoints;

    void Start()
    {
        
        ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
        
    }

    // initialize the hexes values
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

    // sets the terrain of the tile after creation
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
        
    }

    public GameObject createBuilding(string resource){//must be called after map is set up
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
        attachedBuilding = tempBuilding.GetComponent<BuildingScript>();
        return tempBuilding;
    }

    public TerrainType getTerrain(){
        return terrain;
    }

    
    

    public UnitOwner getOccupant(){
        return tileOccupant;
    }

    public UnitOwner removeOccupant(){

        if(tileOccupant == UnitOwner.World){
            movementPoints = realMovement;
            return UnitOwner.World;
        }

        OwnerOutline.GetComponent<SpriteRenderer>().sprite = attachedBuilding.moveOutOfHex(tileOccupant);

        UnitOwner r = tileOccupant;

        tileOccupant = UnitOwner.World;

        print("real movement is " + realMovement);

        movementPoints = realMovement;

        return r;
    }

    public bool addOccupant(UnitOwner incomingOccupant){

        if(tileOccupant != UnitOwner.World || movementPoints == -1){
            print("tile occupied by " + tileOccupant);
            print("movement points were " + movementPoints);
            return false;
        }
        
        movementPoints = -1;

        if(attachedBuilding != null){
            
            OwnerOutline.GetComponent<SpriteRenderer>().sprite = attachedBuilding.moveIntoHex(incomingOccupant);
        }
       
        tileOccupant = incomingOccupant;

        if(tileOccupant != UnitOwner.World){
            return false;
        }else{
            tileOccupant = incomingOccupant;
            return true;
        }
        
        
    }

    // this adds the hex overlay to view who controls the tile, used on and around buildings
    public void addOverlay(Sprite sprite){
        if(OwnerOutline == null){
            float height = GetComponent<MeshCollider>().bounds.size.y /1.98f;
            OwnerOutline = Instantiate(Resources.Load("Prefabs/Owner", typeof (GameObject)) as GameObject,transform.position + new Vector3(0,height,0),Quaternion.Euler(new Vector3(90,0,0)),transform);
        }
        OwnerOutline.GetComponent<SpriteRenderer>().sprite = sprite;
        
    }

    public void addInitialOverlay(Sprite sprite,BuildingScript incomingBuilding){
        
        float height = GetComponent<MeshCollider>().bounds.size.y /1.98f;
        OwnerOutline = Instantiate(Resources.Load("Prefabs/Owner", typeof (GameObject)) as GameObject,transform.position + new Vector3(0,height,0),Quaternion.Euler(new Vector3(90,0,0)),transform);
        attachedBuilding = incomingBuilding;
        OwnerOutline.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    
    
    //remove later, only used for testing
    public int getMovement()
    {
        return movementPoints;
    }
    //
}
