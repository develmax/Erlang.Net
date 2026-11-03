using System.Reflection;

namespace Erlang.Tests;

internal static class NativeStore600Names
{
    internal const string EtsCase = "runtime/native-store600/ets-source-header-drop-real-payload";
    internal const string PersistentCase = "runtime/native-store600/persistent-source-header-drop-real-payload";
    internal const string Failure = "The real native allocation lost its store/header custody.";
    internal const string CleanupFailure = "Native store assertion and independent cleanup failed.";
    internal const string Key = "native_store600";
    internal const string Heap = "TermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Major = "CollectGeneration508";
    internal const string Make = "MakeNativeResource524";
    internal const string PayloadPointer = "Pointer524";
    internal const string Roots = "Erlang.NativeResourceRoots524";
    internal const string Direct = "Direct524";
    internal const string Retired = "Retired524";
    internal const int Sentinel = 600;
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}
