-module(beam_first_tuple_match).
-export([known/2, unknown/2, literal/2, opaque/1, consume/1]).

known(Input, Keep) ->
    {Value, _Unused} = pair(Input),
    Result = ?MODULE:consume(Value),
    {Result, Keep, done}.

unknown(Input, Expected) ->
    {Expected, Value} = ?MODULE:opaque(Input),
    Result = ?MODULE:consume(Value),
    {ok, Result, Expected}.

literal(Input, Keep) ->
    {ok, Value, _, Other} = ?MODULE:opaque(Input),
    Result = ?MODULE:consume(Value),
    {Result, Other, Keep}.

pair(Value) ->
    {Value, ignored}.

opaque(Value) ->
    Value.

consume(Value) ->
    {seen, Value}.
