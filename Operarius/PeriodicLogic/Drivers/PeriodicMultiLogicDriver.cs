using System;

namespace Operarius
{
    public class PeriodicMultiLogicDriver : PeriodicLikeMultiLogicDriver<ManualPeriodicLogicDriver, IPeriodicLogicDriverCtl>
    {
        private readonly TimeSpan _period;

        public PeriodicMultiLogicDriver(TimeSpan period)
        {
            _period = period;
        }
        
        protected override ManualPeriodicLogicDriver ConstructManualDriver()
        {
            return new ManualPeriodicLogicDriver(_period);
        }
    }
}