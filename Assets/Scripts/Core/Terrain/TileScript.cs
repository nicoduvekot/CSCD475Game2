using Selection;
using UnityEngine;
using Units;
using System.Collections.Generic;

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

    private bool hasFog = true;

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

        addFog();

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

    private void fogRemovalCalculations(){
        int viewRange = 3;

        
        

        int currentRows = viewRange + 1;
        // int rowsViewed = 0;

        int topX = 0;
        int topZ = 0 - viewRange;

        for(int topY = -viewRange; topY <= viewRange; topY++){

            
            
            if(topY != -viewRange){
                if(topY <= 0){
                    topX++;
                    currentRows++;
                }else{
                    topZ++;
                    currentRows--;
                }
            }

            for(int i = 0; i < currentRows; i++){

                //if(!(topX - i + x == x && topY + y == y)){// dont check the tile your on or it will crash
                    GameObject nextHex = MapGenerateScript.getHex(topX - i + x,topY + y,topZ + i + z);
                    if(nextHex!= null){
                        if(!canView(topX - i + x,topY + y,topZ + i + z,viewRange,false) && !nextHex.GetComponent<TileScript>().getFog()){
                            nextHex.GetComponent<TileScript>().addFog();
                        }
                    }
                //}
            }


        }
    }

    public void addBuildingOwner(UnitOwner owner){
        tileOccupant = owner;
        canView(x,y,z,3,true);
    }

    public void removeBuildingOwner(){
        tileOccupant = UnitOwner.World;
        fogRemovalCalculations();
    }

    private bool canView(int hexX, int hexY, int hexZ, int viewDistance,bool clearFog){
        
        int viewRange = viewDistance;

        int currentRows = viewRange + 1;

        int topX = 0;
        int topZ = 0 - viewRange;

        for(int topY = -viewRange; topY <= viewRange; topY++){

            
            
            if(topY != -viewRange){
                if(topY <= 0){
                    topX++;
                    currentRows++;
                }else{
                    topZ++;
                    currentRows--;
                }
            }

            for(int i = 0; i < currentRows; i++){
                
                //if(!(topX - i + hexX == hexX && topY + hexY == hexY)){// dont check the tile your on or it will crash
                    if(MapGenerateScript.getHex(topX - i + hexX,topY + hexY,topZ + i + hexZ) != null){
                        if(clearFog){
                            MapGenerateScript.getHex(topX - i + hexX,topY + hexY,topZ + i + hexZ).GetComponent<TileScript>().removeFog();
                        }else if(MapGenerateScript.getHex(topX - i + hexX,topY + hexY,topZ + i + hexZ).GetComponent<TileScript>().tileOccupant == UnitOwner.Player){
                            return true;
                        }
                    }
                //}
            }


        }

        return false;
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

        if(tileOccupant != UnitOwner.World || movementPoints == -1){
            print("tile is occupied by another player or is not an occupiable tile");
            return false;
        }


        movementPoints = -1;

        if(attachedBuilding != null){
            
            OwnerOutline.GetComponent<SpriteRenderer>().sprite = attachedBuilding.moveIntoHex(unit.Owner);
        }
       
        removeFog();
        canView(x,y,z,3,true);

        _occupyingUnit = unit;
        tileOccupant = unit.Owner;
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

        if(tileOccupant == UnitOwner.World){
            print("tileOwner is world, cannot clear tile");
            movementPoints = realMovement;
            return false;
        }

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

        

        if(OwnerOutline != null){
            OwnerOutline.GetComponent<SpriteRenderer>().sprite = attachedBuilding.moveOutOfHex(tileOccupant);
        }

        tileOccupant = UnitOwner.World;

        movementPoints = realMovement;
        
        fogRemovalCalculations();

        _occupyingUnit = null;

        return true;
    }

    #endregion

    public void addFog(){
        float height = GetComponent<MeshCollider>().bounds.size.y /1.7f;
        GameObject fog  =Instantiate(Resources.Load("Prefabs/Hidden", typeof(GameObject)) as GameObject,transform.position + new Vector3(0,height,0),transform.rotation,transform);
        fog.name = "Fog";
        hasFog = true;
    }

    public void removeFog(){
        GameObject fog = null;
        try{
            fog = transform.Find("Fog").gameObject;
        }catch (System.NullReferenceException e){
            
        }
        if(fog != null){
            Destroy(fog);
            hasFog = false;
        }
    }

    public bool getFog(){
        return hasFog;
    }

    public bool canMakeUnit(){
        if(tileOccupant == UnitOwner.World && movementPoints != -1){
            return true;
        }else{
            return false;
        }
    }

    public void makeUnit(GameObject unit){
        //Instantiate(unit,)
    }
}
