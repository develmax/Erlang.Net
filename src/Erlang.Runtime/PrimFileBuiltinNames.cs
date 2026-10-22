namespace Erlang;

internal static class PrimFileBuiltinNames
{
    internal const string GetCwd = "get_cwd_nif";
    internal const string ReadFile = "read_file_nif";
    internal const string ReadInfo = "read_info_nif";
    internal const string ListDir = "list_dir_nif";
    internal const string MissingDeclaration = "The selected prim_file image omits a required native declaration";
    internal const string InvalidOwner = "The builtin prim_file initializer requires its local janitor PID";
    internal static readonly (string Name, int Arity)[] Signatures =
    [
        (GetCwd, 0),
        (ReadFile, 1),
        (ReadInfo, 2),
        (ListDir, 1)
    ];
}
