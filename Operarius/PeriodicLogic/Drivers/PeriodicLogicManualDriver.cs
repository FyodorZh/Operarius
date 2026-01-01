using System;
using Scriba;

namespace Operarius
{
    public class PeriodicLogicManualDriver : IPeriodicLogicDriver, ILogicDriverCtl
    {
        private enum State
        {
            Constructed,
            Started,
            Stopped
        }

        private IPeriodicLogic mLogic = NullLogic.Instance;
        private ILogger mLogger;

        private volatile State mState;

        private volatile bool mIntentionToStop = false;
        private volatile bool mInvokeIntention = false;

        private readonly DeltaTime mPeriod;
        private System.DateTime mNextTickTime = new System.DateTime();

        private readonly Func<bool>? mInvokeLogicAction;

        public IPeriodicLogic Logic => mLogic;

        public PeriodicLogicManualDriver(DeltaTime period, Func<bool>? invokeLogicAction = null)
        {
            mState = State.Constructed;
            mPeriod = period;
            mInvokeLogicAction = invokeLogicAction;
            mLogger = StaticLogger.Instance;
        }

        public DeltaTime Period => mPeriod;

        public bool Start(IPeriodicLogic logic, ILogger logger)
        {
            if (mState == State.Constructed)
            {
                mLogger = logger;
                mLogic = logic;

                mState = State.Started;

                try
                {
                    if (!logic.LogicStarted(this))
                    {
                        mState = State.Stopped;
                        mLogic.LogicStopped();
                        mLogic = NullLogic.Instance;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.wtf(ex);
                    mState = State.Stopped;
                    return false;
                }

                return true;
            }
            return false;
        }

        public bool Tick()
        {
            return Tick(mNextTickTime);
        }

        public bool Tick(DateTime now)
        {
            switch (mState)
            {
                case State.Started:
                    if (!mIntentionToStop)
                    {
                        if (mInvokeIntention || now >= mNextTickTime)
                        {
                            mInvokeIntention = false;
                            mNextTickTime = now.AddMilliseconds(mPeriod.MilliSeconds);
                            try
                            {
                                mLogic.LogicTick();
                            }
                            catch (Exception e)
                            {
                                mState = State.Stopped;
                                Log.wtf(e);
                                throw;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        mState = State.Stopped;
                        try
                        {
                            mLogic.LogicStopped();
                            mLogic = NullLogic.Instance;
                        }
                        catch (Exception exception)
                        {
                            Log.wtf(exception);
                        }
                        return false;
                    }
                default:
                    return false;
            }
        }

        public void Stop()
        {
            mIntentionToStop = true;
        }

        public void StopAndTick()
        {
            Stop();
            Tick();
        }

        #region IPeriodicLogicDriver
        public bool IsStarted => /*!mIntentionToStop &&*/mState == State.Started;

        public ILogger Log => mLogger;

        public bool InvokeLogic()
        {
            mInvokeIntention = true;
            if (mState == State.Started)
            {
                return mInvokeLogicAction == null || mInvokeLogicAction();
            }

            return false;
        }

        #endregion
    }
}