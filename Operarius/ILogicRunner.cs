namespace Operarius
{
    public interface ILogicRunner<out TLogicDriverCtl>
        where TLogicDriverCtl : ILogicDriverCtl
    {
        LogicStartResult Start(ILogic<TLogicDriverCtl> logic);
    }
}