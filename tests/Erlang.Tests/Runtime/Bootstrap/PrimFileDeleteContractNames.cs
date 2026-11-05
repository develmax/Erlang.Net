namespace Erlang.Tests;

internal static class PrimFileDeleteContractNames
{
    internal const string HostTest = "runtime/prim-file/delete-platform-contracts";
    internal const string MappingTest = "runtime/prim-file/delete-errno-and-path-contracts";
    internal const string Prefix = "Erlang.";
    internal const string DeleteType = "PrimFileDeleteOperations";
    internal const string Delete = "Delete";
    internal const string UnixPathType = "PrimFileUnixPaths";
    internal const string Marshal = "Marshal";
    internal const string ErrorType = "PrimFileUnixErrno";
    internal const string Error = "DeleteError";
    internal const string Failure = "prim_file platform deletion contract failed: ";
    internal const string Capture = "tests/Erlang.Tests/Resources/PrimFileDelete";
    internal const string Output = "artifacts/prim-file-platform-delete";
    internal const int RaceRounds = 32;
    internal const int UnknownErrno = 10000;
}
