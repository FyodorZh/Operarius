namespace Operarius
{
    /// <summary>
    /// Represents a contract for logic that is executed periodically, providing methods for initialization, periodic tasks,
    /// and cleanup during its lifecycle. The actual invocation strategy is controlled by the inherited implementations.
    /// </summary>
    public interface ILogic<in TLogicDriverCtl>
        where TLogicDriverCtl : ILogicDriverCtl
    {
        /// <summary>
        /// Called once before any other methods to initialize the logic.
        /// </summary>
        /// <param name="driver">Responsible for managing and allocating processor time for the logic.</param>
        /// <returns>
        /// Returns <c>false</c> if the logic failed to initialize successfully.
        /// In case the initialization fails, <see cref="LogicStopped"/> will be invoked immediately after.
        /// </returns>
        bool LogicStarted(TLogicDriverCtl driver);

        /// <summary>
        /// Invoked once when the logic is terminated for cleanup and resource deallocation.
        /// </summary>
        void LogicStopped();
    }
}