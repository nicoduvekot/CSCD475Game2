using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    public class ExampleStart : MonoBehaviour
    {
        public MapSettings Map;
        public MapView MapView;
        public UnitMove Unit;

        public MapEntity MapEntity { get; private set; }

        void Start()
        {
            if (!MapView)
            {
#if UNITY_2023_1_OR_NEWER
                MapView = FindFirstObjectByType<MapView>();
#else
                MapView = FindObjectOfType<MapView>();
#endif
            }
            MapEntity = new MapEntity(Map, MapView);
            if (MapView)
            {
                MapView.Init(MapEntity);
            }
            else
            {
                Log.E("Can't find MapView. Random errors can occur");
            }

            if (!Unit)
            {
#if UNITY_2023_1_OR_NEWER
                Unit = FindFirstObjectByType<UnitMove>();
#else
                Unit = FindObjectOfType<UnitMove>();
#endif
            }
            if (Unit)
            {
                Unit.Init(MapEntity);
            }
            else
            {
                Log.E("Can't find any Unit. Example level start incorrect");
            }
        }

        void Update()
        {
            if (MyInput.GetGKeyUp())
            {
                MapEntity.GridToggle();
            }
        }
    }
}
