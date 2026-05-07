using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInput : MonoBehaviour
{

    public MouseActions mouseInput;
    private InputAction click;
    private InputAction mousePos;

    private GameObject lastSelected;

    [SerializeField] private Camera MainCamera;

    void Awake(){
        mouseInput = new MouseActions();

    }

    void OnEnable(){
        click = mouseInput.Clicking.Select;
        click.Enable();
        click.performed += Select;

        mousePos = mouseInput.Clicking.MousePosition;
        mousePos.Enable();
        

    }

    void OnDisable(){
        click.Disable();
        mousePos.Disable();
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
            if(lastSelected != null){
                lastSelected.GetComponent<TileScript>().setGreen(false);
            }

            hit.collider.gameObject.GetComponent<TileScript>().setGreen(true);
            lastSelected = hit.collider.gameObject;
        }else{
            print("Hit nothing");
        }
        
    }
}
