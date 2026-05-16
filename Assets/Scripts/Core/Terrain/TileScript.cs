using Selection;
using UnityEngine;
using Units;

public class TileScript : MonoBehaviour, ISelectable
{
    public MonoBehaviour Behaviour => this;
    private BaseUnit _occupyingUnit;
    private MonoBehaviour _occupyingBuilding;
    
    public int x;
    public int y;

    public int z;

    public GameObject tileOccupant;

    public Material green;

    public Material defaultDirt;

    private int movementPoints;

   

    public enum terrainType{
        dirt, grass, forest, mountain, water, desert, snow, building
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
        
        

        


        switch (terrain){
            case terrainType.dirt:
                movementPoints = 1;
                if(materialType != "dirt"){
                    
                    ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
                    terrain = terrainType.dirt;
                }
                break;
            case terrainType.grass:
                movementPoints = 1;
                if(materialType != "grass"){
                    
                    ground = Resources.Load("Material/grass", typeof(Material)) as Material;
                    terrain = terrainType.grass;
                }
                break;
            case terrainType.forest:
                movementPoints = 2;
                if(materialType != "forest"){
                    ground = Resources.Load("Material/forest", typeof(Material)) as Material;
                    terrain = terrainType.forest;
                }
                break;
            case terrainType.mountain:
                movementPoints = 3;
                if(materialType != "mountain"){
                    ground = Resources.Load("Material/mountain", typeof(Material)) as Material;
                    terrain = terrainType.mountain;
                }
                break;
            case terrainType.water:
                movementPoints = -1;
                if(materialType != "water"){
                    ground = Resources.Load("Material/water", typeof(Material)) as Material;
                    terrain = terrainType.water;
                }
                break;
            case terrainType.desert:
                movementPoints = 1;
                if(materialType != "desert"){
                    ground = Resources.Load("Material/desert", typeof(Material)) as Material; 
                    terrain = terrainType.desert;
                }
                break;
            case terrainType.snow:
                movementPoints = 2;
                if(materialType != "snow"){
                    ground = Resources.Load("Material/snow", typeof(Material)) as Material;
                    terrain = terrainType.snow;
                }
                break;
            case terrainType.building:
                movementPoints = -1;
                if(materialType != "building"){
                    ground = Resources.Load("Material/building", typeof(Material)) as Material;
                    terrain = terrainType.building;
                }
                break;   
        }
        
        transform.Find("Hex").GetComponent<Renderer>().material = ground;

        
    }

    public void setTerrain(string type){
        if(type == "dirt"){
            ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.dirt;
        }else if(type == "grass"){
            ground = Resources.Load("Material/grass", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.grass;
        }else if(type == "forest"){
            ground = Resources.Load("Material/forest", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.forest;
        }else if(type == "mountain"){
            ground = Resources.Load("Material/mountain", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.mountain;
        }else if(type == "water"){
            ground = Resources.Load("Material/water", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.water;
        }else if(type == "desert"){
            ground = Resources.Load("Material/desert", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.desert;
        }else if(type == "snow"){
            ground = Resources.Load("Material/snow", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.snow;
        }else if(type == "building"){
            ground = Resources.Load("Material/building", typeof(Material)) as Material;
            transform.Find("Hex").GetComponent<Renderer>().material = ground;
            terrain = terrainType.building;
        }
    }

    public terrainType getTerrain(){
        return terrain;
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

    
    
    //remove later, only used for testing
    public int getMovement()
    {
        return movementPoints;
    }
    //

    #region Additions

    public bool TryGetOccupant(out MonoBehaviour occupant)
    {
        if (_occupyingBuilding != null)
        {
            occupant = _occupyingBuilding;
            return true;
        }

        if (_occupyingUnit != null)
        {
            occupant = _occupyingUnit;
            return true;
        }

        occupant = null;
        return false;
    }
    
    public bool TrySetUnitOccupant(BaseUnit unit)
    {
        if (_occupyingBuilding != null)
        {
            Debug.LogError($"Hex {name} has a building. Units cannot occupy this hex.");
            return false;
        }

        _occupyingUnit = unit;
        return true;
    }
    
    public bool TryClearUnitOccupant(BaseUnit unit)
    {
        if (unit == null)
        {
            Debug.LogError($"Hex {name}: TryClearUnitOccupant called with null requester.");
            return false;
        }
            
        if (_occupyingUnit == null)
        {
            Debug.LogWarning($"Hex {name}: No unit to clear, but {unit.name} attempted to clear occupancy.");
            return false;
        }
            
        if (_occupyingUnit != unit)
        {
            Debug.LogError(
                $"Hex {name}: {unit.name} attempted to clear occupancy, " +
                $"but the current occupant is {_occupyingUnit.name}. Only the occupant should clear itself."
            );
            return false;
        }

        _occupyingUnit = null;
        return true;
    }

    #endregion
}
