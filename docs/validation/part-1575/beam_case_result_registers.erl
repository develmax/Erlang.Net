-module(beam_case_result_registers).
-export([run/1, selector/1, produce/1, consume/2]).
run(Input) ->
    case ?MODULE:selector(Input) of
        true ->
            P = erlang:processes(),
            N = ?MODULE:produce(P),
            finish(N, P);
        false -> finish(false, [])
    end.
selector(Value) -> Value.
produce(Pids) -> is_list(Pids).
consume(Count, Values) -> finish(Count, Values).
finish(Count, Values) -> {Count, is_list(Values)}.
