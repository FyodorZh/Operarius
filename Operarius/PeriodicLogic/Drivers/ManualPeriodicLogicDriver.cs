using System;

namespace Operarius
{
    public class ManualPeriodicLogicDriver : PeriodicLikeLogicManualDriver<IPeriodicLogicDriverCtl>, IPeriodicLogicDriverCtl
    {
        private readonly TimeSpan _period;

        TimeSpan IPeriodicLogicDriverCtl.Period => _period;

        public ManualPeriodicLogicDriver(TimeSpan period)
        {
            _period = period;
            _prevTickTime = DateTime.MinValue;
            _currentTime = _prevTickTime + period;
            _nextTickTime = _currentTime;
        }

        protected override bool InvokeStart(ILogic<IPeriodicLogicDriverCtl> logic)
        {
            return logic.LogicStarted(this);
        }

        protected override void InvokeTick(ILogic<IPeriodicLogicDriverCtl> logic)
        {
            ((IPeriodicLogic)logic).LogicTick(this);
            _nextTickTime = _prevTickTime + _period;
        }
    }
}