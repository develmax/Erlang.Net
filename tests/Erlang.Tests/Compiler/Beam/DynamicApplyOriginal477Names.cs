namespace Erlang.Tests;

internal static class DynamicApplyOriginal477Names
{
    internal const string Test = "oracle/compiled-module/dynamic-apply477-whole-values-and-stacks";
    internal const string HostTest = "compiler/dynamic-apply477-selected-host-bindings";
    internal const string Prefix = "Erlang.Tests.ApplyOriginal477.";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Observer = "otp_apply477_observer";
    internal const string ObserverImage = "otp_apply477_observer.beam";
    internal const string Worker = "worker";
    internal const string Golden = "original-values.etf";
    internal const string Managed = "managed-values.etf";
    internal const string OdbcImage = "odbc.beam";
    internal const string ListsImage = "lists.beam";
    internal const string Output = "artifacts/library477/permanent";
    internal const string Failure = "Original dynamic apply values,full stacks or selected binding differ";
    internal const string Tail = "tail";
    internal const string Fixed = "fixed";
    internal const string Erts = "erlang";
    internal const string Apply = "apply";
    internal const int Marker = 477;
    internal const int Outcomes = 115;
    internal static readonly string[] Imports = ["erlang", "lists", "odbc", "gen_server", "gen_tcp", "application", "supervisor", "code", "filename", "os", "io_lib", "error_logger", "timer", "string", "file", "filelib", "inet", "maps", "persistent_term", Observer];
}
