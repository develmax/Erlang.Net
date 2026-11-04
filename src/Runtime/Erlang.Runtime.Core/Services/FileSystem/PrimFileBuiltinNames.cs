namespace Erlang;

internal static class PrimFileBuiltinNames
{
    internal const int Win32ErrorCodeMask = 0xffff;
    internal const string ReadMode = "read";
    internal const string WriteMode = "write";
    internal const string AppendMode = "append";
    internal const string ExclusiveMode = "exclusive";
    internal const string Beginning = "bof";
    internal const string Current = "cur";
    internal const string EndOfFile = "eof";
    internal const string GetCwd = "get_cwd_nif";
    internal const string ReadFile = "read_file_nif";
    internal const string ReadInfo = "read_info_nif";
    internal const string ListDir = "list_dir_nif";
    internal const string Open = "open_nif";
    internal const string Close = "close_nif";
    internal const string Read = "read_nif";
    internal const string Write = "write_nif";
    internal const string Pread = "pread_nif";
    internal const string Pwrite = "pwrite_nif";
    internal const string Seek = "seek_nif";
    internal const string Sync = "sync_nif";
    internal const string Truncate = "truncate_nif";
    internal const string Indirect = "ipread_s32bu_p32bu_nif";
    internal const string Delete = "del_file_nif";
    internal const string Rename = "rename_nif";
    internal const string DelayedClose = "delayed_close_nif";
    internal const string HandleInfo = "read_handle_info_nif";
    internal const string Handle = "get_handle_nif";
    internal static readonly (string Name, int Arity)[] StorageSignatures =
    [
        (Open, 2), (Close, 1), (Read, 2), (Write, 2), (Pread, 3), (Pwrite, 3),
        (Seek, 3), (Sync, 2), (Truncate, 1), (Indirect, 3), (Delete, 1), (Rename, 2),
        (DelayedClose, 1), (HandleInfo, 1), (Handle, 1)
    ];
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
