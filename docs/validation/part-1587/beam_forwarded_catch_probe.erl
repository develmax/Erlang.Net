-module(beam_forwarded_catch_probe).
-export([run/1]).
run(Input) ->
    Value = prepare(Input),
    catch finish(Input, Value, []).
prepare(fail_before) -> throw(before_call);
prepare(Input) -> {prepared,Input}.
finish(fail_inside, _, _) -> throw(inside_call);
finish(Input, Value, Acc) -> {Input,Value,Acc}.
