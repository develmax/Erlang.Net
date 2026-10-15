using Erlang;

namespace Erlang.Tests;

internal static class DistributionPayloadExitHostNames
{
    internal const string Test = "runtime/distribution-payload-exit-deferred-raw-lifecycle";
    internal const string Local = "payload_exit_local@localhost";
    internal const string Remote = "payload_exit_remote@localhost";
    internal const string Failure = "Deferred payload exit assertion failed";
    internal const string Reason = "payload_exit_reason";
    internal const string FunctionModule = "payload_exit_function";
    internal const string Gate = "gate";
    internal const int FunctionUniqueBytes = 16;
    internal const int FunctionIndex = 7;
    internal const int Compression = 1;
    internal const byte EtfVersion = 131;
    internal const int MissingId = 500000;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal static readonly Atom Value = Term.A(Reason);
}
