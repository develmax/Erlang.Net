using System.Reflection;

namespace Erlang.Tests;

internal static class PrimFileWindows410HostNames
{
    internal const string Test = "HOST/prim-file410/windows-native-files-and-validation";
    internal const string Root = "artifacts/library410-prim-file-host";
    internal const string Format = "N";
    internal const string Type = "Erlang.PrimFileWindowsOperations410";
    internal const string Cwd = "GetCwd";
    internal const string Read = "ReadFile";
    internal const string Info = "ReadInfo";
    internal const string List = "ListDir";
    internal const string Name = "native410.bin";
    internal const string MissingName = "missing410";
    internal const string Failure = "Windows prim_file410 native control failed";
    internal const string Error = "error";
    internal const string Ok = "ok";
    internal const string Invalid = "einval";
    internal const string Missing = "enoent";
    internal const string DirectoryError = "eisdir";
    internal const string Regular = "regular";
    internal const string Directory = "directory";
    internal const string ReadWrite = "read_write";
    internal const string ReadOnly = "read";
    internal const BindingFlags Methods = BindingFlags.Static | BindingFlags.NonPublic;
}
