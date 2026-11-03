namespace Erlang.Tests;

internal static class DictionaryProjection635Names
{
    internal const string Test = "runtime/dictionary635/linear-key-projection-visits";
    internal const string Module = "dictionary_projection635";
    internal const string File = "dictionary_projection635.erl";
    internal const string All = "all_keys";
    internal const string Matching = "matching_keys";
    internal const string Missing = "missing_keys";
    internal const string SelectedValue = "selected635";
    internal const string Observation = "Dictionary635 N={0}, operation={1}, actual visits={2}, limit={3}";
    internal const int VisitsPerEntry = 40;
    internal const int FixedVisits = 256;
    internal static readonly int[] Sizes = [128, 256, 512];
    internal const string Source = """
        -module(dictionary_projection635).
        -export([all_keys/0,matching_keys/0,missing_keys/0]).
        all_keys() -> length(get_keys()).
        matching_keys() -> length(get_keys(selected635)).
        missing_keys() -> length(get_keys(not_selected635)).
        """;
}
