using System;
using Actuarius.Concurrent;

namespace Operarius
{
    /// <summary>
    /// Represents a contract for logic that is executed periodically, providing methods for initialization, periodic tasks,
    /// and cleanup during its lifecycle.
    /// </summary>
    public interface IPeriodicLogic : ILogic<IPeriodicLogicDriverCtl>
    {        
        /// <summary>
        /// Invoked periodically while the logic is in the running state.
        /// </summary>
        /// <remarks>
        /// This method is called after <see cref="LogicStarted"/> has been successfully invoked
        /// and continues to be executed at some intervals until <see cref="LogicStopped"/> is called.
        /// </remarks>
        void LogicTick(IPeriodicLogicDriverCtl driver);
    }

    public static class PeriodicLogicChecker
    {
        public static IPeriodicLogic Test(this IPeriodicLogic core, Action<string> onFail)
        {
#if DEBUG
            return new Wrapper(core, onFail);
#else
            return core;
#endif
        }

        private class Wrapper : InvariantChecker<Wrapper.LogicState>, IPeriodicLogic
        {
            public enum LogicState
            {
                Constructed,
                Started,
                Stopped
            }

            private readonly IPeriodicLogic _logic;

            private int _flag;

            public Wrapper(IPeriodicLogic logic, Action<string> onFail)
                : base(0, onFail)
            {
                _logic = logic;
            }

            protected override int FromState(LogicState state)
            {
                return (int)state;
            }

            protected override LogicState ToState(int state)
            {
                return (LogicState)state;
            }

            bool ILogic<IPeriodicLogicDriverCtl>.LogicStarted(IPeriodicLogicDriverCtl driver)
            {
                bool res;
                BeginCriticalSection(ref _flag);
                {
                    CheckState(LogicState.Constructed);
                    res = _logic.LogicStarted(driver);
                    if (res)
                    {
                        SetState(LogicState.Started);
                    }
                }
                EndCriticalSection(ref _flag);
                return res;
            }

            void IPeriodicLogic.LogicTick(IPeriodicLogicDriverCtl driver)
            {
                BeginCriticalSection(ref _flag);
                {
                    CheckState(LogicState.Started);
                    _logic.LogicTick(driver);
                }
                EndCriticalSection(ref _flag);
            }

            void ILogic<IPeriodicLogicDriverCtl>.LogicStopped()
            {
                BeginCriticalSection(ref _flag);
                {
                    if (State == LogicState.Stopped)
                    {
                        Fail();
                    }
                    SetState(LogicState.Stopped);
                    _logic.LogicStopped();
                }
                EndCriticalSection(ref _flag);
            }
        }
    }
}