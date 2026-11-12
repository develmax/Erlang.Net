-module(beam_general_catch_probe).
-export([run/1, select/4, status/3, lookup/3, pick/2, status_nested/3, tagged_result/2, mixed_result/2, nested_result/2, tagged_tail/2, prepend/1, tuple_tail/3, list_tail/3, pattern_tail/2, grouped_tail/2, single_tail/2, tagged_single_tail/2, tuple_fallback/2, cons_fallback/2, cons_tuple_fallback/2, forwarded_tuple/2, embedded_tuple/3, reused_case/4, nil_case/1, literal_call_case/2, tuple_or_atom/1, tagged_call_tuple/1]).
run({M,F,X}) -> catch M:F(X).
select(M,F,X,Saved) ->
    case catch M:F(X) of
        4 -> Saved;
        _ -> failed
    end.
status(Key, Entries, Saved) ->
    case lists:member(Key, Entries) of
        true -> Saved;
        Other -> Other
    end.
lookup(Key, Entries, Saved) ->
    case lists:keyfind(Key, 1, Entries) of
        Value when is_tuple(Value) -> pair(Saved, Value);
        Other -> Other
    end.
pick(Entries, Saved) ->
    case pick_value(Entries) of
        c -> pair(Saved,c);
        a -> Saved;
        b -> skipped;
        Other -> Other
    end.
pick_value([{key,Value}|_]) -> Value;
pick_value([]) -> missing.
status_nested(Saved, Entries, Key) ->
    case lists:member(Key, relay(Entries)) of
        true -> Saved;
        Other -> Other
    end.
tagged_result(Input, Saved) ->
    case relay(Input) of
        {item,Value} -> Value;
        _ -> Saved
    end.
mixed_result(Input, Saved) ->
    case relay(Input) of
        {item,Value} -> Value;
        Bytes when is_binary(Bytes) -> Bytes;
        _ -> Saved
    end.
nested_result(Input, Saved) ->
    case relay(Input) of
        {item,Value} -> Value;
        Other -> {error,{{Saved},Other}}
    end.
tagged_tail(Input, Saved) ->
    case relay(Input) of
        {item,Value} -> triple(Value,Saved,Value);
        _ -> Saved
    end.
prepend(Head) ->
    Tail = relay(tail),
    pair(marker,[Head,Tail|Tail]).
tuple_tail(A,B,C) -> triple({tag,{B,C},A},A,A).
list_tail(A,B,C) -> pair([A,{B,C}|A],A).
pattern_tail([H|T],A) -> pair({H,T},A);
pattern_tail([],A) -> A.
grouped_tail({H},A) -> pair({H,A},A);
grouped_tail({H,T},A) -> pair([H|T],A).
single_tail({H,T},A) -> pair([H|T],A).
tagged_single_tail({item,H,7},A) -> pair({H,A},A).
tuple_fallback({item,H,7},A) -> pair({H,A},A);
tuple_fallback(Input,_) -> Input.
cons_fallback([H|T],A) -> pair({H,T},A);
cons_fallback(Input,A) -> pair(A,Input).
cons_tuple_fallback([{item,H,7}=Whole|T],A) -> pair([{H,Whole}|T],A);
cons_tuple_fallback(Input,_) -> Input.
forwarded_tuple(Input,Saved) ->
    Value = relay(Input),
    triple({tag,Value,Saved},marker,[]).
embedded_tuple(A,B,Tail) ->
    Name = relay(B),
    Spec = {A,{A,B},{tag,[A,B]},relay(B),[A]++Tail},
    pair(Name,Spec).
reused_case(A,B,C,State) ->
    Name = relay(State),
    Key = {A,B,C},
    case classify(Name,Key) of
        ok -> pair(Name,Key);
        Other -> Other
    end.
classify(allow,_) -> ok;
classify(Error,_) -> Error.
nil_case([]) -> empty_result();
nil_case(Input) -> {error,{badarg,Input}}.
empty_result() -> empty.
literal_call_case(normal,Input) ->
    Local = {local,probe},
    case probe_start(Local,probe,[Input]) of
        {ok,Value} -> {ok,Value,{normal,Input}};
        Error -> Error
    end;
literal_call_case(_,_) -> {error,badarg}.
probe_start(_,_,[allow]) -> {ok,child};
probe_start(_,_,_) -> {error,denied}.
tuple_or_atom(Input) ->
    case relay(Input) of
        {value,Value} -> Value;
        none -> undefined
    end.
tagged_call_tuple(Input) ->
    case relay(Input) of
        {ok,Value} -> {ok,relay(Value)};
        Error -> Error
    end.
triple(A,B,C) -> {A,B,C}.
relay(Entries) -> Entries.
pair(A, B) -> {A,B}.

