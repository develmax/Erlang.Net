-module(unicode_suffix_probe).
-export([run/1, run_length/1]).
-compile({inline, [target/0]}).

target() -> wxe_util.

run(Strings) ->
    Module = target(),
    Converted = [unicode:characters_to_binary([String|[0]]) || String <- Strings],
    Module:queue_cmd(Converted).

run_length(List) when is_list(List) ->
    Module = lists,
    Count = length(List),
    Module:nth(Count, List),
    ok.
