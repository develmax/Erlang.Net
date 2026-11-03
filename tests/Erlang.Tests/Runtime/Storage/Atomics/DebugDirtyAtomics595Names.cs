using System.Reflection;

namespace Erlang.Tests;

internal static class DebugDirtyAtomics595Names
{
    internal const string ValueCase = "runtime/atomics595/dirty-copy-host-wrapper-survives-source-drop";
    internal const string ForeignCase = "runtime/atomics595/dirty-copy-foreign-before-stale-without-entry-grant";
    internal const string CopyType = "Erlang.DebugDirtyTermCopy424";
    internal const string Copy = "Copy";
    internal const string NativeType = "Erlang.AtomicsNative413";
    internal const string Capture = "TryCapture";
    internal const string GuestModule = "atomics";
    internal const string Put = "put";
    internal const string Get = "get";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string Failure = "A dirty copy lost typed actual backing custody or accepted a foreign/stale header.";
    internal const string CleanupFailure = "Dirty atomics copy assertion or independent cleanup failed.";
    internal const int StoredValue = 123;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
