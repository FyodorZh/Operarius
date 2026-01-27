namespace Operarius
{
    public class SemiPeriodicMultiLogicDriver : PeriodicLikeMultiLogicDriver<ManualSemiPeriodicLogicDriver, ISemiPeriodicLogicDriverCtl>
    {
        protected override ManualSemiPeriodicLogicDriver ConstructManualDriver()
        {
            return new ManualSemiPeriodicLogicDriver();
        }
    }
}