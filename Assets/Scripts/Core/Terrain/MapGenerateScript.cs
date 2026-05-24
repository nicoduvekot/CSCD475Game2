using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


public class MapGenerateScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public bool generateMap = true;

    public int rows = 1;

    public int columns = 1;

    public GameObject terrainPrefab;

    private float width;
    private float height;

    private Vector3 widthOffset;
    private Vector3 heightOffset;

    private bool flipOffset = true;

    private static Dictionary<Vector3Int,GameObject>  hexStorage;

    // all of these values are used to track the xyz position of the hex
    private int curX = 0;
    private int curY = 0;
    private int curZ = 0;

    private int columnsLeft;
    private int rowsDown;
    private int columnsRight;
    private int rowsUp;

    private int startX;
    private int startZ;

    public int addColumns = 0;

    public int addRows = 0;

    private Vector3Int maxCords;
    private Vector3Int minCords;
    public bool refreshMaterial = false;

    private List<BuildingScript> buildings = new();

    private Dictionary<Vector3Int,bool> visibleHexes = new();
    

    

    void Start()
    {
        
        
        rowsDown = (rows / 2);
        
        columnsLeft = columns / 2;

        columnsRight = (columns - 1) - columnsLeft;

        rowsUp = (rows - 1) - rowsDown;
        
            
        // curX = 0;
        curY = -columnsLeft;
        // curZ = 0;
        startX = (columnsLeft / 2) + rowsDown + 1;
        startZ = rows - (rowsUp + (columnsLeft / 2));
        if(columnsLeft % 2 != 0){
            startX++;
        }

        hexStorage = new();
        if(!generateMap){

            List<GameObject> buildingHexes = new();
            
            for(int i = 0; i < transform.childCount; i++){
                
                GameObject childObj = transform.GetChild(i).gameObject;
                TileScript childTile = childObj.GetComponent<TileScript>();

                if(childTile.getTerrain() == TerrainType.building){
                    buildingHexes.Add(childObj);
                }
                
                hexStorage.Add(new Vector3Int(childTile.x,childTile.y,childTile.z),transform.GetChild(i).gameObject);// add children to list outside loop
                visibleHexes.Add(new Vector3Int(childTile.x,childTile.y,childTile.z),false);
                if(refreshMaterial){
                    childTile.createHex(childTile.x,childTile.y,childTile.z,childTile.getTerrain());
                }
                

            }
            

            foreach(GameObject hex in buildingHexes ){
                hex.GetComponent<TileScript>().createBuilding("Wood");
                buildings.Add(hex.transform.Find("Building").GetComponent<BuildingScript>());
                
            }
            


            findMaxCords(hexStorage);

            for(int i = 0; i < addRows; i++){
                addRow();
                findMaxCords(hexStorage);
            }
            addRows = 0;

            for(int i = 0; i < addColumns; i++){
                addColumn();
                findMaxCords(hexStorage);
            }
            addColumns = 0;
        }
        
        
    }

    private void findMaxCords(Dictionary<Vector3Int,GameObject> cordMap){

        int minX = 0,maxX = 0,minY = 0,maxY = 0,minZ = 0,maxZ = 0;

        foreach(Vector3Int cord in cordMap.Keys){
            if(minX > cord.x){
                minX = cord.x;
            }else if(maxX < cord.x){
                maxX = cord.x;
            }

            if(minY > cord.y){
                minY = cord.y;
            }else if(maxY < cord.y){
                maxY = cord.y;
            }

            if(minZ > cord.z){
                minZ = cord.z;
            }else if(maxZ < cord.z){
                maxZ = cord.z;
            }
        }

        minCords = new Vector3Int(minX,minY,minZ);
        maxCords = new Vector3Int(maxX,maxY,maxZ);

        

        
        
    }

    

    // Update is called once per frame
    void Update()
    {
        
        if(generateMap){
            
            
            for(int i = 0; i < columns; i++){

                curZ = startZ - rows;
                curX = rows - startX;
                
                for(int j = 0; j < rows; j++){

                    

                    // this physically places the hexes on the map
                    GameObject temp = Instantiate(terrainPrefab,transform.position + widthOffset + heightOffset,transform.rotation,transform);
                    width = temp.GetComponent<MeshCollider>().bounds.size.x * 0.79f;
                    height = temp.GetComponent<MeshCollider>().bounds.size.z * 1.03f;

                    //int randomType = UnityEngine.Random.Range(0,Enum.GetValues(typeof(TileScript.terrainType)).Length);

                    TerrainType terrain = TerrainType.dirt;


                    temp.GetComponent<TileScript>().createHex(curX,curY,curZ,terrain);

                    hexStorage.Add(new Vector3Int(curX,curY,curZ),temp);

                    
                    //temp.GetComponent<TileScript>().setTerrian(terrain);

                    
                    curX--;
                    curZ++;

                    heightOffset += new Vector3(0,0,-height);
                }
                
                widthOffset += new Vector3(width,0,0);
                if(flipOffset){
                    startX--;
                    
                    heightOffset = new Vector3(0,0,height/2);
                    flipOffset = false;
                }else{
                    startZ++;
                    heightOffset = new Vector3(0,0,0);
                    flipOffset = true;
                }
                
                curY++;
                


            }
            findMaxCords(hexStorage);


        }
        generateMap = false;
        
    }


    public static GameObject getHex(int x, int y, int z){

        GameObject returnObject = null;
        hexStorage.TryGetValue(new Vector3Int(x,y,z), out returnObject);

        return returnObject;
    }

    public void addRow(){
        
        

            int size = transform.childCount;

            for(int i = 0; i < size;i++ ){

                TileScript childTile = transform.GetChild(i).GetComponent<TileScript>();
                

                if(childTile.x < childTile.z && Mathf.Abs(childTile.x) + Mathf.Abs(childTile.z) >= rows - 2){ // combined x and z will equal the top row or the top row - 1, 
                                                                                                            // if x is less than z then this is the bottom row

                    
                    height = childTile.gameObject.GetComponent<MeshCollider>().bounds.size.z * 1.03f;
                    GameObject temp = Instantiate(terrainPrefab,childTile.transform.position + new Vector3(0,0,-height),transform.rotation,transform);
                    
                    

                    //int randomType = UnityEngine.Random.Range(0,Enum.GetValues(typeof(TileScript.terrainType)).Length);

                    TerrainType terrain = TerrainType.dirt;

                    int x = childTile.x - 1;
                    int y = childTile.y;
                    int z = childTile.z + 1;
                    


                    temp.GetComponent<TileScript>().createHex(x,y,z,terrain);

                    hexStorage.Add(new Vector3Int(x,y,z),temp);
                }else if(childTile.x > childTile.z && Mathf.Abs(childTile.x) + Mathf.Abs(childTile.z) >= rows - 1){

                    height = childTile.gameObject.GetComponent<MeshCollider>().bounds.size.z * 1.03f;
                    GameObject temp = Instantiate(terrainPrefab,childTile.transform.position + new Vector3(0,0,height),transform.rotation,transform);
                    
                    

                    //int randomType = UnityEngine.Random.Range(0,Enum.GetValues(typeof(TileScript.terrainType)).Length);

                    TerrainType terrain = TerrainType.dirt;

                    int x = childTile.x + 1;
                    int y = childTile.y;
                    int z = childTile.z - 1;               


                    temp.GetComponent<TileScript>().createHex(x,y,z,terrain);
                    hexStorage.Add(new Vector3Int(x,y,z),temp);
                }

            }
            rows += 2;

        
        

    }

    public void addColumn(){
        int size = transform.childCount;

            for(int i = 0; i < size;i++ ){

                TileScript childTile = transform.GetChild(i).GetComponent<TileScript>();

                
                

                if(childTile.y == minCords.y){ // left side 
                    
                    List<Vector3Int> leftColumn = getColumn(childTile.y);

                    int localMaxX = leftColumn[0].x;
                    int localMinZ = leftColumn[0].z;
                    // int localMaxZ = leftColumn[0].x;
                    // int localMinX = leftColumn[0].z;

                    foreach(Vector3Int hex in leftColumn){
                        if(localMaxX < hex.x){
                            localMaxX = hex.x;
                        }
                        if(localMinZ > hex.z){
                            localMinZ = hex.z;
                        }

                        // if(localMinX > hex.x){
                        //     localMinX = hex.x;
                        // }
                        // if(localMaxZ < hex.z){
                        //     localMaxZ = hex.z;
                        // }
                    }

                    

                    int x = childTile.x;
                    int y = childTile.y - 1;
                    int z = childTile.z;
                    
                    if(getHex(localMaxX + 1,childTile.y + 1,localMinZ) != null){ // this means the column will be shifted up
                        height = childTile.gameObject.GetComponent<MeshCollider>().bounds.size.z * 1.03f / 2;
                        z--;

                    }else{ // this means the column will be shifted down
                    
                        height = childTile.gameObject.GetComponent<MeshCollider>().bounds.size.z * 1.03f / -2;
                        x--;

                    }
                    width = childTile.GetComponent<MeshCollider>().bounds.size.x * 0.79f;
                    
                    GameObject temp = Instantiate(terrainPrefab,childTile.transform.position + new Vector3(0,0,height) + new Vector3(-width,0,0),transform.rotation,transform);
                    
                    

                    //int randomType = UnityEngine.Random.Range(0,Enum.GetValues(typeof(TileScript.terrainType)).Length);

                    TerrainType terrain = TerrainType.dirt;
                    


                    temp.GetComponent<TileScript>().createHex(x,y,z,terrain);

                    hexStorage.Add(new Vector3Int(x,y,z),temp);
                }else if(childTile.y == maxCords.y){ // right side

                    List<Vector3Int> leftColumn = getColumn(childTile.y);

                    int localMaxX = leftColumn[0].x;
                    int localMinZ = leftColumn[0].z;
                    // int localMaxZ = leftColumn[0].x;
                    // int localMinX = leftColumn[0].z;

                    foreach(Vector3Int hex in leftColumn){
                        if(localMaxX < hex.x){
                            localMaxX = hex.x;
                        }
                        if(localMinZ > hex.z){
                            localMinZ = hex.z;
                        }

                        // if(localMinX > hex.x){
                        //     localMinX = hex.x;
                        // }
                        // if(localMaxZ < hex.z){
                        //     localMaxZ = hex.z;
                        // }
                    }

                    int x = childTile.x;
                    int y = childTile.y + 1;
                    int z = childTile.z;

                    if(getHex(localMaxX,childTile.y - 1,localMinZ - 1) != null){// column will be shifted up
                        height = childTile.gameObject.GetComponent<MeshCollider>().bounds.size.z * 1.03f / 2;
                        x++;

                    }else{// column will be shifted down
                        height = childTile.gameObject.GetComponent<MeshCollider>().bounds.size.z * 1.03f / -2;
                        z++;

                    }
                    width = childTile.GetComponent<MeshCollider>().bounds.size.x * 0.79f;

                    GameObject temp = Instantiate(terrainPrefab,childTile.transform.position + new Vector3(0,0,height) + new Vector3(width,0,0),transform.rotation,transform);
                    
                    

                    //int randomType = UnityEngine.Random.Range(0,Enum.GetValues(typeof(TileScript.terrainType)).Length);

                    TerrainType terrain = TerrainType.dirt;


                    temp.GetComponent<TileScript>().createHex(x,y,z,terrain);
                    hexStorage.Add(new Vector3Int(x,y,z),temp);
                }

            }
            columns += 2;

    }

    public List<Vector3Int> getColumn(int y){
        List<Vector3Int> hexColumn = new();
        foreach(Vector3Int cord in hexStorage.Keys){
            if(cord.y == y){
                hexColumn.Add(new Vector3Int(cord.x,cord.y,cord.z));
                
            }
        }
        return hexColumn;
    }

    public bool getVisible(int x, int y, int z){

        bool canSee = false;
        visibleHexes.TryGetValue(new Vector3Int(x,y,z),out canSee);
        return canSee;
    }

    public void setVisible(int x, int y, int z){

        visibleHexes[new Vector3Int(x,y,z)] = true;
    }

    public void removeVisible(int x, int y, int z){

        visibleHexes[new Vector3Int(x,y,z)] = false;
    }
}
