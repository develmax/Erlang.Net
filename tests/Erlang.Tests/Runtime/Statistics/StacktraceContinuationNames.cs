namespace Erlang.Tests;

internal static class StacktraceContinuationNames
{
    internal const string Case = "runtime/beam/current-stacktrace-actual-adjacent-continuations";
    internal const string Module = "stacktrace_cp_observer";
    internal const string Entry = "run";
    internal const string Same = "same";
    internal const string Distinct = "distinct";
    internal const string Stacktrace = "current_stacktrace";
    internal const string Profile = "OwnedTermsPilot506";
    internal const string Failure = "Actual BEAM continuation sampling differs from the complete pinned OTP result";
    internal const string BeamResource = "Erlang.Tests.Statistics.ContinuationStacks.stacktrace_cp_observer.beam";
    internal const string OutcomeResource = "Erlang.Tests.Statistics.ContinuationStacks.original.etf";
    internal const string BeamSha256 = "AA043C797B2A80F3C59DC3D5EBCE18A3CA429FB69A4251BB2C1DFF83E03A9132";
    internal const string OutcomeSha256 = "870A6D9214AAEF32AE35D8617968E3581B1E8392AE493DFEA86839BDC24E63D9";
    internal const int SchedulerCount = 2;
    internal const int ProcessCapacity = 1 << 20;
    internal const int SameFrameCount = 2;
    internal const int DistinctFrameCount = 5;
}
