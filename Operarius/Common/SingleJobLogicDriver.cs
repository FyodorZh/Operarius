using System;
using System.Threading;
using System.Threading.Tasks;

namespace Operarius
{
    public class SingleJobLogicDriver<TLogicDriverCtl> : ILogicDriver<TLogicDriverCtl>
        where TLogicDriverCtl : ILogicDriverCtl
    {
        private readonly ILogicDriver<TLogicDriverCtl> _coreDriver;
        private int _stage = 0;

        public event Action<ILogic<TLogicDriverCtl>>? LogicStopped;
        public event Action<Exception>? ErrorStream;

        public SingleJobLogicDriver(ILogicDriver<TLogicDriverCtl> coreDriver)
        {
            _coreDriver = coreDriver;
            _coreDriver.LogicStopped += OnLogicStopped;
            _coreDriver.ErrorStream += OnError;
        }

        public LogicStartResult Start(ILogic<TLogicDriverCtl> logic)
        {
            if (Interlocked.CompareExchange(ref _stage, 1, 0) == 0)
            {
                return _coreDriver.Start(logic);
            }

            return LogicStartResult.CapacityExceeded;
        }

        public Task Finish()
        {
            return _coreDriver.Finish();
        }

        public Task WaitForFinish()
        {
            return _coreDriver.WaitForFinish();
        }

        private void OnLogicStopped(ILogic<TLogicDriverCtl> logic)
        {
            Finish().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    OnError(task.Exception);
                }
            });
            LogicStopped?.Invoke(logic);
        }
        
        private void OnError(Exception exception)
        {
            ErrorStream?.Invoke(exception);
        }
    }
}