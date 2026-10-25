namespace Erlang.Tests;

internal static class BeamClosedWait480Names
{
    internal const string Prefix = "compiler/closed-wait480/";
    internal const string Valid = Prefix + "tuple-dispatch-caught-exit";
    internal const string Returning = Prefix + "returning-dispatch-arm-refused";
    internal const string Foreign = Prefix + "foreign-dispatch-label-refused";
    internal const string Roots = Prefix + "uninitialized-frame-root-refused";
    internal const string Unknown = Prefix + "unknown-tail-refused";
    internal const int Accepted = 11;
    internal const int LabelCount = 12;
}
