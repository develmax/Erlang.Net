using System.Reflection;

namespace Erlang.Tests;

internal static class CodeModificationHostNames
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal const string Test = "runtime/code-modification-permission-writers-cancellation-and-isolation";
    internal const string Permission = "CodeModification";
    internal const string Acquire = "AcquireAsync";
    internal const string Enter = "Enter";
    internal const string Run = "Run";
    internal const string Pending = "PendingSynchronousWriters";
    internal const string PendingAsync = "PendingAsynchronousWriters";
    internal const string DeleteModule = "delete_module";
    internal const string AsTask = "AsTask";
    internal const string Result = "Result";
    internal const string Module = "code_modification_host";
    internal const string Other = "code_modification_other";
    internal const string Function = "fallback";
    internal const string RegistryGate = "gate";
    internal const string Available = "IsAvailable";
    internal const string Broken = "Code modification admission contract failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
