namespace Erlang;

internal static class CodeLibraryDependencies
{
    internal const string Code = "code";
    internal const string Server = "code_server";
    internal const string Init = "init";
    internal const string Erlang = "erlang";
    internal const string Loader = "erl_prim_loader";
    internal const string Filename = "filename";
    internal const string Lists = "lists";
    internal const string BeamLibrary = "beam_lib";
    internal const string File = "file";
    internal const string Io = "io";
    internal const string IoLibrary = "io_lib";
    internal const string Unicode = "unicode";
    internal const string Zlib = "zlib";
    internal const string ErrorLogger = "error_logger";
    internal const string Features = "erl_features";
    internal const string Binary = "binary";
    internal const string Ets = "ets";
    internal const string OrderedSets = "gb_sets";
    internal const string OrderedTrees = "gb_trees";
    internal const string Os = "os";
    internal const string Internal = "erts_internal";
    internal const string Logger = "logger";

    internal static readonly string[] Required =
    [
        Server,
        Init,
        Erlang,
        Loader,
        Filename,
        Lists,
        BeamLibrary,
        File,
        Io,
        IoLibrary,
        Unicode,
        Zlib,
        ErrorLogger,
        Features,
        Binary,
        Ets,
        OrderedSets,
        OrderedTrees,
        Os,
        Internal,
        Logger
    ];
}
