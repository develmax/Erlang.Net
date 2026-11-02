using System.Reflection;

namespace Erlang.Tests;

internal static class CodePurgeOutcome706Names
{
    internal const string Test = "runtime/code-purge706-real-purged-and-did-kill-outcomes";
    internal const string Failure = "Real code purge outcome, victim settlement or current-image retention differs";
    internal const string SubjectModule = "purge_outcome706_subject";
    internal const string Callback = "purge_outcome706_callback";
    internal const string Block = "block";
    internal const string RunFunction = "run";
    internal const string CurrentValue = "current_retained706";
    internal const string ReleasedValue = "released706";
    internal const string Stop = "stop706";
    internal const string OutcomeMethod = "PurgeModuleOutcome706";
    internal const string LegacyMethod = "PurgeModule";
    internal const string Unwound = "Unwound";
    internal const string Down = "DOWN";
    internal const string ProcessTag = "process";
    internal const string OriginalAbsent = "hard_absent";
    internal const string OriginalIdle = "hard_idle";
    internal const string OriginalHeld = "hard_held";
    internal const string OriginalValue = "value";
    internal const string OldSource = "-module(purge_outcome706_subject). -export([run/0]). run()->purge_outcome706_callback:block(),released706.";
    internal const string NewSource = "-module(purge_outcome706_subject). -export([run/0]). run()->current_retained706.";
    internal const int Victims = 2;
    internal const int ActorLanes = 2;
    internal const int OriginalHeldArity = 2;
    internal const int DownArity = 5;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
}
