using System.Reflection;

namespace Erlang.Tests;

internal static class ExactImageCodeHostNames
{
    internal const string Test = "HOST exact-image process-code requests and purge drain";
    internal const string Broken = "Exact-image process-code invariant failed";
    internal const string Module = "exact_image_code_host";
    internal const string Make = "make";
    internal const string Kept = "kept";
    internal const string Continue = "continue";
    internal const string Held = "held";
    internal const string ImageCheck = "CheckProcessCodeImage";
    internal const string ModuleCheck = "CheckProcessCode";
    internal const string DrainOne = "PurgeCodeImageProcess";
    internal const string Drain = "PurgeCodeImageProcesses";
    internal const string Pending = "PendingCodeChecks";
    internal const string Gate = "gate";
    internal const string OldCode = "oldCode";
    internal const string Unwound = "Unwound";
    internal const string Source = "-module(exact_image_code_host). -export([make/0]). make()->fun hidden/0. hidden()->kept.";
    internal const string ReplacementSource = "-module(exact_image_code_host). -export([make/0]). make()->fun hidden/0. hidden()->different.";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
}
