namespace Erlang.Tests;

internal static class BeamApplyHostNames
{
    internal const byte Opcode = 112;
    internal const string Calls = "runtime/beam/apply381/native-and-awaited-host-calls";
    internal const string Errors = "runtime/beam/apply381/badarg-frame-and-typed-register-rejections";
    internal const string Module = "apply_host381";
    internal const string Echo = "echo";
    internal const string Failure = "Dynamic MFA apply check failed";
}
