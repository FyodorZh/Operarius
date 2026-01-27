using System;
using System.Threading;
using System.Threading.Tasks;

namespace Operarius
{
    public abstract class PeriodicLikeLogicManualDriver<TLogicDriverCtl> : ILogicDriver<TLogicDriverCtl>, ILogicDriverCtl
        where TLogicDriverCtl : class, ILogicDriverCtl
    {
        private ILogic<TLogicDriverCtl>? _logic;
        private volatile bool _intentionToStop;
        
        protected DateTime _currentTime = DateTime.MinValue;
        protected DateTime _prevTickTime = DateTime.MinValue;
        protected DateTime _nextTickTime = DateTime.MinValue;
        
        public event Action<ILogic<TLogicDriverCtl>>? LogicStopped;
        public event Action<Exception>? ErrorStream;
        
        public DateTime NextTickTime => _nextTickTime;
        
        public DateTime CurrentTime => _currentTime;
        
        public bool IsRunning => Volatile.Read(ref _logic) != null;
        
        protected abstract bool InvokeStart(ILogic<TLogicDriverCtl> logic);
        protected abstract void InvokeTick(ILogic<TLogicDriverCtl> logic);
        
        public LogicStartResult Start(ILogic<TLogicDriverCtl> logic)
        {
            if (Interlocked.CompareExchange(ref _logic, logic, null) == null)
            {
                try
                {
                    if (!InvokeStart(_logic))
                    {
                        try
                        {
                            _logic.LogicStopped();
                        }
                        catch (Exception ex2)
                        {
                            ReportError(ex2);
                        }
                        ReportLogicStopped(_logic);

                        Volatile.Write(ref _logic, null);
                        return LogicStartResult.FailedToStart;
                    }
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                    
                    try
                    {
                        logic.LogicStopped();
                    }
                    catch (Exception ex2)
                    {
                        ReportError(ex2);
                    }
                    ReportLogicStopped(logic);
                    
                    Volatile.Write(ref _logic, null);
                    return LogicStartResult.FailedToStart;
                }

                return LogicStartResult.Success;
            }
            return LogicStartResult.CapacityExceeded;
        }

        Task ILogicDriver<TLogicDriverCtl>.Finish()
        {
            throw new NotSupportedException();
        }
        
        Task ILogicDriver<TLogicDriverCtl>.WaitForFinish()
        {
            throw new NotSupportedException();
        }
        
        public bool Tick()
        {
            return Tick(_nextTickTime);
        }

        public bool Tick(DateTime now)
        {
            _currentTime = now;
            return DoTick();
        }

        protected bool DoTick()
        {
            var logic = Volatile.Read(ref _logic);
            if (logic != null)
            {
                if (!_intentionToStop)
                {
                    if (_currentTime >= _nextTickTime)
                    {
                        _prevTickTime = _currentTime;
                        try
                        {
                            InvokeTick(logic);
                        }
                        catch (Exception e)
                        {
                            ReportError(e);
                            try
                            {
                                logic.LogicStopped();
                            }
                            catch (Exception ex2)
                            {
                                ReportError(ex2);
                            }
                            ReportLogicStopped(logic);
                            
                            Volatile.Write(ref _logic, null);
                            return false;
                        }
                    }
                }

                if (_intentionToStop)
                {
                    try
                    {
                        logic.LogicStopped();
                    }
                    catch (Exception ex)
                    {
                        ReportError(ex);
                    }
                    ReportLogicStopped(logic);

                    _intentionToStop = false;
                    Volatile.Write(ref logic, null);
                    return false;
                }
                
                return true;
            }
            return false;
        }

        void ILogicDriverCtl.Stop()
        {
            _intentionToStop = true;
        }

        public void StopNow()
        {
            _intentionToStop = true;
            DoTick();
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