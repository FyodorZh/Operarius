using System;
using System.Threading.Tasks;

namespace Operarius
{
    public class ThreadBasedSemiPeriodicMultiLogicDriver :  ILogicDriver<ISemiPeriodicLogicDriverCtl>
    {
        private readonly ThreadBasedSemiPeriodicLogicDriver _threadDriver;
        private readonly SemiPeriodicMultiLogicDriver _multiDriver;
        
        public event Action<ILogic<ISemiPeriodicLogicDriverCtl>>? LogicStopped
        {
            add => _multiDriver.LogicStopped += value;
            remove => _multiDriver.LogicStopped -= value;
        }
        public event Action<Exception>? ErrorStream;

        public ThreadBasedSemiPeriodicMultiLogicDriver(IDateTimeProvider timeProvider)
        {
            _multiDriver = new SemiPeriodicMultiLogicDriver();
            _threadDriver = new ThreadBasedSemiPeriodicLogicDriver(timeProvider, true);
            _multiDriver.ErrorStream += e => ErrorStream?.Invoke(e);
            _threadDriver.ErrorStream += e => ErrorStream?.Invoke(e);
            _threadDriver.Start(_multiDriver);
        }
        
        public LogicStartResult Start(ILogic<ISemiPeriodicLogicDriverCtl> logic)
        {
            return _multiDriver.Start(logic);
        }

        Task ILogicDriver<ISemiPeriodicLogicDriverCtl>.Finish()
        {
            return _threadDriver.Finish();
        }
        
        Task ILogicDriver<ISemiPeriodicLogicDriverCtl>.WaitForFinish()
        {
            return _threadDriver.WaitForFinish();
        }
    }
}