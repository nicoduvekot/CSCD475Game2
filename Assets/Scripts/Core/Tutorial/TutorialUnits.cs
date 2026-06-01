using UnityEngine;
using Units;

public class TutorialUnits : MonoBehaviour
{
    //[SerializeField] private GameObject EnemyArcher;
    //[SerializeField] private GameObject EnemyArcher1;
    //[SerializeField] private GameObject EnemySoldier;
    //[SerializeField] private GameObject EnemySoldier1;
    //[SerializeField] private GameObject EnemyHorseman;
    //[SerializeField] private GameObject EnemyHorseman1;

    void Start()
    {
        TileScript openHex = MapGenerateScript.getHex(0, 8, 8).GetComponent<TileScript>();
        openHex.makeUnit(Resources.Load("Prefabs/EnemySoldier_Prefab", typeof(GameObject)) as GameObject, UnitOwner.Enemy);

        openHex = MapGenerateScript.getHex(1, 5, 4).GetComponent<TileScript>();
        openHex.makeUnit(Resources.Load("Prefabs/EnemyArcher_Prefab", typeof(GameObject)) as GameObject, UnitOwner.Enemy);

        openHex = MapGenerateScript.getHex(7, 8, 1).GetComponent<TileScript>();
        openHex.makeUnit(Resources.Load("Prefabs/EnemyArcher_Prefab", typeof(GameObject)) as GameObject, UnitOwner.Enemy);

        openHex = MapGenerateScript.getHex(7, 7, 0).GetComponent<TileScript>();
        openHex.makeUnit(Resources.Load("Prefabs/EnemySoldier_Prefab", typeof(GameObject)) as GameObject, UnitOwner.Enemy);

        openHex = MapGenerateScript.getHex(0, 8, 8).GetComponent<TileScript>();
        openHex.makeUnit(Resources.Load("Prefabs/EnemyHorseman_Prefab", typeof(GameObject)) as GameObject, UnitOwner.Enemy);

        openHex = MapGenerateScript.getHex(1, 9, 8).GetComponent<TileScript>();
        openHex.makeUnit(Resources.Load("Prefabs/EnemyHorseman_Prefab", typeof(GameObject)) as GameObject, UnitOwner.Enemy);
    }
    
}
