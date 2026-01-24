using System;

namespace Operarius
{
    public class ThreadSafeDateTime : IDateTimeProvider
    {
        private long _time;

        public ThreadSafeDateTime()
            : this(new DateTime())
        { }

        public ThreadSafeDateTime(DateTime time)
        {
            Time = time;
        }

        public DateTime Time
        {
            get
            {
                long time = System.Threading.Interlocked.Read(ref _time);
                return DateTime.FromBinary(time);
            }
            set
            {
                long time = value.ToBinary();
                System.Threading.Interlocked.Exchange(ref _time, time);
            }
        }

        public DateTime Now => Time;
    }
}
