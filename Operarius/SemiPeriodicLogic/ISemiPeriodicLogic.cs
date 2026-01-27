using System;

namespace Operarius
{
    /// <summary>
    /// Represents a contract for logic that is executed semi-periodically, providing methods for initialization, periodic tasks,
    /// and cleanup during its lifecycle.
    /// The actual invocation period is controlled by <see cref="ISemiPeriodicLogicDriverCtl"/>
    /// </summary>
    public interface ISemiPeriodicLogic : ILogic<ISemiPeriodicLogicDriverCtl>
    {
        /// <summary>
        /// Invoked periodically while the logic is in the running state.
        /// </summary>
        /// <remarks>
        /// This method is called after <see cref="LogicStarted"/> has been successfully invoked
        /// and continues to be executed at some intervals until <see cref="LogicStopped"/> is called.
        /// </remarks>
        TimeSpan LogicTick(ISemiPeriodicLogicDriverCtl driver);
    }
}