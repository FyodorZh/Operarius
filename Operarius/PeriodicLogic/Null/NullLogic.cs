using System;

namespace Operarius
{
    internal class NullLogic : IPeriodicLogic
    {
        public static readonly NullLogic Instance = new NullLogic();
        
        public bool LogicStarted(ILogicDriverCtl driver)
        {
            throw new InvalidOperationException(nameof(LogicStarted));
        }

        public void LogicTick()
        {
            throw new InvalidOperationException(nameof(LogicTick));
        }

        public void LogicStopped()
        {
            throw new InvalidOperationException(nameof(LogicStopped));
        }
    }
}