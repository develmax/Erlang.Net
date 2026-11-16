-module(beam_entry_patterns).
-export([tuple_case/2,list_case/2,tuple_clause/2,list_clause/2,retained/2]).
tuple_case(V, Keep) ->
    case V of
        {A,B} when is_integer(A),is_list(B) -> put(tuple_case,A),{ok,B,Keep};
        _ -> {default,Keep}
    end.
list_case(V, Keep) ->
    case V of
        [H|T] when is_integer(H) -> put(list_case,H),{ok,T,Keep};
        _ -> {default,Keep}
    end.
tuple_clause({A,B}, Keep) when is_integer(A),is_list(B) ->
    put(tuple_clause,A),{ok,B,Keep};
tuple_clause(_, Keep) -> {default,Keep}.
list_clause([H|T], Keep) when is_integer(H) ->
    put(list_clause,H),{ok,T,Keep};
list_clause(_, Keep) -> {default,Keep}.
retained(Value, Keep) ->
    case Value of
        {A,B} when is_integer(A),is_list(B) -> put(retained,A),{ok,B,Keep};
        _ -> {default,Value,Keep}
    end.
