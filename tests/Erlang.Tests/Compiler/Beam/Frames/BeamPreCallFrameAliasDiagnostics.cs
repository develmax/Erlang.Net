namespace Erlang.Tests;

internal static class BeamPreCallFrameAliasDiagnostics
{
    internal const string FrameAndGuard = "Expected the guard to read the existing X value and keep the other Y root.";
    internal const string AcrossCall = "A Y value used after the call must keep its owned stack slot.";
    internal const string Heap = "A heap reservation may discard X before the next alias read.";
    internal const string Clobber = "An overwritten X value cannot replace its old stack copy.";
    internal const string Reinitialized = "A cleared Y slot does not still contain the original X value.";
    internal const string InteriorEntry = "A branch can reach the old Y slot without executing its X spill.";
    internal const string Debugger = "Keep the original stack slot available to debugger metadata.";
    internal const string CollectorRoots = "The retained register roots and the shortened frame must describe the same pre-call values.";
    internal const string RootGap = "An alias cannot root unrelated registers below its source.";
    internal const string SecondCollector = "An alias with two collectors is outside the single-check proof.";
}
