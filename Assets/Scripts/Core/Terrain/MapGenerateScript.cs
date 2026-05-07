using UnityEngine;
using System;

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
        
    }

    // Update is called once per frame
    void Update()
    {
        if(generateMap){
            
            // print("columns left + " + columnsLeft);
            // 
            // print("rows down + " + rowsDown);
            // print("startX " + startX);
            // print("startZ " + startZ);

            // print("bounds are " + temp.GetComponent<MeshCollider>().bounds);
            // print("width is " + width + " height is " + height);
            // Debug.DrawRay(transform.position,transform.right * width,Color.red,50f);
            // Debug.DrawRay(transform.position,transform.forward * height,Color.red,50f);

            for(int i = 0; i < columns; i++){

                curZ = startZ - rows;
                curX = rows - startX;
                
                for(int j = 0; j < rows; j++){

                    

                    // this physically places the hexes on the map
                    GameObject temp = Instantiate(terrainPrefab,transform.position + widthOffset + heightOffset,transform.rotation,transform);
                    width = temp.GetComponent<MeshCollider>().bounds.size.x * 0.79f;
                    height = temp.GetComponent<MeshCollider>().bounds.size.z * 1.03f;

                    int randomType = UnityEngine.Random.Range(0,Enum.GetValues(typeof(TileScript.terrainType)).Length);

                    TileScript.terrainType terrain = (TileScript.terrainType)randomType;


                    temp.GetComponent<TileScript>().createHex(curX,curY,curZ,terrain);

                    
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
            


        }
        generateMap = false;
    }
}
