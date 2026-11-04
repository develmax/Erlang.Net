namespace Erlang;

internal static class DetsLibraryModuleNames
{
    internal const string Module = "dets";
    internal const string Server = "dets_server";
    internal const string Supervisor = "dets_sup";
    internal const string ProcessLibrary = "proc_lib";
    internal const string Ets = "ets";
    internal const string System = "sys";
    internal const string Query = "qlc";
    internal const string File = "file";
    internal const string Io = "io";
    internal const string IoLibrary = "io_lib";
    internal const string Logger = "logger";
    internal const string Lists = "lists";
    internal const string Erlang = "erlang";

    internal static readonly string[] Required =
    [
        Server,
        Supervisor,
        ProcessLibrary,
        Ets,
        System,
        Query,
        File,
        Io,
        IoLibrary,
        Logger,
        Lists,
        Erlang
    ];
}
