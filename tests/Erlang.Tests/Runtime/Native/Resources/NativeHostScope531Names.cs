using System.Reflection;

namespace Erlang.Tests;

internal static class NativeHostScope531Names
{
    internal const string Test = "native-resource531/parallel-host-export-lease-custody";
    internal const string Failure = "Parallel real native exports lost host custody or physical retirement.";
    internal const string EnvironmentType = "Erlang.NifNativeCallEnvironment";
    internal const string Export = "ExportResult516";
    internal const string Add = "Add";
    internal const string Roots = "roots";
    internal const string Gate = "gate";
    internal const string ActiveCount = "ActiveCount";
    internal const string Image = "Image";
    internal const string Pointer = "Pointer524";
    internal const string Layout = "Erlang.NativeResourceMagic527";
    internal const string UserOffset = "UserOffset527";
    internal const int Workers = 32;
    internal const int ExportsPerWorker = 512;
    internal const int Batch = 8;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}
