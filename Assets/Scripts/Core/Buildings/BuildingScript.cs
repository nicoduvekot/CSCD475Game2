using UnityEngine;
using System.Collections.Generic;
using Units;
using Unity.Collections;

public class BuildingScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public  enum ResourceType{
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

    private float captureTimePassed = 0;

    private ResourceType resource;

    private List<TileScript> surroundingTiles = new();
    private GameObject occupantTile;

    

    void Start()
    {
        
    }

    
    void Update()
    {
        
        if(capturing > 0){
            captureTimePassed += Time.deltaTime * capturing;
        }else if(capturing < 0){
            captureTimePassed += Time.deltaTime * capturing;
        }
        
        if(captureTimePassed > 5f){
            captureTimePassed = 0f;
            controlPercent += 15;
            print("control percentage is " + controlPercent + "%");
        }else if(captureTimePassed < -5f){
            captureTimePassed = 0f;
            controlPercent -= 15;
            print("control percentage is " + controlPercent + "%");
        }

        controlPercent = controlPercent > 100 ? 100:controlPercent;
        

        if(controlPercent < 0){
            UnitOwner newCapture = getNewCapturer();
            if(newCapture == UnitOwner.World){
                
                occupantTile.GetComponent<TileScript>().removeBuildingOwner();
            }else{
                if(newCapture != controller){
                    occupantTile.GetComponent<TileScript>().removeBuildingOwner();
                    occupantTile.GetComponent<TileScript>().addBuildingOwner(newCapture);
                }
                controller = newCapture;
                capturing = getCapturingCount();
            }

            
            updateVisuals();
            controlPercent = 0;
        }
        
        
        
    }

    public void createBuilding(GameObject currentTile, string incomingResourceType){
        
        occupantTile = currentTile;
        if(incomingResourceType == "Wood"){
            resource = ResourceType.Wood;
        }else if(incomingResourceType == "Iron"){
            resource = ResourceType.Iron;
        }else if(incomingResourceType == "Food"){
            resource = ResourceType.Food;
        }else if(incomingResourceType == "Fort"){
            resource = ResourceType.Fort;
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

            print("new capturer is world");
            return UnitOwner.World;
        }

        if(player[0] > player[1]){
            print("new capturer is player");
            return UnitOwner.Player;
        }else{
            print("new capturer is enemy");
            return UnitOwner.Enemy;
        }

        
        
        
    }

    public int getCapturingCount(){
        int num = 0;
        foreach(TileScript tile in surroundingTiles){
            if(tile.getOccupant() == controller){
                num++;
            }else if(tile.getOccupant() != UnitOwner.World){
                num--;
            }
        }
        print("num is " + num);
        return num;
    }

    private void updateVisuals(){
        if(controller == UnitOwner.Player){
            occupantTile.GetComponent<TileScript>().addOverlay(playerControl);
        }else if(controller == UnitOwner.Enemy){
            occupantTile.GetComponent<TileScript>().addOverlay(enemyControl);
        }
    }

    // public List<int> updateResource(){
    //     if(resource == resourceType.Fort){
    //         return null;
    //     }
    //     List<int> resourceList = new();
    //     if(controller != UnitOwner.World){
    //         resourceList.Add((int)controller);
    //         resourceList.Add((int)resource);
    //         resourceList.Add(resourceGeneration);
    //     }
    //     return resourceList;
    // }

    public UnitOwner getOwner(){
        return controller;
    }

    public ResourceType getResource(){
        return resource;
    }

    public void setResource(ResourceType r){ // ONLY used 
        resource = r;
    }

    public void recruitUnit(int type){

        if(controller == UnitOwner.World){
            print("cannot created units if owned by world");
            return;
        }

        TileScript openHex = null;
        TileScript tile = occupantTile.GetComponent<TileScript>();
        


        for(int i = 1; i < 7; i++){

            int[] nextHex = UnitPathing.hexNeighbor(new int[] {tile.x,tile.y,tile.z},i);

            if(MapGenerateScript.getHex(nextHex[0],nextHex[1],nextHex[2]) != null 
            && MapGenerateScript.getHex(nextHex[0],nextHex[1],nextHex[2]).GetComponent<TileScript>().canMakeUnit()){
                
                openHex = MapGenerateScript.getHex(nextHex[0],nextHex[1],nextHex[2]).GetComponent<TileScript>();
                break;
            }
        }

        if(openHex == null){
            print("no open hexes");
            return;
        }

        if(controller == UnitOwner.Player){
            if(type == 0){
                openHex.makeUnit(Resources.Load("Prefabs/PlayerSoldier_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Player);
            }else if(type == 1){
                openHex.makeUnit(Resources.Load("Prefabs/PlayerArcher_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Player);
            }else if(type == 2){
                openHex.makeUnit(Resources.Load("Prefabs/PlayerHorseman_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Player);
            }
        }else{
            if(type == 0){
                openHex.makeUnit(Resources.Load("Prefabs/EnemySoldier_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Enemy);
            }else if(type == 1){
                openHex.makeUnit(Resources.Load("Prefabs/EnemyArcher_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Enemy);
            }else if(type == 2){
                openHex.makeUnit(Resources.Load("Prefabs/EnemyHorseman_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Enemy);
            }
        }
        

    }


}
