namespace Erlang;

internal static class PrimFilePosixNames
{
    internal const string Library = "Erlang.FileSystem.Native";
    internal const string Abi = "enfs_abi";
    internal const string Open = "enfs_open";
    internal const string Read = "enfs_read";
    internal const string Write = "enfs_write";
    internal const string Seek = "enfs_seek";
    internal const string Truncate = "enfs_truncate";
    internal const string Sync = "enfs_sync";
    internal const string Path = "enfs_path";
    internal const string Stat = "enfs_stat";
    internal const string Cwd = "enfs_cwd";
    internal const string OpenDir = "enfs_opendir";
    internal const string ReadDir = "enfs_readdir";
    internal const string CloseDir = "enfs_closedir";
    internal const string AbiMismatch = "The POSIX filesystem helper ABI differs from the runtime contract";
    internal const string SyncMode = "sync";
    internal const string FileInfoRecord = "file_info";
    internal const string Directory = "directory";
    internal const string Regular = "regular";
    internal const string Symlink = "symlink";
    internal const string Device = "device";
    internal const string Other = "other";
    internal const string ReadAccess = "read";
    internal const string WriteAccess = "write";
    internal const string ReadWriteAccess = "read_write";
    internal const string NoAccess = "none";
    internal const int ReadFlag = 1;
    internal const int WriteFlag = 2;
    internal const int AppendFlag = 4;
    internal const int ExclusiveFlag = 8;
    internal const int SyncFlag = 16;
    internal const int RenameOperation = 1;
    internal const int MkdirOperation = 2;
    internal const int RmdirOperation = 3;
    internal const int DirectoryType = 1;
    internal const int RegularType = 2;
    internal const int SymlinkType = 3;
    internal const int DeviceType = 4;
    internal const int InitialBufferBytes = 4096;
    internal const int ReadBufferBytes = 16384;
    internal const int RangeError = 34;
}
