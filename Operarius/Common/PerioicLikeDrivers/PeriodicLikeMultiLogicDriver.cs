using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actuarius.Collections;
using Actuarius.Concurrent;

namespace Operarius
{
    public abstract class PeriodicLikeMultiLogicDriver<TManualLogicDriver, TLogicDriverCtl> : ILogicDriver<TLogicDriverCtl>, ISemiPeriodicLogic
        where TLogicDriverCtl : class, ILogicDriverCtl
        where TManualLogicDriver : PeriodicLikeLogicManualDriver<TLogicDriverCtl>
    {
        private enum State { Constructed, Running, Stopped }

        private readonly TimeSpan EmptyFrameDuration = TimeSpan.FromMilliseconds(50);

        private readonly Action<ILogic<TLogicDriverCtl>> _reportLogicStopped;
        private readonly Action<Exception> _reportError;
        
        private readonly ConcurrentUnorderedCollectionValve<TManualLogicDriver> _driversToAdd;
        private readonly PriorityQueue<DateTime, TManualLogicDriver> _drivers = new();
        private readonly List<TManualLogicDriver> _processedDrivers = new();

        private readonly TaskCompletionSource<int> _waitForFinishTcs = new();
        
        private volatile State _state = State.Constructed;

        private volatile bool _intentionToFinish;

        public event Action<ILogic<TLogicDriverCtl>>? LogicStopped;
        public event Action<Exception>? ErrorStream;
        
        protected abstract TManualLogicDriver ConstructManualDriver();

        protected PeriodicLikeMultiLogicDriver()
        {
            _reportLogicStopped = ReportLogicStopped;
            _reportError = ReportError;
            _driversToAdd = new(new SystemConcurrentUnorderedCollection<TManualLogicDriver>(), 
                d => d.StopNow());
        }
        
        public LogicStartResult Start(ILogic<TLogicDriverCtl> logic)
        {
            if (_state != State.Running)
            {
                return LogicStartResult.DriverIsNotActive;
            }

            var manualDriver = ConstructManualDriver();
            manualDriver.LogicStopped += _reportLogicStopped;
            manualDriver.ErrorStream += _reportError;

            var res = manualDriver.Start(logic);
            if (res == LogicStartResult.Success)
            {
                _driversToAdd.Put(manualDriver);
                return LogicStartResult.Success;
            }

            manualDriver.ErrorStream -= _reportError;
            return res;
        }

        bool ILogic<ISemiPeriodicLogicDriverCtl>.LogicStarted(ISemiPeriodicLogicDriverCtl driver)
        {
            _state = State.Running;
            return true;
        }

        TimeSpan ISemiPeriodicLogic.LogicTick(ISemiPeriodicLogicDriverCtl driver)
        {
            var now = driver.CurrentTime;
            
            while (_driversToAdd.TryPop(out var d))
            {
                var nextTickTime = d.NextTickTime;
                _drivers.Put(new KeyValuePair<DateTime, TManualLogicDriver>(nextTickTime, d));
            }
            
            while (_drivers.Count > 0 && _drivers.TopKey() <= now)
            {
                _drivers.TryPop(out var kv);
                if (kv.Value.Tick(now))
                {
                    _processedDrivers.Add(kv.Value);
                }
                else
                {
                    kv.Value.ErrorStream -= _reportError;
                }
            }
            
            foreach (var d in _processedDrivers)
            {
                var nextTickTime = d.NextTickTime;
                _drivers.Put(new KeyValuePair<DateTime, TManualLogicDriver>(nextTickTime, d));
            }
            _processedDrivers.Clear();

            if (_intentionToFinish)
            {
                driver.Stop();
            }
            
            if (_drivers.Count > 0)
            {
                var dt = _drivers.TopKey() - now;
                return dt;
            }
            return EmptyFrameDuration;
        }

        void ILogic<ISemiPeriodicLogicDriverCtl>.LogicStopped()
        {
            _state = State.Stopped;
            _driversToAdd.CloseValve();
            while (_drivers.TryPop(out var kv))
            {
                kv.Value.StopNow();
                kv.Value.ErrorStream -= _reportError;
            }

            _waitForFinishTcs.SetResult(1);
        }
        
        Task ILogicDriver<TLogicDriverCtl>.Finish()
        {
            _intentionToFinish = true;
            return _waitForFinishTcs.Task;
        }
        
        Task ILogicDriver<TLogicDriverCtl>.WaitForFinish()
        {
            return _waitForFinishTcs.Task;
        }
        
        private void ReportLogicStopped(ILogic<TLogicDriverCtl> logic)
        {
            try
            {
                LogicStopped?.Invoke(logic);
            }
            catch
            {
                // ignored
            }
        }

        private void ReportError(Exception exception)
        {
            try
            {
                ErrorStream?.Invoke(exception);
            }
            catch
            {
                // ignored
            }
        }
    }
}