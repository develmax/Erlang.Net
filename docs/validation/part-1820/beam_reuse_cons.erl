-module(beam_reuse_cons).
-export([run/1]).

run([H, Next | Rest]) ->
    {cell, H, run([Next | Rest])};
run([Value]) ->
    Value;
run([]) ->
    none.
