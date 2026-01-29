using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Actuarius.Collections;
using Actuarius.Concurrent;

namespace Operarius
{
    public class ThreadBasedNonPeriodicLogicMultiDriver : ILogicDriver<INonPeriodicLogicDriverCtl>
    {
        private readonly IDateTimeProvider _timeProvider;
        private readonly AutoResetEvent _resetEvent;

        private readonly ConcurrentUnorderedCollectionValve<NonPeriodicLogicDriverCtl> _logicsToAdd;
        private readonly IConcurrentUnorderedCollection<NonPeriodicLogicDriverCtl> _logicsToInvoke = 
            new SystemConcurrentUnorderedCollection<NonPeriodicLogicDriverCtl>();
        private readonly IConcurrentUnorderedCollection<NonPeriodicLogicDriverCtl> _logicsToStop =
            new SystemConcurrentUnorderedCollection<NonPeriodicLogicDriverCtl>();
        

        private readonly HashSet<NonPeriodicLogicDriverCtl> _logics = new();
        
        private readonly TaskCompletionSource<int> _finishTcs = new ();

        private readonly Action<ILogic<INonPeriodicLogicDriverCtl>> _logicStoppedReport;
        private readonly Action<Exception> _errorReport;
        
        private int _intentionToDestroy;
        
        
        public event Action<ILogic<INonPeriodicLogicDriverCtl>>? LogicStopped;
        public event Action<Exception>? ErrorStream;

        public ThreadBasedNonPeriodicLogicMultiDriver(IDateTimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            var thread = new Thread(Work, 256 * 1024);
            _resetEvent = new AutoResetEvent(false);
            thread.Start();
            
            _logicsToAdd = new ConcurrentUnorderedCollectionValve<NonPeriodicLogicDriverCtl>(
                new SystemConcurrentUnorderedCollection<NonPeriodicLogicDriverCtl>(), d =>
                {
                    d.StopRightNow();
                });

            _errorReport = ReportError;
            _logicStoppedReport = ReportLogicStopped;
        }
        
        private void Work()
        {
            while (true)
            {
                _resetEvent.WaitOne();
                
                if (Volatile.Read(ref _intentionToDestroy) == 1)
                {
                    _logicsToAdd.CloseValve();
                    foreach (var logic in _logics)
                    {
                        logic.StopRightNow();
                    }
                    _logics.Clear();
                    break;
                }

                while (_logicsToAdd.TryPop(out var logicToAdd))
                {
                    _logics.Add(logicToAdd);
                }
                
                while (_logicsToInvoke.TryPop(out var logicToInvoke))
                {
                    logicToInvoke.Tick();
                }

                while (_logicsToStop.TryPop(out var logicToInvoke))
                {
                    _logics.Remove(logicToInvoke);
                    logicToInvoke.StopRightNow();
                }
            }
            _finishTcs.SetResult(0);
        }
        
        LogicStartResult ILogicDriver<INonPeriodicLogicDriverCtl>.Start(ILogic<INonPeriodicLogicDriverCtl> logic)
        {
            if (Volatile.Read(ref _intentionToDestroy) == 0)
            {
                if (logic is not INonPeriodicLogic nonPeriodicLogic)
                {
                    return LogicStartResult.FailedToStart;
                }
                
                NonPeriodicLogicDriverCtl driver = new NonPeriodicLogicDriverCtl(this);
                var res = driver.Start(nonPeriodicLogic);
                if (res == LogicStartResult.Success)
                {
                    if (_logicsToAdd.EnqueueEx(driver) == ValveEnqueueResult.Ok)
                    {
                        _resetEvent.Set();
                        return LogicStartResult.Success;
                    }

                    return LogicStartResult.DriverIsNotActive;
                }
                return res;
            }
            return LogicStartResult.DriverIsNotActive;
        }

        Task ILogicDriver<INonPeriodicLogicDriverCtl>.Finish()
        {
            Volatile.Write(ref _intentionToDestroy, 1);
            _resetEvent.Set();
            return ((ILogicDriver<INonPeriodicLogicDriverCtl>)this).WaitForFinish();
        }

        Task ILogicDriver<INonPeriodicLogicDriverCtl>.WaitForFinish()
        {
            return _finishTcs.Task;
        }
        
        private void ReportLogicStopped(ILogic<INonPeriodicLogicDriverCtl> logic)
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
        
        private class NonPeriodicLogicDriverCtl : INonPeriodicLogicDriverCtl
        {
            private readonly ThreadBasedNonPeriodicLogicMultiDriver _owner;

            private INonPeriodicLogic? _logic;

            private int _stopRequested;
            private int _invocationRequested;

            public NonPeriodicLogicDriverCtl(ThreadBasedNonPeriodicLogicMultiDriver owner)
            {
                _owner = owner;
            }

            public LogicStartResult Start(INonPeriodicLogic logic)
            {
                Volatile.Write(ref _logic, logic);
                try
                {
                    if (!logic.LogicStarted(this))
                    {
                        try
                        {
                            logic.LogicStopped();
                        }
                        catch (Exception ex2)
                        {
                            _owner._errorReport(ex2);
                        }
                        _owner._logicStoppedReport(logic);

                        Volatile.Write(ref _logic, null);
                        return LogicStartResult.FailedToStart;
                    }
                }
                catch (Exception ex)
                {
                    _owner._errorReport(ex);
                    
                    try
                    {
                        logic.LogicStopped();
                    }
                    catch (Exception ex2)
                    {
                        _owner._errorReport(ex2);
                    }
                    _owner._logicStoppedReport(logic);
                    
                    Volatile.Write(ref _logic, null);
                    return LogicStartResult.FailedToStart;
                }

                return LogicStartResult.Success;
            }

            public void Tick()
            {
                var logic = Volatile.Read(ref _logic);
                if (logic != null)
                {
                    try
                    {
                        Volatile.Write(ref _invocationRequested, 0);
                        logic.LogicTick(this);
                    }
                    catch (Exception ex)
                    { 
                        _owner._errorReport(ex);
                        StopRightNow();
                    }
                }
            }

            public void StopRightNow()
            {
                var logic = Interlocked.Exchange(ref _logic, null);
                if (logic != null)
                {
                    try
                    {
                        logic.LogicStopped();
                    }
                    catch (Exception ex)
                    { 
                        _owner._errorReport(ex);
                    }
                    _owner._logicStoppedReport(logic);
                }
            }

            DateTime ILogicDriverCtl.CurrentTime => _owner._timeProvider.Now;

            void ILogicDriverCtl.Stop()
            {
                if (Interlocked.CompareExchange(ref _stopRequested, 1, 0) == 0)
                {
                    _owner._logicsToStop.Put(this);
                    _owner._resetEvent.Set();
                }
            }

            void INonPeriodicLogicDriverCtl.RequestInvocation()
            {
                if (Interlocked.CompareExchange(ref _invocationRequested, 1, 0) == 0)
                {
                    _owner._logicsToInvoke.Put(this);
                    _owner._resetEvent.Set();
                }
            }
        }
    }
}