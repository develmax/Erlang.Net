-module(beam_tuple_case_probe).
-export([parsed/1, direct/1, fields/1]).

parsed(String) ->
    {ok, Tokens, _} = erl_scan:string(String ++ "."),
    case catch erl_parse:parse_term(Tokens) of
        {ok, Term} -> {ok, Term};
        Error -> {error, {Error, String}}
    end.

direct(Pairs) ->
    {ok, Value} = proplists:get_value(key, Pairs),
    case Value of
        yes -> matched;
        no -> missed;
        Other -> {other, Other}
    end.

fields(Pairs) ->
    {Data, Tag} = proplists:get_value(key, Pairs),
    case Tag of
        yes -> {matched, Data};
        Other -> {Other, Data}
    end.
