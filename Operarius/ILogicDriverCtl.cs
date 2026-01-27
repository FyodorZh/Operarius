using System;

namespace Operarius
{
    /// <summary>
    /// Passed to ILogic implementations for controlling the execution driver.
    /// </summary>
    public interface ILogicDriverCtl
    {
        DateTime CurrentTime { get; }
        
        /// <summary>
        /// Stops the execution.
        /// </summary>
        void Stop();
    }
}
