using System.Reflection;

namespace Erlang.Tests;

internal static class DictionaryKeys538Names
{
    internal const string NativeCase = "runtime/dictionary538/native-resource-key-selected-list-last-root";
    internal const string AtomicsCase = "runtime/dictionary538/atomics-resource-key-selected-list-last-root";
    internal const string CountersCase = "runtime/dictionary538/counters-resource-key-selected-list-last-root";
    internal const string RegexCase = "runtime/dictionary538/regex-resource-key-selected-list-last-root";
    internal const string BinaryCase = "runtime/dictionary538/binary-resource-key-selected-list-last-root";
    internal const string Module = "dictionary_keys538";
    internal const string File = "dictionary_keys538.erl";
    internal const string Select = "select_and_erase_key";
    internal const string Probe = "probe_selected";
    internal const string Drop = "drop_selected";
    internal const string ErlangModule = "erlang";
    internal const string GetKeys = "get_keys";
    internal const string Functions = "functions";
    internal const string Selected = "SelectedOwnedBinding507";
    internal const string LocalDictionary = "LocalDictionary";
    internal const string Failure = "Canonical resource-key selection538 lost actual guest list custody or final retirement.";
    internal const int Value = 53891;
    internal const int Words = 3;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Source = """
        -module(dictionary_keys538).
        -export([select_and_erase_key/0,probe_selected/0,drop_selected/0]).
        select_and_erase_key() ->
            Keys0=get_keys(),
            [K]=Keys0,
            Keys1=get_keys(53891),
            [K]=Keys1,
            []=get_keys(53892),
            true=is_reference(K),
            undefined=put(held_selected538,Keys1),
            53891=erase(K),
            erlang:garbage_collect(),
            probe_selected().
        probe_selected() ->
            [K]=get(held_selected538),
            true=is_reference(K),
            erlang:garbage_collect(),
            erts_debug:flat_size(K).
        drop_selected() ->
            [_]=erase(held_selected538),
            erlang:garbage_collect(),
            0.
        """;
}
