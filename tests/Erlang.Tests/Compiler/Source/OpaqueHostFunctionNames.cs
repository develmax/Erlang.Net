namespace Erlang.Tests;

internal static class OpaqueHostFunctionNames
{
    internal const string MetadataCase = "compiler/opaque-host-local-identity-argument-preserved";
    internal const string ProviderCase = "compiler/opaque-host-identity-provider-call-count-preserved";
    internal const string Guest = "opaque_host_function_guest";
    internal const string HostModule = "opaque_host_function_metadata";
    internal const string Entry = "run";
    internal const string State = "host_function_state";
    internal const string Failure = "Ordinary canonical entry changed a public opaque Host function argument";
    internal const int Operand = 41;
    internal const int Result = 42;
    internal const int Index = 7;
    internal const int OldUnique = 19;
    internal const int UniqueBytes = 16;
    internal const string Source = "-module(opaque_host_function_guest). -export([run/1]). run(F) -> F(41).";
}
