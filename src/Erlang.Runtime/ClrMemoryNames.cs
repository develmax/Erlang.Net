namespace Erlang;

internal static class ClrMemoryNames
{
    internal const string Function = "memory";
    internal const int AllArity = 0;
    internal const int QueryArity = 1;
    internal const string Total = "total";
    internal const string Processes = "processes";
    internal const string ProcessesUsed = "processes_used";
    internal const string System = "system";
    internal const string Atom = "atom";
    internal const string AtomUsed = "atom_used";
    internal const string Binary = "binary";
    internal const string Code = "code";
    internal const string Ets = "ets";
    internal static readonly string[] Ordered = [Total, Processes, ProcessesUsed, System, Atom, AtomUsed, Binary, Code, Ets];
}
