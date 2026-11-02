namespace Erlang.Tests;

internal static class AckTrace173ReplayNames
{
    internal const string Command = "--ack-trace173";
    internal const string Inputs = "managed-trace-inputs.json";
    internal const string Module = "ack_receive_trace173";
    internal const string Worker = "ack_receive_trace173.beam";
    internal const string Observe = "observe";
    internal const string Values = "values.etf";
    internal const string Raw = "trace-pairs.etf";
    internal const string Whole = "whole.etf";
    internal const string Failure = "Complete ACK173 own-ID trace protocol failed; retain complete raw actual/expected pairs.";
    internal const int NativeExports = 0;
    internal const int WholeArity = 2;
    internal const int ValuesIndex = 0;
    internal const int RawIndex = 1;
    internal const int PairCount = 2;
    internal const int PairArity = 2;
    internal const int ActualIndex = 0;
    internal const int ExpectedIndex = 1;
    internal static readonly string[] Dependencies = ["erlang", "file"];
}
