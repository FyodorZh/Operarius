using System;

namespace Operarius
{
    public interface IPerformanceMonitor
    {
        TimeSpan UpdatePeriod { get; }

        /// <summary>
        /// Информирует о текущей нагрузке в диапазоне [0..1]
        /// Возможны кратковременные аномалии, когда величина нагрузки становится больше 1
        /// </summary>
        void Set(double performance);
    }

    public class WorkTimeAggregator
    {
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly long _workers;
        private readonly IDateTimeProvider _timeProvider;

        private long _time;

        private DateTime _flushTime;

        public WorkTimeAggregator(IPerformanceMonitor monitor, int workers, IDateTimeProvider timeProvider)
        {
            _performanceMonitor = monitor;
            _workers = Math.Max(workers, 1);
            _flushTime = timeProvider.Now;
            _timeProvider = timeProvider;
        }

        public void Register(System.TimeSpan currentLoad)
        {
            System.Threading.Interlocked.Add(ref _time, (long)(currentLoad.TotalMilliseconds + 0.5));
        }

        public void Flush()
        {
            long workTime = System.Threading.Interlocked.Exchange(ref _time, 0);

            var now = _timeProvider.Now;

            var dt = (now - _flushTime).TotalMilliseconds;
            _flushTime = now;

            _performanceMonitor.Set(workTime / (_workers * dt));
        }
    }
}