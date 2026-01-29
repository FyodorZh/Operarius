using System;
using System.Threading.Tasks;

namespace Operarius
{
    public class ThreadBasedPeriodicMultiLogicDriver : ILogicDriver<IPeriodicLogicDriverCtl>
    {
        private readonly ThreadBasedSemiPeriodicLogicDriver _threadDriver;
        private readonly PeriodicMultiLogicDriver _multiDriver;
        
        public event Action<ILogic<IPeriodicLogicDriverCtl>>? LogicStopped
        {
            add => _multiDriver.LogicStopped += value;
            remove => _multiDriver.LogicStopped -= value;
        }
        public event Action<Exception>? ErrorStream;

        public ThreadBasedPeriodicMultiLogicDriver(IDateTimeProvider timeProvider, TimeSpan period)
        {
            _multiDriver = new PeriodicMultiLogicDriver(period);
            _threadDriver = new ThreadBasedSemiPeriodicLogicDriver(timeProvider, true);
            _multiDriver.ErrorStream += e => ErrorStream?.Invoke(e);
            _threadDriver.ErrorStream += e => ErrorStream?.Invoke(e);
            _threadDriver.Start(_multiDriver);
        }
        
        public LogicStartResult Start(ILogic<IPeriodicLogicDriverCtl> logic)
        {
            return _multiDriver.Start(logic);
        }

        Task ILogicDriver<IPeriodicLogicDriverCtl>.Finish()
        {
            return _threadDriver.Finish();
        }
        
        Task ILogicDriver<IPeriodicLogicDriverCtl>.WaitForFinish()
        {
            return _threadDriver.WaitForFinish();
        }
    }
}