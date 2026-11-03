-module(variadic_family526).
-export([load/1,run/0,normal/1,cpu/1,io/1]).
-nifs([normal/1,cpu/1,io/1]).
load(Path) -> erlang:load_nif(Path,0).
run() ->
    Payload = {lists:seq(1,300), <<1,2,3>>, #{native526 => true}},
    [normal(Payload),cpu(Payload),io(Payload)].
normal(_) -> erlang:nif_error(not_loaded526).
cpu(_) -> erlang:nif_error(not_loaded526).
io(_) -> erlang:nif_error(not_loaded526).
