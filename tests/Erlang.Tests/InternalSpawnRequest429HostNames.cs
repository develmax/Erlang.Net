using System.Reflection;
namespace Erlang.Tests;

internal static class InternalSpawnRequest429HostNames
{
    internal const string NativeType = "Erlang.InternalSpawnRequestNative429", Capture = "TryCapture", Module = "erts_internal", Function = "spawn_request";
    internal const string TargetModule = "internal_spawn_host429", Target = "held", BadArgument = "badarg", BadOptions = "badopt";
    internal const string Error = "error", Reply = "spawn_reply", Ok = "ok", Link = "link", Monitor = "monitor", Exit = "EXIT", Abandoned = "abandoned";
    internal const string OwnerDeath = "owner_death429", MinimumHeap = "min_heap_size";
    internal const string Failure = "Internal spawn request429 real lifecycle/status/owner control failed.";
    internal const string StatusTest = "HOST/internal-spawn429-status-and-owner";
    internal const string AbandonTest = "HOST/internal-spawn429-real-abandon-ack-and-child";
    internal const string OwnerTest = "HOST/internal-spawn429-real-linked-owner-death";
    internal const string Requests = "spawnRequests";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
}
