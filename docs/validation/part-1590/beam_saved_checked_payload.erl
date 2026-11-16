-module(beam_saved_checked_payload).
-export([run/2]).
run(Input, Other) ->
    {ok, Payload} = prepare(Input),
    ok = touch(Payload),
    finish(Payload, Other).
prepare(fail) -> invalid;
prepare(Value) -> {ok,{payload,Value}}.
touch(_) -> ok.
finish(Value, Other) -> {Value,Other}.
