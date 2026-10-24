namespace Erlang.Tests;

internal static class BeamTryLifecycleHostNames
{
    internal const string NormalTest = "runtime/beam-try/host-callback-normal";
    internal const string ErlangTest = "runtime/beam-try/host-erlang-exception-caught";
    internal const string ClrTest = "runtime/beam-try/host-clr-failure-bypasses-handler";
    internal const string CancellationTest = "runtime/beam-try/kill-physically-unwinds-callback-image";
    internal const string CallbackError = "beam_try_callback378";
    internal const string ClrFailure = "beam_try_clr_transport378";
    internal const string Failure = "Synthetic BEAM try lifecycle contract failed";
    internal const string Kill = "kill";
    internal const int FunctionArity = 0;
    internal const int RunArity = 1;
    internal const int FrameSlots = 1;
    internal const int InfoLabel = 1;
    internal const int EntryLabel = 2;
    internal const int HandlerLabel = 3;
    internal const int LabelCount = 4;
    internal const int RunAtomIndex = 2;
    internal const int HandlerMarkerAtomIndex = 7;
    internal const byte LabelOpcode = 1;
    internal const byte AllocateOpcode = 12;
    internal const byte DeallocateOpcode = 18;
    internal const byte ReturnOpcode = 19;
    internal const byte MoveOpcode = 64;
    internal const byte CallFunOpcode = 75;
    internal const byte TryOpcode = 104;
    internal const byte TryEndOpcode = 105;
    internal const byte TryCaseOpcode = 106;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
