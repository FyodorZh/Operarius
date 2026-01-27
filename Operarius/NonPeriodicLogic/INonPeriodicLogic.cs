namespace Operarius
{
    /// <summary>
    /// Represents a contract for logic that is executed on demand, providing methods for initialization,
    /// execution, and cleanup during its lifecycle.
    /// </summary>
    public interface INonPeriodicLogic : ILogic<INonPeriodicLogicDriverCtl>
    {        
        /// <summary>
        /// Invoked periodically while the logic is in the running state.
        /// </summary>
        /// <remarks>
        /// This method is called after <see cref="LogicStarted"/> has been successfully invoked
        /// and continues to be executed at some intervals until <see cref="LogicStopped"/> is called.
        /// </remarks>
        void LogicTick(INonPeriodicLogicDriverCtl driver);
    }
}
