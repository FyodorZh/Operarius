namespace Operarius.Tests
{
    public class SemiPeriodicTester_ThreadBasedSemiPeriodicMultiLogicDriver : PeriodicLikeTester<SemiPeriodicTestLogic, ISemiPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<ISemiPeriodicLogicDriverCtl> GetDriver()
        {
            return new ThreadBasedSemiPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance);
        }
    }
    
    public class SemiPeriodicTester_ManualSemiPeriodicLogicDriver : PeriodicLikeTester<SemiPeriodicTestLogic, ISemiPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<ISemiPeriodicLogicDriverCtl> GetDriver()
        {
            return new ManualSemiPeriodicLogicDriver();
        }
    }
    
    public class SemiPeriodicTester_ThreadBasedSemiPeriodicLogicDriver : PeriodicLikeTester<SemiPeriodicTestLogic, ISemiPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<ISemiPeriodicLogicDriverCtl> GetDriver()
        {
            return new ThreadBasedSemiPeriodicLogicDriver(UtcNowDateTimeProvider.Instance, true);
        }
    }
}