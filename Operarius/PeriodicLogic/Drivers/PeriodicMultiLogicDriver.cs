using System;

namespace Operarius
{
    public class PeriodicMultiLogicDriver : PeriodicLikeMultiLogicDriver<ManualPeriodicLogicDriver, IPeriodicLogicDriverCtl>
    {
        private readonly TimeSpan _period;

        public PeriodicMultiLogicDriver(TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
            }
            _period = period;
        }
        
        protected override ManualPeriodicLogicDriver ConstructManualDriver()
        {
            return new ManualPeriodicLogicDriver(_period);
        }
    }
}