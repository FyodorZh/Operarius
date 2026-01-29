namespace Operarius
{
    public interface INonPeriodicLogicDriverCtl : ILogicDriverCtl
    {
        /// <summary>
        /// Запрос на получение кванта вне очереди. Работает ассинхронно.
        /// </summary>
        void RequestInvocation();
    }
}