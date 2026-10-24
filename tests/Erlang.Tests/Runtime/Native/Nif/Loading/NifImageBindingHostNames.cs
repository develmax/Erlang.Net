using System.Reflection;

namespace Erlang.Tests;

internal static class NifImageBindingHostNames
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal const string Test = "runtime/nif-image-attachment-transactions-and-quiescence";
    internal const string Module = "nif_image_binding_host";
    internal const string Function = "fallback";
    internal const string Other = "other";
    internal const string Table = "NifBindings";
    internal const string Begin = "Begin";
    internal const string Publish = "Publish";
    internal const string Admit = "Admit";
    internal const string Retire = "RetireAsync";
    internal const string Invoke = "InvokeAsync";
    internal const string Broken = "NIF image attachment primitive contract failed";
    internal const string CleanupFailure = "Owned NIF test cleanup failed";
    internal const string BindingStore = "Erlang.NifImageBindingStore";
    internal const string IsCreated = "IsCreated";
    internal const int ConcurrentReaders = 64;
    internal const int Expected = 37;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
