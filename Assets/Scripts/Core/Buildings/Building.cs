// using UnityEngine;
// using System.Collections.Generic;
// 
// public class Building : MonoBehaviour
// {
//     // Start is called once before the first execution of Update after the MonoBehaviour is created
// 
//     private int controller = -1; // -1 for neutral, anything else is player num will be filled in even if control% isen't 100
// 
//     public int captureTime = 30;
// 
//     private bool isCaptured = false;
//     private int capturing = 0; // this get how many units are in the hexes surrounding the buildings
// 
//     private float controlPercent = 0; // this relates to the current owner or capturer of the building
// 
//     private float timePassed = 0;
// 
//     private List<GameObject> surroundingTiles;
//     private GameObject occupantTile;
// 
//     
// 
//     void Start()
//     {
//         
//     }
// 
//     // Update is called once per frame
//     void Update()
//     {
//         
// 
//         if(capturing != 0){
//             controlPercent += (capturing * Time.deltaTime);
//         }
// 
//         if(controlPercent < 0){
// 
//         }
//     }
// 
//     public void createBuilding(GameObject currentTile, string resourceType){
//         occupantTile = currentTile;
//         
//         TileScript tile = occupantTile.GetComponent<TileScript>();
// 
// 
//         GameObject hex = MapGenerateScript.getHex(tile.x + 1,tile.y,tile.z - 1);
//         GameObject hex2 = MapGenerateScript.getHex(tile.x + 1,tile.y + 1,tile.z);
//         GameObject hex3 = MapGenerateScript.getHex(tile.x,tile.y + 1,tile.z + 1);
//         GameObject hex4 = MapGenerateScript.getHex(tile.x - 1,tile.y,tile.z + 1);
//         GameObject hex5 = MapGenerateScript.getHex(tile.x - 1,tile.y - 1,tile.z);
//         GameObject hex6 = MapGenerateScript.getHex(tile.x,tile.y - 1,tile.z - 1);
//         if(hex != null){
//             surroundingTiles.Add(hex);
//         }
//         if(hex2 != null){
//             surroundingTiles.Add(hex2);
//         }
//         if(hex3 != null){
//             surroundingTiles.Add(hex3);
//         }
//         if(hex4 != null){
//             surroundingTiles.Add(hex4);
//         }
//         if(hex5 != null){
//             surroundingTiles.Add(hex5);
//         }
//         if(hex6 != null){
//             surroundingTiles.Add(hex6);
//         }
//         
//     }
// 
//     
// 
//     public void moveIntoHex(int unitOwnerID){
//         if(unitOwnerID == controller){
//             capturing++;
//         }else{
//             capturing--;
//         }
//     }
// 
//     
// 
//     public int getNewCapturer(){
//         int[] player = new int[2];
//         foreach(GameObject tile in surroundingTiles){
//             player[tile.owner] += 1;
//         }
// 
//         int largest = player[0];
//         int index = 0;
// 
//         for(int i = 0; i < player.Length; i++){
//             if(player[i] > largest){
//                 largest = player[i];
//                 index = i;
//             }
//         }
// 
//         return index;
//         
//     }
// 
//     public int getCapturingCount(){
//         int num = 0;
//         foreach(GameObject tile in surroundingTiles){
//             if(tile.owner == controller){
//                 num++;
//             }else if(tile.owner != -1){
//                 num--;
//             }
//         }
//         return num;
//     }
// }
