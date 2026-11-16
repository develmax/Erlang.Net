-module(mapping_queue_probe).
-export([run/2, dispatch/1, dispatch_forward/1, fail/0]).
-compile({inline, [target/0]}).

target() -> wxe_util.

run(Number, Strings) when is_integer(Number), is_list(Strings) ->
    Module = target(),
    Converted = [unicode:characters_to_binary([String|[0]]) || String <- Strings],
    Count = length(Strings),
    Module:queue_cmd(Number, Count, Converted, 7),
    ok.

dispatch(first) -> finish(1);
dispatch(second) -> finish(2).

dispatch_forward(first) -> forward(first, 3);
dispatch_forward(second) -> forward(second, 3).

forward(Atom, Number) -> {Atom, Number}.

finish(Number) -> {ok, Number}.
fail() -> reason(17).
reason(Line) -> erlang:nif_error({nif_not_loaded, line, Line}).
