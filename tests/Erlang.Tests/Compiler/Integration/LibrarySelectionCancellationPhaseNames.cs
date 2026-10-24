using System.Reflection;

namespace Erlang.Tests;

internal static class LibrarySelectionCancellationPhaseNames
{
    internal const string Test = "runtime/library-selection/cancellation-during-held-purge-phase";
    internal const string Failure = "Startup purge-phase cancellation or owned cleanup failed";
    internal const string RuntimeGate = "gate";
    internal const string Processes = "processes";
    internal const string Purger = "codePurger";
    internal const string Prepare = "PrepareStartupLibraries";
    internal const string Publish = "PublishStartupLibraries";
    internal const string Finish = "FinishStartupLibraries";
    internal const string Signals = "ProcessSignals";
    internal const string Done = "Done";
    internal const string Unwound = "Unwound";
    internal const string CodeGate = "codeGate";
    internal const string ActiveCode = "activeCode";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}
