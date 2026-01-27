namespace Operarius.Tests
{
    public class PeriodicLikeTestLogic<TLogicDriverCtl> : ILogic<TLogicDriverCtl>
        where TLogicDriverCtl : ILogicDriverCtl
    {
        public readonly int TicksCount = 10;
        public bool ErrorOnStart = false;
        public bool CrashOnStart = false;
        public bool CrashOnTick = false;
        public bool CrashOnStop = false;

        public bool StartInvoked = false;
        public int TickInvoked = 0;
        public bool StopInvoked = false;
        
        
        public bool LogicStarted(TLogicDriverCtl driver)
        {
            StartInvoked = true;
            if (ErrorOnStart)
            {
                return false;
            }
            if (CrashOnStart)
            {
                throw new Exception("Crash on start");
            }
            return true;
        }

        public void LogicTick(TLogicDriverCtl driver)
        {
            TickInvoked += 1;
            if (CrashOnTick)
            {
                throw new Exception("Crash on tick");
            }
            if (TickInvoked >= TicksCount)
            {
                driver.Stop();
            }
        }

        public void LogicStopped()
        {
            StopInvoked = true;
            if (CrashOnStop)
            {
                throw new Exception("Crash on stop");
            }
        }
    }
    
    public class PeriodicTestLogic : PeriodicLikeTestLogic<IPeriodicLogicDriverCtl>, IPeriodicLogic
    {
    }
    
    public class SemiPeriodicTestLogic : PeriodicLikeTestLogic<ISemiPeriodicLogicDriverCtl>, ISemiPeriodicLogic
    {
        TimeSpan ISemiPeriodicLogic.LogicTick(ISemiPeriodicLogicDriverCtl driver)
        {
            LogicTick(driver);
            return TimeSpan.FromMilliseconds(13);
        }
    }
}