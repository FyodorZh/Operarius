using System;

namespace Operarius
{
    public class ManualSemiPeriodicLogicDriver : PeriodicLikeLogicManualDriver<ISemiPeriodicLogicDriverCtl>, ISemiPeriodicLogicDriverCtl
    {
        public ManualSemiPeriodicLogicDriver()
        {
            var time = DateTime.MinValue.AddSeconds(1);
            _prevTickTime = time;
            _currentTime = time;
            _nextTickTime = time;
        }

        protected override bool InvokeStart(ILogic<ISemiPeriodicLogicDriverCtl> logic)
        {
            return logic.LogicStarted(this);
        }

        protected override void InvokeTick(ILogic<ISemiPeriodicLogicDriverCtl> logic)
        {
            var delay = ((ISemiPeriodicLogic)logic).LogicTick(this);
            if (delay < TimeSpan.Zero)
            {
                throw new InvalidOperationException("The returned delay must be non-negative.");
            }
            _nextTickTime = _prevTickTime + delay;
        }
    }
}