-module(beam_map_across_call).
-export([run/1]).
run(Input) ->
    Value = produce(Input),
    Map = #{tag => saved, value => Value},
    finish(invoke(Input, Value), Map).
produce(fail) -> erlang:error(first);
produce(Input) -> put(stage, {produce, Input}), {produced, Input}.
invoke(stop, _) -> erlang:error(second);
invoke(Input, Value) -> put(stage, {invoke, Value}), {invoked, Input, Value}.
finish(Result, Map) -> {Result, Map, get(stage)}.
