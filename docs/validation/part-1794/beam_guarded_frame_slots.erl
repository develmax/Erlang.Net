-module(beam_guarded_frame_slots).
-export([head/1, sequence/2, collect/2]).

-record(spec, {prefix, tag, tail}).

head([#spec{tag=Tag}|Rest]) ->
    accept(Rest,{true,Tag}).

sequence(Value, Info) when is_integer(Value), is_list(Info) ->
    A = put(first, Value),
    B = put(second, A),
    C = {ok,B},
    {C, Info}.

collect(#spec{tag=Options}, Label) ->
    Name = join(Label),
    Items = [I || {entry,I} <- Options],
    accept(Name,[default|Items]).

join(Label) -> {label,Label}.
accept(Rest, Expected) -> {Rest,Expected}.
