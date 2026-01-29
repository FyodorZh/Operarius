using System;
using System.Threading;
using System.Threading.Tasks;

namespace Operarius
{
    public class ThreadBasedSemiPeriodicLogicDriver : ILogicDriver<ISemiPeriodicLogicDriverCtl>
    {
        private readonly IDateTimeProvider _timeProvider;
        private readonly ManualSemiPeriodicLogicDriver _driver = new ();
        private readonly bool _finishOnComplete;
        
        private AutoResetEvent? _resetEvent;
        private bool _intentionToDestroy;
        private readonly TaskCompletionSource<int> _waitForFinishTcs = new();

        private readonly object _locker = new();
        
        public event Action<ILogic<ISemiPeriodicLogicDriverCtl>>? LogicStopped;
        public event Action<Exception>? ErrorStream;

        public  ThreadBasedSemiPeriodicLogicDriver(IDateTimeProvider timeProvider, bool finishOnComplete)
        {
            _timeProvider = timeProvider;
            _finishOnComplete = finishOnComplete;
            _driver.ErrorStream += e => ErrorStream?.Invoke(e);
            _driver.LogicStopped += logic => LogicStopped?.Invoke(logic);
            var thread = new Thread(Work, 256 * 1024);
            _resetEvent = new AutoResetEvent(false);
            thread.Start();
        }

        private void Work()
        {
            try
            {
                while (true)
                {
                    _resetEvent!.WaitOne();
                    if (_intentionToDestroy)
                    {
                        return;
                    }

                    while (true)
                    {
                        var now = _timeProvider.Now;
                        if (!_driver.Tick(now))
                        {
                            if (_finishOnComplete)
                            {
                                return;
                            }
                            break;
                        }

                        var dt = _driver.NextTickTime - now;
                        if (dt > TimeSpan.Zero)
                        {
                            Thread.Sleep(dt);
                        }

                        if (_intentionToDestroy)
                        {
                            _driver.StopNow();
                            return;
                        }
                    }
                }
            }
            finally
            {
                lock (_locker)
                {
                    _resetEvent!.Dispose();
                    _resetEvent = null;
                }

                _waitForFinishTcs.SetResult(1);
            }
        }
        
        public LogicStartResult Start(ILogic<ISemiPeriodicLogicDriverCtl> logic)
        {
            lock (_locker)
            {
                if (_resetEvent != null)
                {
                    var res = _driver.Start(logic);
                    if (res == LogicStartResult.Success)
                    {
                        _resetEvent.Set();
                        return LogicStartResult.Success;
                    }

                    if (_finishOnComplete)
                    {
                        Finish();
                    }
                    return res;
                }

                return LogicStartResult.DriverIsNotActive;
            }
        }

        public Task Finish()
        {
            lock (_locker)
            {
                _intentionToDestroy = true;
                _resetEvent?.Set();
            }

            return WaitForFinish();
        }

        public Task WaitForFinish()
        {
            return _waitForFinishTcs.Task;
        }
    }
}