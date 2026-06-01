namespace Units
{
    public class HorsemanUnit : BaseUnit
    {
        protected override void Awake()
        {
            base.Awake();
            UnitType = 2;
        }
    }
}