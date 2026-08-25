using System;
using System.Threading.Tasks;

namespace Operarius
{
    public enum LogicStartResult 
    {
        Success,
        FailedToStart,
        CapacityExceeded,
        DriverIsNotActive
    }

    public interface ILogicDriver<TLogicDriverCtl> : ILogicRunner<TLogicDriverCtl>
        where TLogicDriverCtl : ILogicDriverCtl
    {
        event Action<ILogic<TLogicDriverCtl>> LogicStopped;
        event Action<Exception> ErrorStream;
        Task Finish();
        Task WaitForFinish();
    }
}