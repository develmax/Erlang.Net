-module(case_payload_scratch_probe).
-export([run/2, step/1, audit/1]).
run(Selector, Keep) ->
    Result =
        case ?MODULE:step(Selector) of
            {ok, {prompt, "login: "}, _} -> matched;
            {ok, [{prompt, _First}, {prompt, _Second}], _} -> accepted;
            {ok, [{capture, First}], _} -> ?MODULE:audit(First), First;
            Error -> ?MODULE:audit(Error), {error, Error}
        end,
    ?MODULE:audit(Keep),
    Result.
step(throw) -> erlang:error(selector_failed);
step(Value) -> Value.
audit(Value) -> self() ! {recorded, Value}.
