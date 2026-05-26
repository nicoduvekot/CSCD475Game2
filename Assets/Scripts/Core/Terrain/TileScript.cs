using Selection;
using UnityEngine;
using Units;
using System.Collections.Generic;
using TeamControl;

public class TileScript : MonoBehaviour, ISelectable
{

    // ISelectable requirement
    public MonoBehaviour Behaviour => this;
    private BaseUnit _occupyingUnit;
    
    // this change is part of Nico re-write
    public BaseUnit OccupyingUnit => _occupyingUnit;
    
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
    
    
    public bool fogForPlayer = true;
    public bool fogForEnemy = true;
    public bool fogForWorld = true;
    
    private int revealCountPlayer = 0;
    private int revealCountEnemy = 0;
    private int revealCountWorld = 0;

    private int viewRange = 3;

    void Start()
    {
        ground = Resources.Load("Material/dirt", typeof(Material)) as Material;
     
        PerspectiveManager.Instance.OnPerspectiveChanged += _ => UpdateVisibilityForCurrentPerspective();
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

    // private void fogRemovalCalculations(){
    //     int viewRange = 3;
    //
    //     
    //     
    //
    //     int currentRows = viewRange + 1;
    //     // int rowsViewed = 0;
    //
    //     int topX = 0;
    //     int topZ = 0 - viewRange;
    //
    //     for(int topY = -viewRange; topY <= viewRange; topY++){
    //
    //         
    //         
    //         if(topY != -viewRange){
    //             if(topY <= 0){
    //                 topX++;
    //                 currentRows++;
    //             }else{
    //                 topZ++;
    //                 currentRows--;
    //             }
    //         }
    //
    //         for(int i = 0; i < currentRows; i++){
    //
    //             //if(!(topX - i + x == x && topY + y == y)){// dont check the tile your on or it will crash
    //                 GameObject nextHex = MapGenerateScript.getHex(topX - i + x,topY + y,topZ + i + z);
    //                 if(nextHex!= null){
    //                     if(!canView(topX - i + x,topY + y,topZ + i + z,viewRange,false) && !nextHex.GetComponent<TileScript>().getFog()){
    //                         nextHex.GetComponent<TileScript>().addFog();
    //                     }
    //                 }
    //             //}
    //         }
    //
    //
    //     }
    // }

    public void addBuildingOwner(UnitOwner owner){
        tileOccupant = owner;
        
        RevealForOwner(owner);
        RevealAround(owner);
    }

    public void removeBuildingOwner(){
        UnitOwner oldOwner = tileOccupant;
        
        tileOccupant = UnitOwner.World;
        
        HideForOwner(oldOwner);
        HideAround(oldOwner);
    }

    // private bool canView(int hexX, int hexY, int hexZ, int viewDistance,bool clearFog){
    //     
    //     int viewRange = viewDistance;
    //
    //     int currentRows = viewRange + 1;
    //
    //     int topX = 0;
    //     int topZ = 0 - viewRange;
    //
    //     for(int topY = -viewRange; topY <= viewRange; topY++){
    //
    //         
    //         
    //         if(topY != -viewRange){
    //             if(topY <= 0){
    //                 topX++;
    //                 currentRows++;
    //             }else{
    //                 topZ++;
    //                 currentRows--;
    //             }
    //         }
    //
    //         for(int i = 0; i < currentRows; i++){
    //             
    //             //if(!(topX - i + hexX == hexX && topY + hexY == hexY)){// dont check the tile your on or it will crash
    //                 if(MapGenerateScript.getHex(topX - i + hexX,topY + hexY,topZ + i + hexZ) != null){
    //                     if(clearFog){
    //                         MapGenerateScript.getHex(topX - i + hexX,topY + hexY,topZ + i + hexZ).GetComponent<TileScript>().removeFog();
    //                     }else if(MapGenerateScript.getHex(topX - i + hexX,topY + hexY,topZ + i + hexZ).GetComponent<TileScript>().tileOccupant == UnitOwner.Player){
    //                         return true;
    //                     }
    //                 }
    //             //}
    //         }
    //
    //
    //     }
    //
    //     return false;
    // }

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


        // this change is part of Nico re-write
        //movementPoints = -1;

        if(attachedBuilding != null){
            
            OwnerOutline.GetComponent<SpriteRenderer>().sprite = attachedBuilding.moveIntoHex(unit.Owner);
        }
       
        RevealForOwner(unit.Owner);
        RevealAround(unit.Owner);

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
        
        HideForOwner(tileOccupant);
        HideAround(tileOccupant);

        tileOccupant = UnitOwner.World;

        movementPoints = realMovement;

        _occupyingUnit = null;

        return true;
    }

    public bool TryUpdateUnitOwnership(BaseUnit unit)
    {
        if (_occupyingUnit == null)
        {
            Debug.LogError("[TileScript] Update ownership failed. No unit occupies this tile.");
            return false;
        }
        
        if (_occupyingUnit != unit)
        {
            Debug.LogError("[TileScript] Update Ownership failed. Wrong unit tried to update");
            return false;
        }
        
        // retrieve the new ownership value
        UnitOwner newOwner = unit.Owner;
        
        // if it is the same as it was, nothing needs to happen
        if (tileOccupant == newOwner)
            return true;
        
        // cache the old ownership value
        UnitOwner oldOwner = tileOccupant;
        
        // clear old owner from seeing
        HideForOwner(oldOwner);
        HideAround(oldOwner);
        
        // set the new owner
        tileOccupant = newOwner;
        
        // set the new owner as seeing
        RevealForOwner(newOwner);
        RevealAround(newOwner);

        return true;
    }

    #endregion

    public void addFog()
    {
        float height = GetComponent<MeshCollider>().bounds.size.y /1.7f;
        
        Transform fog = transform.Find("Fog");
        if (fog == null)
        {
            fog = Instantiate(
                Resources.Load("Prefabs/Hidden", typeof(GameObject)) as GameObject,
                transform.position + new Vector3(0, height, 0),
                transform.rotation,
                transform
            ).transform;
            fog.name = "Fog";
        }
        
        fogForPlayer = true;
        fogForEnemy = true;
        revealCountPlayer = 0;
        revealCountEnemy = 0;
        
        UpdateVisibilityForCurrentPerspective();
    }

    public void removeFog(UnitOwner owner)
    {
        HideForOwner(owner);
    }

    public bool getFog()
    {
        Perspective p = PerspectiveManager.Instance.CurrentPerspective;

        return p switch
        {
            Perspective.Player => fogForPlayer,
            Perspective.Enemy  => fogForEnemy,
            Perspective.World  => fogForWorld,
            Perspective.Admin  => false,
            _ => false
        };
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

    private void RevealForOwner(UnitOwner owner)
    {
        switch (owner)
        {
            case UnitOwner.Player:
                revealCountPlayer++;
                fogForPlayer = revealCountPlayer <= 0;
                break;

            case UnitOwner.Enemy:
                revealCountEnemy++;
                fogForEnemy = revealCountEnemy <= 0;
                break;

            case UnitOwner.World:
                revealCountWorld++;
                fogForWorld = revealCountWorld <= 0;
                break;
        }

        UpdateVisibilityForCurrentPerspective();
    }

    private void HideForOwner(UnitOwner owner)
    {
        switch (owner)
        {
            case UnitOwner.Player:
                revealCountPlayer = Mathf.Max(0, revealCountPlayer - 1);
                fogForPlayer = revealCountPlayer <= 0;
                break;

            case UnitOwner.Enemy:
                revealCountEnemy = Mathf.Max(0, revealCountEnemy - 1);
                fogForEnemy = revealCountEnemy <= 0;
                break;

            case UnitOwner.World:
                revealCountWorld = Mathf.Max(0, revealCountWorld - 1);
                fogForWorld = revealCountWorld <= 0;
                break;
        }
        
        UpdateVisibilityForCurrentPerspective();
    }

    private void RevealAround(UnitOwner owner)
    {
        List<TileScript> tiles = GetTilesInRange(viewRange);

        foreach (TileScript tile in tiles)
            tile.RevealForOwner(owner);
    }
    
    private void HideAround(UnitOwner owner)
    {
        List<TileScript> tiles = GetTilesInRange(viewRange);

        foreach (TileScript tile in tiles)
            tile.HideForOwner(owner);
    }

    private List<TileScript> GetTilesInRange(int range)
    {
        List<TileScript> results = new();
        
        int currentRows = range + 1;
        
        int topX = 0;
        int topZ = 0 - range;

        for (int topY = -range; topY <= range; topY++)
        {
            if (topY != -range)
            {
                if (topY <= 0)
                {
                    topX++;
                    currentRows++;
                }
                else
                {
                    topZ++;
                    currentRows--;
                }
            }

            for (int i = 0; i < currentRows; i++)
            {
                int hx = topX - i + x;
                int hy = topY + y;
                int hz = topZ + i + z;

                GameObject hexObj = MapGenerateScript.getHex(hx, hy, hz);
                if (hexObj != null)
                {
                    TileScript tile = hexObj.GetComponent<TileScript>();
                    if (tile != null)
                        results.Add(tile);
                }
            }
        }

        return results;
    }

    public void UpdateVisibilityForCurrentPerspective()
    {
        Perspective p = PerspectiveManager.Instance.CurrentPerspective;
        
        bool showFog = p switch
        {
            Perspective.Player => fogForPlayer,
            Perspective.Enemy  => fogForEnemy,
            Perspective.World  => fogForWorld,
            Perspective.Admin  => false,
            _ => false
        };

        Transform fog = transform.Find("Fog");
        if (fog != null)
            fog.gameObject.SetActive(showFog);
        
        if (_occupyingUnit != null)
            _occupyingUnit.UpdateVisibility(p);
    }
}
