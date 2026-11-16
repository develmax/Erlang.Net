-module(beam_tuple_call_chain).
-export([known/1, unknown/2, direct/1, direct_unknown/2, prebound/2, identity/1, next/2]).

known(Input) ->
    {First, Value} = first(Input),
    {Second, Third, Rest} = last(Value),
    case Rest of
        [] -> {First, Second, Third, Second};
        Other -> {illegal, Other, Third}
    end.

unknown(Input, Result) ->
    {Left, Value} = ?MODULE:identity(Input),
    {Right, Rest} = ?MODULE:next(Value, Result),
    case Rest of
        accepted -> {Left, Right, Rest};
        Other -> {Right, Other}
    end.

direct(Input) ->
    {First, Value} = first(Input),
    {Second, Third, Rest} = last(Value),
    {First, Rest, Second, Third, Second}.

direct_unknown(Input, Result) ->
    {Left, Value} = ?MODULE:identity(Input),
    {Right, Rest} = ?MODULE:next(Value, Result),
    {Left, Right, Rest}.

prebound(Input, Expected) ->
    {First, Value} = first(Input),
    {Second, Third, Rest} = last(Value),
    case Rest of
        [] -> {First, Second};
        Expected -> {Third, Expected}
    end.

first(Input) ->
    {first, Input}.

last({Value, Rest}) ->
    {Value, {carried, Value}, Rest};
last(Value) ->
    {Value, {carried, Value}, []}.

identity(Input) ->
    Input.

next(_, fail) ->
    malformed;
next(Value, Result) ->
    {Value, Result}.
