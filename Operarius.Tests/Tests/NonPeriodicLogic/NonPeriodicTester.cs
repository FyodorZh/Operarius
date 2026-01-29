namespace Operarius.Tests
{
    public class NonPeriodicTester_ThreadBasedNonPeriodicLogicMultiDriver : PeriodicLikeTester<NonPeriodicTestLogic, INonPeriodicLogicDriverCtl>
    {
        protected override ILogicDriver<INonPeriodicLogicDriverCtl> GetDriver()
        {
            return new ThreadBasedNonPeriodicLogicMultiDriver(NowDateTimeProvider.Instance);
        }
    }
}