-module(apply_optimizer_local).
-export([run/1]).
-compile({no_auto_import,[{apply,3}]}).
apply(Mod,Fun,Args)->{local,Mod,Fun,Args}.
run(M)->apply(erlang,call_on_load_function,[M]).
