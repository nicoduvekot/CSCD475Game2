namespace Units
{
    public class SoldierUnit : BaseUnit
    {
        protected override void Awake()
        {
            base.Awake();
            UnitType = 0;
        }
    }
}