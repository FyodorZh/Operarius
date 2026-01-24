using System;
using System.Collections.Generic;
using System.Diagnostics;
using Actuarius.Collections;
using Actuarius.Concurrent;
using Scriba;

namespace Operarius
{
    public class PeriodicMultiLogicDriver : IPeriodicMultiLogicDriver, IPeriodicLogic
    {
        private readonly List<PeriodicLogicManualDriver> mLogics = new List<PeriodicLogicManualDriver>();

        private readonly ConcurrentQueueValve<PeriodicLogicManualDriver> mPendingToAppend;

        private readonly IPeriodicLogicDriver mDriver;
        private readonly IDateTimeProvider _timeProvider;
        private ILogicDriverCtl? mDriverCtl;

        private readonly WorkTimeAggregator? mWorkAggregator;
        private System.TimeSpan mStatisticsFlushPeriod;
        private DateTime mStatisticsFlushTime;

        private readonly Stopwatch mTimer = new Stopwatch();

        private ILogger Log { get; set; }

        public int Count { get; private set; }

        public PeriodicMultiLogicDriver(IPeriodicLogicDriver driver, IDateTimeProvider timeProvider, IPerformanceMonitor? monitor = null)
        {
            mPendingToAppend = new ConcurrentQueueValve<PeriodicLogicManualDriver>(new TinyConcurrentQueue<PeriodicLogicManualDriver>(), d => d.StopAndTick());

            mDriver = driver;
            _timeProvider = timeProvider;
            Log = StaticLogger.Instance;
            Count = 0;

            mWorkAggregator = monitor != null ? new WorkTimeAggregator(monitor, 1, timeProvider) : null;
            mStatisticsFlushPeriod = monitor?.UpdatePeriod ?? TimeSpan.Zero;
        }

        public bool Start(ILogger logger)
        {
            Log = logger;
            return mDriver.Start(this, logger);
        }

        public void Stop()
        {
            var ctl = mDriverCtl;
            if (ctl != null)
            {
                ctl.Stop();
            }
        }

        public ILogicDriverCtl? Append(IPeriodicLogic logic, DeltaTime period)
        {
            if (mDriverCtl == null)
            {
                return null;
            }

            var manualDriver = new PeriodicLogicManualDriver(period);
            if (manualDriver.Start(logic, Log))
            {
                mPendingToAppend.Put(manualDriver);
                return manualDriver;
            }
            return null;
        }

        bool IPeriodicLogic.LogicStarted(ILogicDriverCtl driver)
        {
            mDriverCtl = driver;
            mStatisticsFlushTime = _timeProvider.Now.Add(mStatisticsFlushPeriod);
            return true;
        }

        void IPeriodicLogic.LogicTick()
        {
            mTimer.Reset();
            mTimer.Start();

            System.DateTime now = _timeProvider.Now;

            while (mPendingToAppend.TryPop(out var driver))
            {
                mLogics.Add(driver);
            }

            int lastPos = mLogics.Count - 1;
            for (int i = lastPos; i >= 0; --i)
            {
                var logic = mLogics[i];
                logic.Tick(now);
                if (!logic.IsStarted)
                {
                    mLogics[i] = mLogics[lastPos];
                    mLogics[lastPos--] = null!;
                }
            }
            mLogics.RemoveRange(lastPos + 1, mLogics.Count - (lastPos + 1));

            Count = mLogics.Count;

            mTimer.Stop();
            if (mWorkAggregator != null)
            {
                mWorkAggregator.Register(mTimer.Elapsed);
                if (mStatisticsFlushTime <= now)
                {
                    mStatisticsFlushTime = now.Add(mStatisticsFlushPeriod);
                    mWorkAggregator.Flush();
                }
            }
        }

        void IPeriodicLogic.LogicStopped()
        {
            mDriverCtl = null;
            mPendingToAppend.CloseValve();

            for (int i = mLogics.Count - 1; i >= 0; --i)
            {
                mLogics[i].StopAndTick();
            }
            mLogics.Clear();

            Count = 0;
        }
    }
}
