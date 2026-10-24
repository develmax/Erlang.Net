namespace Erlang.Tests;

internal static class BeamRaiseTraceHostNames
{
    internal const string Test = "HOST beam/original-raise-trace/whole-values-and-five-depths";
    internal const string Module = "beam_raise_trace384";
    internal const string ResourcePrefix = "Erlang.Tests.BeamRaiseTrace384.";
    internal const string Beam = "beam_raise_trace384.beam";
    internal const string Values = "values.etf";
    internal const string Stacks = "stacks.etf";
    internal const string Run = "run";
    internal const string RunStacks = "run_stacks";
    internal const string Erlang = "erlang";
    internal const string Normal = "normal";
    internal const string Failure = "Original raise/trace whole observation differs";
    internal const int Cases = 29;
    internal const int Profiles = 145;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        [Beam] = "1A8D9E4FA7577E8B76E159DBC65A44B3E3D26251F8DEFF9C43BB3EFF5CC91627",
        [Values] = "497C6D9E51D685EA608855BE7E89D9A9A99907C2A0F2B409DA1417D5D1F57DA8",
        [Stacks] = "72AC40DC14442FECB1D2E8452EC07ED965FEC47CF97120CAA8A9157BC3341A25"
    };
}
