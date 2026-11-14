-module(beam_constructed_continuation).
-export([run/1, finish/3]).
run(Input) ->
    Options = [{name, filename:rootname(Input)}],
    finish(module, [Input], Options).
finish(Module, Args, Options) ->
    {Module, Args, Options}.
