namespace Erlang.Tests;

internal static class BeamDeepFrameNames
{
    internal const string Observer = "beam_deep_observer388";
    internal const string Subject = "beam_deep_subject388";
    internal const string Erlang = "erlang";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string Default = "default";
    internal const string Normal = "normal";
    internal const string Shutdown = "shutdown";
    internal const string Probe = "probe";
    internal const string ProbeFunction = "probe_descend";
    internal const string HeldProbe = "held_probe";
    internal const string HeldFunction = "held_probe_descend";
    internal const string HeldMessage = "deep_held";
    internal const string Measured = "measured_only";
    internal const string ImageIdentity = "ExecutionIdentity";
    internal const string Image = "Image";
    internal const string RuntimeGate = "gate";
    internal const string Processes = "processes";
    internal const string CodeGate = "codeGate";
    internal const string ActiveCode = "activeCode";
    internal const string Waiting = "WaitingForMessage";
    internal const string Unwound = "Unwound";
    internal const string Done = "Done";
    internal const string Task = "Task";
    internal const string VisibleFrame = "visibleExecutionFrame";
    internal const string Parent = "Parent";
    internal const string Module = "Module";
    internal const string Function = "Function";
    internal const int MaximumSampleClrFrames = 512;
    internal const int MaximumClrFrameGrowth = 64;
    internal const int HeldDepth = 16384;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan CaptureDeadline = TimeSpan.FromSeconds(240);
    internal const string SourceInspectionBoundExceeded = "Source frame chain exceeded declared inspection bound";
    internal const string RecursionNotSettled = "Measured recursion did not return declared flat count or settle source lease";
    internal const string MissingDeepReady = "Deep base-only ready message missing";
    internal const string NotActuallyWaiting = "Ready message did not establish actual deep receive suspension";
    internal const string MissingDeepSourceLease = "Deep suspended source stack/image lease not established";
    internal const string ClrStackBoundExceeded = "Physical CLR stack sample grew beyond declared shallow/deep bound";
    internal const string CancellationNotSettled = "Deep cancellation entered guest handler, returned or leaked source lease";
    internal const string InvalidProbeProcess = "Probe did not execute on actual source process";
    internal const string MissingProbeSourceDepth = "Probe did not observe declared non-tail source depth/image lease";
}
