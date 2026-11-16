-module(beam_length_selector_apply).
-export([run/2, select/2, missing/2, add_fun/0, abs_fun/0]).

run(Key, Args) ->
    case ?MODULE:select(Key, length(Args)) of
        undefined -> ?MODULE:missing(Key, Args);
        Fun when is_function(Fun, length(Args)) ->
            apply(Fun, Args)
    end.

select(add, 2) -> ?MODULE:add_fun();
select(wrong_arity, _) -> ?MODULE:abs_fun();
select(other, _) -> other;
select(_, _) -> undefined.

missing(Key, Args) -> {missing, Key, Args}.

add_fun() -> fun erlang:'+'/2.
abs_fun() -> fun erlang:abs/1.
