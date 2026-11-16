-module(beam_singleton_checked_calls).
-export([run/1]).
run(Value) when is_atom(Value) ->
    run([Value]);
run([Value]) ->
    Selected = beam_probe:read(Value),
    case beam_probe:check(Selected) of
        true ->
            case beam_probe:open() of
                ok ->
                    beam_probe:finish(Selected),
                    done;
                Failure -> Failure
            end;
        _ -> {error, blocked}
    end.
