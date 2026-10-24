namespace Erlang.Tests;

internal static class DriverExternal455Names
{
    internal const string Test = "runtime/library455/native-external-term-keeps-own-local-reference";
    internal const string Decode = "DecodeDriverExternal455";
    internal const string Encode = "term_to_binary";
    internal const string Local = "local";
    internal const string Failure = "Native external term lost its owning VM identity or accepted invalid bytes";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
