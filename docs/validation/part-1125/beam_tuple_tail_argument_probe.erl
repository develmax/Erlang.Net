-module(beam_tuple_tail_argument_probe).
-export([run/3, member/3, forward/3]).
run(Ref, State, Module) ->
    Error = {undefined_import, helper(Ref), Module},
    finish(State, Ref, Error).
member(Source, Key, Value) ->
    List = read(Source),
    lists:member({Key, Value}, List).
forward(Source, Key, Value) ->
    List = read(Source),
    finish({Key, Value}, List).
read(Source) -> Source.
finish(Key, Value) -> {Key, Value}.
helper(Ref) -> {name, Ref}.
finish(State, Ref, Error) -> {State, Ref, Error}.

