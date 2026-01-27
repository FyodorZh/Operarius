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

    public interface ILogicDriver<TLogicDriverCtl>
        where TLogicDriverCtl : ILogicDriverCtl
    {
        event Action<ILogic<TLogicDriverCtl>> LogicStopped;
        event Action<Exception> ErrorStream;
        LogicStartResult Start(ILogic<TLogicDriverCtl> logic);
        Task Finish();
        Task WaitForFinish();
    }
}