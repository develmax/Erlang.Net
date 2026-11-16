-module(tuple_arithmetic_probe).
-export([add/2, keep_both/2, guarded/2, unknown_shape/1]).

add(Input, Base) ->
    {Value, N} = pair(Input),
    {Value, Base + N}.

keep_both(Input, Base) ->
    {Value, N} = pair(Input),
    {Base * N, Value, N}.

guarded(Input, Base) when is_integer(Base) ->
    {Value, N} = pair(Input),
    {Value, Base + N}.

unknown_shape(Input) ->
    {Value, N} = unknown(Input),
    {Value, N + 1}.

pair(Input) ->
    pair(Input, 1).

pair({next, Input}, N) ->
    pair(Input, N + 1);
pair(Input, N) ->
    {Input, N}.

unknown(bad) ->
    bad;
unknown(Input) ->
    {Input, 0}.
