-module(register_sup_probe).
-export([start_child/1, start_link/1, pack/2, pack/3]).
start_child(T) ->
    Selected = case element(1,T) of
        accept -> transport;
        connect -> transport;
        listen -> listener;
        monitor -> monitor
    end,
    register_sup_probe:pack(Selected, [T]).
start_link(Name) ->
    register_sup_probe:pack({local,Name}, register_sup_probe, []).
pack(A,B) -> {A,B}.
pack(A,B,C) -> {A,B,C}.
