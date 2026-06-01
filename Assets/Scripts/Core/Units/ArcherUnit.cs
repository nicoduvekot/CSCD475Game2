namespace Units
{
    public class ArcherUnit : BaseUnit
    {
        protected override void Awake()
        {
            base.Awake();
            UnitType = 1;
        }
    }
}