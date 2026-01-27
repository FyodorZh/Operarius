using System;

namespace Operarius
{
    public interface IPeriodicLogicDriverCtl : ILogicDriverCtl
    {
        TimeSpan Period { get; }
    }
}