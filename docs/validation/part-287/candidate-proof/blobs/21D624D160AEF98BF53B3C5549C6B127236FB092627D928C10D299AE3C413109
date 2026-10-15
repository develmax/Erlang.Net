using Erlang;

namespace Erlang.Tests;

internal static class DistributionRawInputHostNames
{
    internal const string Test = "runtime/distribution-raw-input-attempt-generation-owner-exiting-lifecycle";
    internal const int ExpectedAssertions = 41;
    internal const string Local = "raw_input_local@localhost";
    internal const string Remote = "raw_input_remote@localhost";
    internal const string ExitingControllers = "exitingInputControllers";
    internal const string StopLocalNode = "StopLocalNode";
    internal const string Payload = "raw_input_payload";
    internal const string Failure = "Raw distribution input assertion failed";
    internal const int LinkOpcode = 1;
    internal const int ExitOpcode = 3;
    internal const int UnsupportedSpawnRequestOpcode = DistributionControlOpcodes.SpawnRequest;
    internal const ulong UnsupportedSpawnRequestFlag = 0x1UL << 32;
    internal const string UnsupportedSpawnModule = "raw_input_spawn_module";
    internal const string UnsupportedSpawnFunction = "raw_input_spawn_function";
    internal const byte PassThrough = 112;
    internal const int PartialBits = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal static readonly Atom Ok = Term.A(ConnectedDistributionHostNames.Success);
}
