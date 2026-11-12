-module(literal_case_probe).
-export([encoding/1, numeric/1, single/1, partial/1, identity/1, exhaustive/1, throwing/1]).

encoding(Options) ->
    case proplists:get_value(encoding, Options, epp:default_encoding()) of
        latin1 -> latin1;
        utf8 -> unicode;
        unicode -> unicode
    end.

numeric(Value) ->
    case read(Value) of
        1 -> found;
        2 -> found;
        3 -> 3
    end.

single(Value) ->
    Selected = read(Value),
    case Selected of
        yes -> yes
    end.

partial(Value) ->
    case read(Value) of
        true -> report(accepted);
        false -> rejected
    end.

identity(Value) ->
    case read(Value) of
        one -> one;
        two -> two;
        _ -> unknown
    end.

exhaustive(Value) ->
    case choose(Value) of
        ready -> ready;
        error -> failed
    end.

throwing(Value) ->
    case choose(Value) of
        ready -> ready;
        error -> erlang:error(failed)
    end.

choose([]) -> ready;
choose([_|Rest]) -> choose(Rest);
choose(_) -> error.

read(Value) ->
    erlang:put(last_read, Value),
    Value.

report(Value) ->
    erlang:put(last_report, Value),
    Value.

