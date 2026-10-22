namespace Erlang;

internal static class RuntimeBootstrapSchedulerNames
{
    internal const string Module = "erts_internal";
    internal const string Function = "scheduler_wall_time";
    internal const string Foreign = "Bootstrap scheduler registration requires the runtime own registry";
    internal const string Conflict = "Bootstrap scheduler refuses an existing or source-owned native entry";
    internal const int Arity = 1;
}
