-module(beam_list_terminal_calls).
-export([run/1, other/0]).

run(X) ->
    [tag(X), ?MODULE:other()].

tag(X) -> {tag, X}.
other() -> {other}.
