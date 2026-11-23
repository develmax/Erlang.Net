-module(beam_deferred_cons_head).
-export([drop/3, captured/2, improper/3, repeated/3, ordered/1,
         head_match/2, head_match_failure/1, shadow/1, malformed/2,
         invoke_captured/3, invoke_shadow/2]).

drop(Rest, Name, Arity) ->
    [{Name, Arity} |
     Rest -- [{Name, Arity}]].

captured(Name, Arity) ->
    fun(Rest) ->
        [{Name, Arity} |
         Rest -- [{Name, Arity}]]
    end.

improper(Head, Key, New) ->
    [{Head, keep} | put(Key, New)].

repeated(Rest, A, B) ->
    [{B, A, A, keep} | Rest -- [{A, B}]].

ordered(Key) ->
    [{put(Key, head), keep} | effect_tail(Key)].

head_match(Input, Key) ->
    [{(expected = Input), keep} | effect_tail(Key)].

head_match_failure(Key) ->
    put(Key, initial),
    try head_match(other, Key)
    catch error:{badmatch, other} -> erase(Key)
    end.

effect_tail(Key) ->
    [put(Key, tail)].

shadow(Name) ->
    {fun(Name) -> [{Name, keep} | put(Name, tail)] end, Name}.

malformed(Name, Arity) ->
    try drop(not_a_list, Name, Arity)
    catch error:badarg -> caught
    end.

invoke_captured(Rest, Name, Arity) ->
    F = captured(Name, Arity),
    F(Rest).

invoke_shadow(Outside, Inside) ->
    {F, Outside} = shadow(Outside),
    F(Inside).
