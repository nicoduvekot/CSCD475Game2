using UnityEngine;

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


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(generateMap){
            
            // print("bounds are " + temp.GetComponent<MeshCollider>().bounds);
            // print("width is " + width + " height is " + height);
            // Debug.DrawRay(transform.position,transform.right * width,Color.red,50f);
            // Debug.DrawRay(transform.position,transform.forward * height,Color.red,50f);

            for(int i = 0; i < columns; i++){

                for(int j = 0; j < rows; j++){
                    GameObject temp = Instantiate(terrainPrefab,transform.position + widthOffset + heightOffset,transform.rotation,transform);
                    width = temp.GetComponent<MeshCollider>().bounds.size.x * 0.79f;
                    height = temp.GetComponent<MeshCollider>().bounds.size.z * 1.03f;

                    heightOffset += new Vector3(0,0,-height);
                }
                widthOffset += new Vector3(width,0,0);
                if(flipOffset){
                    heightOffset = new Vector3(0,0,height/2);
                    flipOffset = false;
                }else{
                    heightOffset = new Vector3(0,0,0);
                    flipOffset = true;
                }


            }
            


        }
        generateMap = false;
    }
}
