using Selection;
using UnityEngine;
using Units;

public class TileScript : MonoBehaviour, ISelectable
{

    // ISelectable requirement
    public MonoBehaviour Behaviour => this;
    private BaseUnit _occupyingUnit;
    
    // NOTES: This can change out of being a MonoBehaviour, was set up as such just to provide intended usage 
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

    #region Occupancy Operations

    /// <summary>
    /// Will give out the occupant, if there is, false return means no occupant
    ///
    /// Intended to a means of detecting occupation prior to movement
    /// </summary>
    /// <param name="occupant"></param>
    /// <returns></returns>
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
    
    
    /// <summary>
    /// Will Try to Set the unit as the occupant, false return means it failed
    ///
    /// Intended to be called by a unit trying to occupy
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Will Try to clearn the occupant from the tile. Must be the occupant who does currently occupy.
    /// False return means a failure (this will cause a LogError with reasoning to fix)
    ///
    /// Intended to be called by the occupying unit, when they no longer occupy
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public bool TryClearUnitOccupant(BaseUnit unit)
    {
        if (unit == null)
        {
            Debug.LogError($"Hex {name}: TryClearUnitOccupant called with null requester.");
            return false;
        }
            
        if (_occupyingUnit == null)
        {
            Debug.LogError($"Hex {name}: No unit to clear, but {unit.name} attempted to clear occupancy.");
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
