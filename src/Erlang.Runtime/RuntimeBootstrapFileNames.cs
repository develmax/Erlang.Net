namespace Erlang;

internal static class RuntimeBootstrapFileNames
{
    internal const string Module = "prim_file";
    internal const string InternalModule = "erts_internal";
    internal const string SpawnSystemProcess = "spawn_system_process";
    internal const string IsSystemProcess = "is_system_process";
    internal const string ForeignRegistry = "Bootstrap filesystem registration requires the runtime's own registry";
    internal const string Conflict = "Bootstrap filesystem registration refuses an existing module entry";
    internal const string WindowsRequired = "The bootstrap filesystem currently requires Windows";
}
