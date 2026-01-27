namespace Operarius
{
    public interface INonPeriodicLogicDriverCtl : ILogicDriverCtl
    {
        long CurrentInvocationId { get; }
        /// <summary>
        /// Запрос на получение кванта вне очереди. Работает ассинхронно.
        /// </summary>
        /// <returns> Next invocation ID</returns>
        long RequestInvocation();
    }
}