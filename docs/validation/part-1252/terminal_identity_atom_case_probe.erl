-module(terminal_identity_atom_case_probe).
-export([run/1, run_bound/2]).

run(Input) ->
    case pick(route, Input) of
        remote ->
            case pick(value, Input) of
                {error, Reason} -> {error, Reason};
                Other -> Other
            end;
        local -> local
    end.

run_bound(Input, Bound) ->
    case pick(route, Input) of
        remote ->
            case pick(value, Input) of
                {error, Reason} -> {error, Reason};
                Bound -> Bound
            end;
        local -> local
    end.

pick(Key, Input) ->
    maps:get(Key, Input).
