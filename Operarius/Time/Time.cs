using System;

namespace Operarius
{
    public struct Time : System.IComparable<Time>, IEquatable<Time>//, IDataStruct
    {
        private int _timeMs;

        public static readonly Time Zero = new Time(0);
        public static readonly Time SecondInPast = new Time(-1000);

        /// <summary>
        /// int max value in milliseconds.
        /// Use this value carefully, it is not supported in operators and can cause overflows
        /// </summary>
        public static readonly Time Infinity = new Time(int.MaxValue);

        private Time(double seconds) : this((int)System.Math.Round(seconds * 1000.0)) { }
        private Time(int milliseconds)
        {
            _timeMs = milliseconds;
        }

        public static Time FromMiliseconds(int millisecdonds)
        {
            return new Time(millisecdonds);
        }

        public static Time FromSeconds(double time)
        {
            return new Time(time);
        }

        public static Time Max(Time t1, Time t2)
        {
            return new Time(System.Math.Max(t1._timeMs, t2._timeMs));
        }

        public static Time Min(Time t1, Time t2)
        {
            return new Time(System.Math.Min(t1._timeMs, t2._timeMs));
        }

        public int MilliSeconds
        {
            get
            {
                return _timeMs;
            }
        }

        public double Seconds
        {
            get
            {
                return _timeMs / 1000.0;
            }
        }

        public DeltaTime FromZero
        {
            get
            {
                return DeltaTime.FromMiliseconds(_timeMs);
            }
        }

        public bool IsZero
        {
            get
            {
                return _timeMs == 0;
            }
        }

        public bool IsInfinity
        {
            get
            {
                return _timeMs == Infinity._timeMs;
            }
        }

        public int CompareTo(Time other)
        {
            return _timeMs.CompareTo(other._timeMs);
        }

        public static bool operator <(Time t1, Time t2)
        {
            return t1._timeMs < t2._timeMs;
        }

        public static bool operator >(Time t1, Time t2)
        {
            return t1._timeMs > t2._timeMs;
        }

        public static bool operator <=(Time t1, Time t2)
        {
            return t1._timeMs <= t2._timeMs;
        }

        public static bool operator >=(Time t1, Time t2)
        {
            return t1._timeMs >= t2._timeMs;
        }

        public static bool operator ==(Time t1, Time t2)
        {
            return t1._timeMs == t2._timeMs;
        }

        public static bool operator !=(Time t1, Time t2)
        {
            return t1._timeMs != t2._timeMs;
        }

        public bool Equals(Time other)
        {
            return _timeMs == other._timeMs;
        }

        public override bool Equals(object obj)
        {
            if (obj is Time)
            {
                return this == (Time)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this._timeMs.GetHashCode();
        }

        public static Time operator +(Time time, DeltaTime dTime)
        {
            return new Time(time.MilliSeconds + dTime.MilliSeconds);
        }

        public static Time operator +(DeltaTime dTime, Time time)
        {
            return time + dTime;
        }

        public static DeltaTime operator -(Time time1, Time time2)
        {
            return DeltaTime.FromMiliseconds(time1.MilliSeconds - time2.MilliSeconds);
        }

        public static Time operator -(Time time, DeltaTime dTime)
        {
            return Time.FromMiliseconds(time.MilliSeconds - dTime.MilliSeconds);
        }

        public override string ToString()
        {
            return "[T:" + _timeMs + "]";
        }

        // public void Serialize(ISerializer dst)
        // {
        //     dst.Add(ref mTimeMs);
        // }

        public static Time CropInfinity(Time time, DeltaTime dTime)
        {
            long t = (long)time.MilliSeconds + (long)dTime.MilliSeconds;
            if (t < (long)Infinity.MilliSeconds)
            {
                return new Time((int)t);
            }
            return Infinity;
        }

        public static Time CropInfinity(DeltaTime dTime, Time time)
        {
            return CropInfinity(time, dTime);
        }
    }
}