-module(beam_retained_tuple_construction).
-export([pack/2, packed_change/2, shared_before_call/2, duplicate_head/2, guarded/2, recursive_single/3,
         shadowed/2, invoke_shadow/3, rejected_head/1, rejected_guard/1,
         explicit_failure/2, observed_failure/1, direct_second/1, direct_third/2, repeated_producer_input/1]).

pack({A,B,Payload,C}, Key) ->
    R = put(Key, Payload),
    {ok,{A,B,R,C}}.

packed_change({A,Payload,C}, Key) ->
    {C,put(Key,Payload),A,changed}.

shared_before_call({A,B,Payload,C}, Destination) ->
    _ = put(Destination, {A,Payload}),
    {ok,{C,B,A}}.

duplicate_head({A,A,Payload,C}, Key) ->
    R = put(Key, Payload),
    {R,A,C}.

guarded({A,B,Payload,C}, Key) when is_integer(B) ->
    R = put(Key, Payload),
    {R,A,B,C}.

recursive_single(F, S, [H | T]) ->
    {A, S1} = F(H, S),
    {As, S2} = recursive_single(F, S1, T),
    {[A | As], S2};
recursive_single(_, S, []) ->
    {[], S}.

shadowed({A,B,Payload,C}, Key) ->
    R = put(Key, Payload),
    {A,B,R,C,fun(A) -> {A,B} end}.

invoke_shadow(Outer, Inner, Key) ->
    {Outer,kept,undefined,last,F} = shadowed({Outer,kept,saved,last}, Key),
    F(Inner).

rejected_head(Key) ->
    try duplicate_head({left,right,value,last}, Key)
    catch error:function_clause -> get(Key)
    end.

rejected_guard(Key) ->
    try guarded({left,not_integer,value,last}, Key)
    catch error:function_clause -> get(Key)
    end.

explicit_failure({A,B,Payload,C}, Key) ->
    R = producer_failure(Payload, Key),
    {A,B,R,C}.

producer_failure(Payload, Key) ->
    put(Key, Payload),
    case Key of
        normal_return -> Payload;
        _ -> erlang:error(failed)
    end.

observed_failure(Key) ->
    try explicit_failure({left,right,saved,last}, Key)
    catch error:failed -> erase(Key)
    end.

direct_second({A,B,Payload}) ->
    R = put(field_key, Payload),
    {A,B,R}.

direct_third(Key, {A,B,Payload}) ->
    R = describe(Key, default, Payload),
    {A,B,R}.

repeated_producer_input({A,B,Payload}) ->
    R = pair(Payload, Payload),
    {A,B,R}.

describe(Key, Default, Payload) ->
    {Key,Default,Payload}.

pair(First, Second) ->
    {First,Second}.
