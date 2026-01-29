namespace Operarius.Tests
{
    public class PeriodicTester_ManualPeriodicLogicDriver : PeriodicLikeTester<PeriodicTestLogic, IPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<IPeriodicLogicDriverCtl> GetDriver()
        {
            return new ManualPeriodicLogicDriver(TimeSpan.FromMilliseconds(10));
        }
    }
}