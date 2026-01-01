using System;
using System.Diagnostics;
using System.Threading;
using Scriba;

namespace Operarius
{
    public class PeriodicLogicThreadedDriver : IPeriodicLogicDriver, ILogicDriverCtl
    {
        private readonly Action<int>? mOnTickDelay;
        private readonly DeltaTime mLogicQuantLength;
        private readonly string? mThreadName;

        private IPeriodicLogic mLogic = NullLogic.Instance;

        private volatile bool mStarted;
        private readonly Thread mThread;

        private AutoResetEvent? mResetEvent;
        private volatile int mInvocationIntention;

        private static int mActiveDriversCount;

        public static int ActiveDriversCount => mActiveDriversCount;

        private static void IncDriversCount()
        {
            Interlocked.Increment(ref mActiveDriversCount);
            Scriba.Log.i("PeriodicLogicThreadedDriver working count: " + ActiveDriversCount);
        }

        private static void DecDriversCount()
        {
            Interlocked.Decrement(ref mActiveDriversCount);
            Scriba.Log.i("PeriodicLogicThreadedDriver working count: " + ActiveDriversCount);
        }

        public ILogger Log { get; private set; }

        public bool IsStarted => mStarted;

        public PeriodicLogicThreadedDriver(DeltaTime period, int maxStackSizeKb = 0, Action<int>? onTickDelay = null, string? threadName = null)
        {
            mOnTickDelay = onTickDelay;
            mLogicQuantLength = period;

            mThreadName = threadName;

            mThread = maxStackSizeKb == 0 ? new Thread(Work) : new Thread(Work, maxStackSizeKb * 1024); // Мона не понимает 0 как дефолтное значение размера стэка
            mThread.IsBackground = true;

            Log = StaticLogger.Instance;
        }

        public DeltaTime Period => mLogicQuantLength;

        public bool Start(IPeriodicLogic logic, ILogger logger)
        {
            if (mLogic != NullLogic.Instance)
            {
                return false;
            }

            mLogic = logic;
            Log = logger;

            mResetEvent = new AutoResetEvent(false);

            try
            {
                try
                {
                    if (!logic.LogicStarted(this))
                    {
                        mLogic.LogicStopped();
                        mLogic = NullLogic.Instance;
                        throw new Exception("LogicStarted() failed");
                    }
                }
                catch (Exception ex)
                {
                    Log.wtf(ex);
                    throw new Exception("LogicStarted() failed with exception");
                }

                try
                {
                    mThread.Name = mThreadName ?? mLogic.GetType().ToString();
                    mStarted = true;
                    mThread.Start();
                }
                catch (Exception ex)
                {
                    Log.wtf(ex);
                    mStarted = false;
                    try
                    {
                        mLogic.LogicStopped();
                        mLogic = NullLogic.Instance;
                    }
                    catch (Exception ex2)
                    {
                        Log.wtf(ex2);
                    }
                    throw new Exception("Failed to start thread");
                }

                return true;
            }
            catch
            {
                mResetEvent.Close();
                mResetEvent = null;
                return false;
            }
        }

        public void Stop()
        {
            mStarted = false;
        }

        public bool InvokeLogic()
        {
            if (mStarted)
            {
                if (mInvocationIntention != 0)
                {
                    return true;
                }
                mInvocationIntention = 1;

                var evt = mResetEvent;
                if (evt != null)
                {
                    try
                    {
                        evt.Set();
                        return true;
                    }
                    catch { }
                }
            }
            return false;
        }

        //~PeriodicLogicThreadedDriver()
        //{
        //    if (mStarted)
        //    {
        //        mStarted = false;
        //        Log.e("PeriodicLogic[" + mLogic + "] was not stopped");
        //    }
        //}

        private void Work(object obj)
        {
            try
            {
                IncDriversCount();

                try
                {
                    Stopwatch sw = new Stopwatch();
                    while (mStarted)
                    {
                        sw.Reset();
                        sw.Start();

                        mLogic.LogicTick();

                        sw.Stop();
                        int timeToSleep = mLogicQuantLength.MilliSeconds - (int)sw.ElapsedMilliseconds;
                        if (timeToSleep > 0)
                        {
                            mResetEvent?.WaitOne(timeToSleep);
                            mInvocationIntention = 0;
                        }
                        else if (timeToSleep < -10 && mLogicQuantLength.MilliSeconds > 0)
                        {
                            if (mOnTickDelay != null)
                            {
                                mOnTickDelay(-timeToSleep);
                            }

                            Log.w("{0}['{1}'] PeriodicLogic tick delay is {2}ms", mLogic, mLogic.GetType(), -timeToSleep);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.wtf(ex);
                }
                finally
                {
                    try
                    {
                        mLogic.LogicStopped();
                        mLogic = NullLogic.Instance;
                    }
                    catch (Exception ex)
                    {
                        Log.wtf(ex);
                    }

                    try
                    {
                        var evt = Interlocked.Exchange(ref mResetEvent, null);
                        evt!.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.wtf(ex);
                    }
                }
            }
            finally
            {
                DecDriversCount();
            }
        }
    }
}
