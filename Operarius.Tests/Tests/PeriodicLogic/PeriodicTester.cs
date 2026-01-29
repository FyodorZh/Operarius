namespace Operarius.Tests
{
    public class PeriodicTester_ManualPeriodicLogicDriver : PeriodicLikeTester<PeriodicTestLogic, IPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<IPeriodicLogicDriverCtl> GetDriver()
        {
            return new ManualPeriodicLogicDriver(TimeSpan.FromMilliseconds(10));
        }
    }
    
    public class PeriodicTester_ThreadBasedPeriodicMultiLogicDriver : PeriodicLikeTester<PeriodicTestLogic, IPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<IPeriodicLogicDriverCtl> GetDriver()
        {
            return new ThreadBasedPeriodicMultiLogicDriver(NowDateTimeProvider.Instance, TimeSpan.FromMilliseconds(10));
        }
    }
    
    
}