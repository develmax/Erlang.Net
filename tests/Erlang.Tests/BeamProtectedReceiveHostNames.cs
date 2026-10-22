namespace Erlang.Tests;

internal static class BeamProtectedReceiveHostNames
{
    internal const string Test = "runtime/beam/protected-receive386/original-whole-values-and-five-depths";
    internal const string Module = "beam_receive_try386";
    internal const string ResourcePrefix = "Erlang.Tests.BeamProtectedReceive386.";
    internal const string Beam = "beam_receive_try386.beam";
    internal const string BeamExtension = ".beam";
    internal const string Values = "values.etf";
    internal const string Stacks = "stacks.etf";
    internal const string Run = "run";
    internal const string RunStacks = "run_stacks";
    internal const string Erlang = "erlang";
    internal const string Normal = "normal";
    internal const string Failure = "Original protected receive whole observation differs";
    internal const int Cases = 8;
    internal const int Profiles = 40;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        [Beam] = "DF57353EB63A5E81C55BD40BDC3EDED074F7015747B6E98F4D8ADA1EF0CB349C",
        [Values] = "BBBAA692E25F850B3AAC425F019F0630177718D2A34205495666388FE9E55F32",
        [Stacks] = "9D030A670B512A30AEE2964DABD79242B3CD840380FC0D94820516FF31DEDDAE"
    };
}
