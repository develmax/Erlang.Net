-module(beam_singleton_call_construct).
-export([init/1, direct/1]).

init([Input]) ->
    Kind = one_for_one,
    Limit = 10,
    Period = 3600,
    Result = wrap(Input),
    {ok, {{Kind, Limit, Period}, Result}}.

direct([Input]) ->
    {ok, wrap(Input)}.

wrap(Input) -> {value, Input}.
