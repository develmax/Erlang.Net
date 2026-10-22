namespace Erlang;

internal static class EtsFixationNames420
{
    internal const string Module = "ets", Fix = "safe_fixtable", Slot = "slot";
    internal const string Foreign = "ETS fixation provider belongs to another runtime", Invalid = "Invalid ETS fixation native invocation", Conflict = "ETS fixation binding conflicts with an existing source or native owner";
    internal static readonly (string Function, int Arity)[] Signatures = [(Fix, 2), (Slot, 2)];
}
