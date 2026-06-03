using System;
using UnityEngine;
using System.Collections.Generic;
using Units;
using Resource;
using Selection;
using TeamControl;

public class BuildingScript : MonoBehaviour, ISelectable
{
    // used by ISelection to retrieve the Mono behavior of this
    public MonoBehaviour Behaviour => this;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private GameObject recruitBar;
    private bool recruiting;
    private GameObject captureBar;

    private bool visibleToPerspective = false;

    private bool isFort = false;
    
    public bool isCapital = false;
    public UnitOwner initialOwner = UnitOwner.World;
    private UnitOwner controller = (UnitOwner)2; // 2 for neutral, anything else is player num will be filled in even if control% isen't 100
    
    public event Action<BuildingScript, UnitOwner> OnBuildingCaptured;

    //public int captureTime = 30;
    // public Material neutral;
    public Material player1Material;
    public Material player2Material;
    public Sprite neutralControl;
    public Sprite playerControl;
    public Sprite enemyControl;
    public int resourceGeneration = 5;
    private Sprite resourceBuilding;

    private int capturing = 0; // this get how many units are in the hexes surrounding the buildings

    private float controlPercent = 100; // this relates to the current owner or capturer of the building

    private float captureTimePassed = 0;
    private float recruitTimePassed = 0;
    private float recruitCooldown = 12f;

    private Queue<int> recruitQueue = new();

    [SerializeField] 
    private ResourceType resource;

    private List<TileScript> surroundingTiles = new();
    public TileScript occupantTile; // the tile this building is on

    public IReadOnlyList<TileScript> GetNeighbourTiles() => surroundingTiles;

    void Start()
    {
        recruitBar = transform.Find("RecruitBar").gameObject;
        captureBar = transform.Find("CaptureBar").gameObject;
       
        captureBar.GetComponent<BuildingCapture>().setFill(100,controller);
     
        recruitBar.SetActive(false);
        captureBar.SetActive(false);
    }

    
    void Update()
    {
        // this checks if the tile has fog for the current perspective, if it does it will disable the color of the hex, recruit and capture bar
        if(visibleToPerspective){
            visibleToPerspective = !occupantTile.GetComponent<TileScript>().getFog();
            if(!visibleToPerspective){
                captureBar.SetActive(false);
                recruitBar.SetActive(false);
                occupantTile.GetComponent<TileScript>().addOverlay(neutralControl);
            }
        }else{
            visibleToPerspective = !occupantTile.GetComponent<TileScript>().getFog();
            if(visibleToPerspective){
                captureBar.SetActive(true);
                updateVisuals();
            }
        }
        
        
        
        if(capturing > 0){
            captureTimePassed += Time.deltaTime * capturing;
        }else if(capturing < 0){
            captureTimePassed += Time.deltaTime * capturing;
        }
        
        if(captureTimePassed > 1f){
            captureTimePassed = 0f;
            controlPercent += 15;
            print("control percentage is " + controlPercent + "%");
            captureBar.GetComponent<BuildingCapture>().setFill(controlPercent,controller);
        }else if(captureTimePassed < -1f){
            captureTimePassed = 0f;
            controlPercent -= 15;
            print("control percentage is " + controlPercent + "%");
            captureBar.GetComponent<BuildingCapture>().setFill(controlPercent,controller);
        }

        controlPercent = controlPercent > 100 ? 100:controlPercent;
        

        if(controlPercent < 0)
        {
            UnitOwner newCapture = getNewCapturer();
            
            if(newCapture == UnitOwner.World)
            {
                
                occupantTile.removeBuildingOwner();
            }else{
                if(newCapture != controller){
                    occupantTile.removeBuildingOwner();
                    occupantTile.addBuildingOwner(newCapture);
                    
                    OnBuildingCaptured?.Invoke(this, newCapture);
                }
                controller = newCapture;
                capturing = getCapturingCount();
            }

            
            updateVisuals();
            controlPercent = 0;
        }

        if(recruitQueue.Count >= 1 && recruitTimePassed >= recruitCooldown){
            recruitBar.SetActive(false);
            recruiting = false;
            recruitUnit();
            recruitTimePassed = 0f;

        }else if(recruitQueue.Count >= 1 && recruitTimePassed >= 0){
            if(!recruiting){
                if(visibleToPerspective){
                    recruitBar.SetActive(true);
                    recruitBar.GetComponent<BuildingRecruitment>().startRecruit(recruitCooldown);
                }
                recruiting = true;
            }
            recruitTimePassed += Time.deltaTime;
            recruitBar.GetComponent<BuildingRecruitment>().setFill(recruitTimePassed);

        }else if(recruitQueue.Count == 0){
            recruitBar.SetActive(false);
            recruiting = false;
            recruitTimePassed = 0f;
        }
        
        
        
    }

    public void OnSelected()
    {
        if (resource == ResourceType.Fort && controller == UnitOwner.Player)
            UnitRecruitPopup.Instance.show(this);
    }

    public void createBuilding(GameObject currentTile,out UnitOwner owner){

        
        occupantTile = currentTile.GetComponent<TileScript>();
        controller = initialOwner;

        owner = controller;
        
        if(resource == ResourceType.Wood){
            resourceBuilding = Resources.Load("PixelArt/woodcutter", typeof(Sprite)) as Sprite;
        }else if(resource == ResourceType.Iron){
            resourceBuilding = Resources.Load("PixelArt/mine", typeof(Sprite)) as Sprite;
        }else if(resource == ResourceType.Food){
            resourceBuilding = Resources.Load("PixelArt/farm", typeof(Sprite)) as Sprite;
        }else if(resource == ResourceType.Fort){
            resourceBuilding = Resources.Load("PixelArt/fort", typeof(Sprite)) as Sprite;
        }
        
        TileScript tile = occupantTile.GetComponent<TileScript>();

       
        TileScript hex  = MapGenerateScript.getHex(tile.x + 1,tile.y,tile.z - 1).GetComponent<TileScript>();
        TileScript hex2 = MapGenerateScript.getHex(tile.x + 1,tile.y + 1,tile.z).GetComponent<TileScript>();
        TileScript hex3 = MapGenerateScript.getHex(tile.x,tile.y + 1,tile.z + 1).GetComponent<TileScript>();
        TileScript hex4 = MapGenerateScript.getHex(tile.x - 1,tile.y,tile.z + 1).GetComponent<TileScript>();
        TileScript hex5 = MapGenerateScript.getHex(tile.x - 1,tile.y - 1,tile.z).GetComponent<TileScript>();
        TileScript hex6 = MapGenerateScript.getHex(tile.x,tile.y - 1,tile.z - 1).GetComponent<TileScript>();

        
        
        if(hex != null){
            surroundingTiles.Add(hex);
            hex.addInitialOverlay(neutralControl,this);
            hex.addOverlay(neutralControl);
        }
        if(hex2 != null){
            surroundingTiles.Add(hex2);
            hex2.addInitialOverlay(neutralControl,this);
            hex2.addOverlay(neutralControl);
        }
        if(hex3 != null){
            surroundingTiles.Add(hex3);
            hex3.addInitialOverlay(neutralControl,this);
            hex3.addOverlay(neutralControl);
        }
        if(hex4 != null){
            surroundingTiles.Add(hex4);
            hex4.addInitialOverlay(neutralControl,this);
            hex4.addOverlay(neutralControl);
        }
        if(hex5 != null){
            surroundingTiles.Add(hex5);
            hex5.addInitialOverlay(neutralControl,this);
            hex5.addOverlay(neutralControl);
        }
        if(hex6 != null){
            surroundingTiles.Add(hex6);
            hex6.addInitialOverlay(neutralControl,this);
            hex6.addOverlay(neutralControl);
        }

        updateVisuals();
        GetComponent<SpriteRenderer>().sprite = resourceBuilding;
        
        
        
    }

    

    public Sprite moveIntoHex(UnitOwner unitOwnerID){
        
        if(unitOwnerID == controller){
            capturing++;
        }else{
            capturing--;
        }
        
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
        print("moving out of hex");

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

        if(isCapital){
            //Do stuff

            // Used to end the game once the capital has been taken
            if (player[0] > player[1])
            {
                print("Capital has been captured by player");
                GameManager.Instance.gameOver(UnitOwner.Player);
            }
            else
            {
                print("Capital has been captured by enemy");
                GameManager.Instance.gameOver(UnitOwner.Enemy);
            }
        }

        if(player[0] > player[1]){
            print("new capturer is player");
            GlobalSound.capturePOI();
            return UnitOwner.Player;
        }else{
            print("new capturer is enemy");
            GlobalSound.losePOI();
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
        return num;
    }

    private void updateVisuals(){
        if(visibleToPerspective){
            if(controller == UnitOwner.Player){
                occupantTile.addOverlay(playerControl);
            }else if(controller == UnitOwner.Enemy){
                occupantTile.addOverlay(enemyControl);
            }else if(controller == UnitOwner.World){
                occupantTile.addOverlay(neutralControl);
            }
        }else{
            occupantTile.addOverlay(neutralControl);
        }
    }

    public UnitOwner getOwner(){
        return controller;
    }

    /// <summary>
    /// Intended to be:
    /// Called by GameManager to Reset the game (for agent usage primarily)
    /// </summary>
    /// <param name="resetOwnership">
    /// The Ownership this building should be reset to
    /// </param>
    public void ResetBuilding(UnitOwner resetOwnership)
    {
        // reset internal trackers
        capturing = 0;
        captureTimePassed = 0f;
        
        // clear recruitment status
        recruitQueue.Clear();
        recruitTimePassed = 0f;
        
        // reset the ownership
        controller = resetOwnership;
        
        // reset control percent (if capital 100 control, else 0)
        controlPercent = isCapital ? 100f : 0f;

        // change building owner to resetOwnership value
        occupantTile.removeBuildingOwner();
        occupantTile.addBuildingOwner(resetOwnership);
        
        // reset surrounding tiles (capture tiles)
        foreach (TileScript tile in surroundingTiles)
        {
            // each surrounding tile to any building is neutral
            tile.addOverlay(neutralControl);
        }
        
        // reset the visual overlay
        updateVisuals();
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

        if(resource != ResourceType.Fort){
            print("cannot create units at buildings other than forts");
            return;
        }

        // if((PerspectiveManager.Instance.CurrentPerspective == Perspective.Player && controller != UnitOwner.Player) ||
        // PerspectiveManager.Instance.CurrentPerspective == Perspective.Enemy && controller != UnitOwner.Enemy){
        //     print("cannot create units if fort is owner by other player");
        //     return;
        // }

        int available = 0;
        TileScript tile = occupantTile.GetComponent<TileScript>();

        for(int i = 1; i < 7; i++){

            int[] nextHex = UnitPathing.hexNeighbor(new int[] {tile.x,tile.y,tile.z},i);

            if(MapGenerateScript.getHex(nextHex[0],nextHex[1],nextHex[2]) != null 
            && MapGenerateScript.getHex(nextHex[0],nextHex[1],nextHex[2]).GetComponent<TileScript>().canMakeUnit()){
                available++;
                
            }
        }


        if(recruitQueue.Count >= available){
            print("cannot recruit more than " + available + " units at once");
            return;
        }

        

        int[] spend = {0,0,0};
        spend[type] = 100;

        if(GameManager.Instance.spendResources(controller,spend[0],spend[1],spend[2])){
            print("started recruiting unit");
            recruitQueue.Enqueue(type);
        }else{
            print("missing resources");
        }

        

    }

    private void recruitUnit(){
        int type;
        

        

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
            int refundType = recruitQueue.Dequeue();
            GameManager.Instance.addResource(controller,refundType,100);
            return;
        }else{
            type = recruitQueue.Dequeue();
        }

        if(controller == UnitOwner.Player){
            
            if(type == 0){
                openHex.makeUnit(Resources.Load("Prefabs/PlayerSoldier_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Player);
            }else if(type == 1){
                openHex.makeUnit(Resources.Load("Prefabs/PlayerArcher_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Player);
            }else if(type == 2){
                openHex.makeUnit(Resources.Load("Prefabs/PlayerHorseman_Prefab", typeof (GameObject)) as GameObject,UnitOwner.Player);
            }
            GlobalSound.recruitUnitSound();
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

    // Added by Nathan for getting the Queue for display
    // Passes a clone of the queue
    public Queue<int> getUnitProductionQueue()
    {
        return new Queue<int>(recruitQueue);
    } 


}
