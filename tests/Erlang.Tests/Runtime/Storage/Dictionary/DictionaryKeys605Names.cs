using System.Reflection;

namespace Erlang.Tests;

internal static class DictionaryKeys605Names
{
    internal const string NativeCase = "runtime/dictionary627/native-keys-guest-only";
    internal const string ExitCase = "runtime/dictionary627/native-keys-owner-exit";
    internal const string OpaqueCase = "runtime/dictionary627/public-same-body-replacement-opaque";
    internal const string AtomicsCase = "runtime/dictionary627/atomics-keys-guest-only";

    internal const string Module = "dictionary_keys605";
    internal const string KeysEntry = "keys";
    internal const string ResourceKeysEntry = "keys_resource";
    internal const string KeysOnlyEntry = "keys_only";
    internal const string EraseEntry = "erase_value";
    internal const string Stored = "stored";
    internal const string GetKeys = "get_keys";
    internal const string Erlang = "erlang";
    internal const string RegistryFunctions = "functions";
    internal const string Heap = "TermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Major = "CollectGeneration508";
    internal const string DictionaryData = "data";
    internal const string Make = "MakeNativeResource524";
    internal const string Pointer = "Pointer524";
    internal const string Retired = "Retired524";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string Capture = "TryCapture";
    internal const string AtomicsNativeType = "Erlang.AtomicsNative413";
    internal const string AtomicsModule = "atomics";
    internal const string AtomicsPut = "put";
    internal const string AtomicsGet = "get";
    internal const string Failure = "Canonical dictionary key projection605 did not preserve selected guest custody.";
    internal const string CleanupFailure = "Dictionary key projection605 or independent cleanup failed.";
    internal const int Sentinel = 60591;
    internal const int NativeWords = 3;
    internal const int KeyChecks = 2;
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
        -module(dictionary_keys605).
        -export([keys/0,keys_only/0,keys_resource/0,erase_value/0]).
        keys() ->
          erlang:garbage_collect(),
          [stored]=get_keys(),
          [stored]=get_keys(get(stored)),
          2.
        keys_only() -> case get_keys() of [stored] -> 1 end.
        keys_resource() -> [K]=get_keys(), true=is_reference(K), erts_debug:flat_size(K).
        erase_value() ->
          R=erase(stored),
          erlang:garbage_collect(),
          erts_debug:flat_size(R).
        """;
}
