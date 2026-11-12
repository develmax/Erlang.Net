-module(beam_guarded_list_lookup_probe).
-export([lookup/2, mode/1, active/1, partition/3]).

lookup([], Default) ->
    Default;
lookup([{active, Value} | _], _) when Value =< 0 ->
    false;
lookup([{active, Value} | _], _) ->
    Value;
lookup([_ | Tail], Default) ->
    lookup(Tail, Default).

mode(Value) ->
    validate(mode, Value).

active(Value) ->
    validate(active, Value).

validate(mode, Value) when Value =/= list, Value =/= binary ->
    throw({error, {options, {mode, Value}}});
validate(active, Value) when Value >= -32768, Value =< 32767 ->
    ok;
validate(active, Value) when Value =/= true, Value =/= false, Value =/= once ->
    throw({error, {options, {active, Value}}});
validate(_, _) ->
    ok.

partition([{mode, Value} = Opt | Tail], Native, Emulated) ->
    validate(mode, Value),
    partition(Tail, Native, [Opt | proplists:delete(mode, Emulated)]);
partition([{header, _} = Opt | _], _, _) ->
    throw({error, {options, {not_supported, Opt}}});
partition([{active, Value} = Opt | Tail], Native, Emulated) ->
    validate(active, Value),
    partition(Tail, Native, [Opt | proplists:delete(active, Emulated)]);
partition([{packet, _} = Opt | _], _, _) ->
    throw({error, {options, {not_supported, Opt}}});
partition([{packet_size, _} = Opt | _], _, _) ->
    throw({error, {options, {not_supported, Opt}}});
partition([Opt | Tail], Native, Emulated) ->
    partition(Tail, [Opt | Native], Emulated);
partition([], Native, Emulated) ->
    {lists:reverse(Native), Emulated}.
