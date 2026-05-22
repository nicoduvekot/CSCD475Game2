using UnityEngine;
using System.Collections.Generic;
using Units;

public class BuildingScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private enum resourceType{
        Wood, Iron, Food, Fort
    }

    private bool isFort = false;
    private UnitOwner controller = (UnitOwner)2; // 2 for neutral, anything else is player num will be filled in even if control% isen't 100

    //public int captureTime = 30;
    // public Material neutral;
    public Material player1Material;
    public Material player2Material;
    public Sprite neutralControl;
    public Sprite playerControl;
    public Sprite enemyControl;
    public int resourceGeneration = 5;

    private int capturing = 0; // this get how many units are in the hexes surrounding the buildings

    private float controlPercent = 100; // this relates to the current owner or capturer of the building

    private float timePassed = 0;

    private resourceType resource;

    private List<TileScript> surroundingTiles = new();
    private GameObject occupantTile;

    

    void Start()
    {
        
    }

    
    void Update()
    {
        
        if(capturing > 0){
            timePassed += Time.deltaTime * capturing;
        }else if(capturing < 0){
            timePassed += Time.deltaTime * capturing;
        }
        
        if(timePassed > 5f){
            timePassed = 0f;
            controlPercent += 15;
            print("control percentage is " + controlPercent + "%");
        }else if(timePassed < -5f){
            timePassed = 0f;
            controlPercent -= 15;
            print("control percentage is " + controlPercent + "%");
        }

        controlPercent = controlPercent > 100 ? 100:controlPercent;
        

        if(controlPercent < 0){
            UnitOwner newCapture = getNewCapturer();
            if(newCapture == UnitOwner.World){
                capturing = 0;
                occupantTile.GetComponent<TileScript>().removeBuildingOwner();
            }else{
                if(newCapture != controller){
                    occupantTile.GetComponent<TileScript>().removeBuildingOwner();
                    occupantTile.GetComponent<TileScript>().addBuildingOwner(newCapture);
                }
                controller = newCapture;
                capturing = getCapturingCount();
            }

            
            updateVisuals(controller);
            controlPercent = 0;
        }
        
        
        
    }

    public void createBuilding(GameObject currentTile, string incomingResourceType){
        
        occupantTile = currentTile;
        if(incomingResourceType == "Wood"){
            resource = resourceType.Wood;
        }else if(incomingResourceType == "Iron"){
            resource = resourceType.Iron;
        }else if(incomingResourceType == "Food"){
            resource = resourceType.Food;
        }else if(incomingResourceType == "Fort"){
            resource = resourceType.Fort;
        }
        
        TileScript tile = occupantTile.GetComponent<TileScript>();

       
        TileScript hex = MapGenerateScript.getHex(tile.x + 1,tile.y,tile.z - 1).GetComponent<TileScript>();
        TileScript hex2 = MapGenerateScript.getHex(tile.x + 1,tile.y + 1,tile.z).GetComponent<TileScript>();
        TileScript hex3 = MapGenerateScript.getHex(tile.x,tile.y + 1,tile.z + 1).GetComponent<TileScript>();
        TileScript hex4 = MapGenerateScript.getHex(tile.x - 1,tile.y,tile.z + 1).GetComponent<TileScript>();
        TileScript hex5 = MapGenerateScript.getHex(tile.x - 1,tile.y - 1,tile.z).GetComponent<TileScript>();
        TileScript hex6 = MapGenerateScript.getHex(tile.x,tile.y - 1,tile.z - 1).GetComponent<TileScript>();

        
        
        if(hex != null){
            surroundingTiles.Add(hex);
            hex.addInitialOverlay(neutralControl,this);
        }
        if(hex2 != null){
            surroundingTiles.Add(hex2);
            hex2.addInitialOverlay(neutralControl,this);
        }
        if(hex3 != null){
            surroundingTiles.Add(hex3);
            hex3.addInitialOverlay(neutralControl,this);
        }
        if(hex4 != null){
            surroundingTiles.Add(hex4);
            hex4.addInitialOverlay(neutralControl,this);
        }
        if(hex5 != null){
            surroundingTiles.Add(hex5);
            hex5.addInitialOverlay(neutralControl,this);
        }
        if(hex6 != null){
            surroundingTiles.Add(hex6);
            hex6.addInitialOverlay(neutralControl,this);
        }

        occupantTile.GetComponent<TileScript>().addInitialOverlay(neutralControl,this);

        
        
    }

    

    public Sprite moveIntoHex(UnitOwner unitOwnerID){
        
        if(unitOwnerID == controller){
            capturing++;
        }else{
            capturing--;
        }

        print("capturing count is " + capturing);
        
        if(unitOwnerID == UnitOwner.Player){
            return playerControl;
        }else{
            return enemyControl;
        }
        
    }

    public Sprite moveOutOfHex(UnitOwner unitOwnerID){
        if(unitOwnerID == controller){
            capturing--;
        }else{
            capturing++;
        }

        print("capturing count is " + capturing);

        return neutralControl;
    }

    

    public UnitOwner getNewCapturer(){
        int[] player = new int[2];
        foreach(TileScript tile in surroundingTiles){
            if(tile.getOccupant() != UnitOwner.World){
                player[(int)tile.getOccupant()] += 1;
            }
        }

        if(player[0] == player[1]){
            return UnitOwner.World;
        }

        if(player[0] > player[1]){
            return UnitOwner.Player;
        }else{
            return UnitOwner.Enemy;
        }

        
        
        
    }

    public int getCapturingCount(){
        int num = 0;
        foreach(TileScript tile in surroundingTiles){
            if(tile.getOccupant() == controller){
                print("tile owner is " + tile.getOccupant());
                num++;
            }else if(tile.getOccupant() != UnitOwner.World){
                num--;
            }
        }
        print("num is " + num);
        return num;
    }

    private void updateVisuals(UnitOwner controller){
        if(controller == UnitOwner.Player){
            occupantTile.GetComponent<TileScript>().addOverlay(playerControl);
        }else if(controller == UnitOwner.Enemy){
            occupantTile.GetComponent<TileScript>().addOverlay(enemyControl);
        }
    }

    public List<int> updateResource(){
        if(resource == resourceType.Fort){
            return null;
        }
        List<int> resourceList = new();
        if(controller != UnitOwner.World){
            resourceList.Add((int)controller);
            resourceList.Add((int)resource);
            resourceList.Add(resourceGeneration);
        }
        return resourceList;
    }

    public void recruitUnits(int type){
        
        if(type == 0){
            
        }else if(type == 1){

        }else if(type == 2){

        }
    }


}
