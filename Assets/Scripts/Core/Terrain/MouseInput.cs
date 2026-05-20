using Units;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInput : MonoBehaviour
{

    public MouseActions mouseInput;
    private InputAction click;

    private InputAction rightClick;
    private InputAction mousePos;

    public MapGenerateScript map;
    private GameObject lastSelected;

    private string terrainType = "dirt";

    [SerializeField] private Camera MainCamera;
    private int controller;

    void Awake(){
        mouseInput = new MouseActions();

    }

    void OnEnable(){
        click = mouseInput.Clicking.Select;
        click.Enable();
        click.performed += Select;

        rightClick = mouseInput.Clicking.RightClick;
        rightClick.Enable();
        rightClick.performed += changeTerrainType;

        

        mousePos = mouseInput.Clicking.MousePosition;
        mousePos.Enable();
        

    }

    void OnDisable(){
        click.Disable();
        mousePos.Disable();
        rightClick.Disable();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //print(Mouse.current.position.ReadValue());
    }

    private void Select(InputAction.CallbackContext context){
        Vector2 MousePosition = Mouse.current.position.ReadValue();
        Vector3 mousePosition3D = new Vector3(MousePosition.x,MousePosition.y,1f);
        
        Vector3 realPos = MainCamera.ScreenToWorldPoint(mousePosition3D);

        RaycastHit hit;

        //used for Orthographic
        //Physics.Raycast(realPos,MainCamera.transform.forward,out hit,50f);

        Ray ray = MainCamera.ScreenPointToRay(MousePosition);

        Physics.Raycast(realPos,ray.direction,out hit,50f);
        //Debug.DrawRay(realPos,ray.direction * 50,Color.red,50f);
        if(hit.collider != null){
            

//             GameObject inverseTile;
//             TileScript inverseTileScript = hit.collider.gameObject.GetComponent<TileScript>();
//             hit.collider.gameObject.GetComponent<TileScript>().setTerrain(terrainType);
// 
// 
// 
//             inverseTile = MapGenerateScript.getHex(-inverseTileScript.z,-inverseTileScript.y,-inverseTileScript.x);
// 
//             inverseTile.GetComponent<TileScript>().setTerrain(terrainType);

            lastSelected = hit.collider.gameObject;
        }else{
            print("Hit nothing");
        }

        TileScript hex = hit.collider.gameObject.GetComponent<TileScript>();
        GameObject hexObject = hit.collider.gameObject;

        if(controller == 0){
            hit.collider.gameObject.GetComponent<TileScript>().addOccupant(UnitOwner.Player);
        }else if(controller == 1){
            hit.collider.gameObject.GetComponent<TileScript>().addOccupant(UnitOwner.Enemy);
        }else{
            hit.collider.gameObject.GetComponent<TileScript>().removeOccupant();
        }
        
        

        print($"hit hex is {hex.x},{hex.y},{hex.z}");

        TileScript tempHex = MapGenerateScript.getHex(hex.x,hex.y,hex.z).GetComponent<TileScript>();

        print($"hit hex is {tempHex.x},{tempHex.y},{tempHex.z}");
        
    }

    public void changeTerrainType(InputAction.CallbackContext context){
        // if(terrainType == "dirt"){
        //     terrainType = "grass";
        // }else if(terrainType == "grass"){
        //     terrainType = "forest";
        // }else if(terrainType == "forest"){
        //     terrainType = "mountain";
        // }else if(terrainType == "mountain"){
        //     terrainType = "water";
        // }else if(terrainType == "water"){
        //     terrainType = "desert";
        // }else if(terrainType == "desert"){
        //     terrainType = "snow";
        // }else if(terrainType == "snow"){
        //     terrainType = "building";
        // }else if(terrainType == "building"){
        //     terrainType = "dirt";
        // }
        // print("terrain type is + " + terrainType);
        if(controller == 0){
            controller = 1;
            print("controller is now enemy");
        }else if(controller == 1){
            controller = 2;
            print("controller is now remove");
        }else{
            controller = 0;
            print("controller is now player");
        }
    }
}
