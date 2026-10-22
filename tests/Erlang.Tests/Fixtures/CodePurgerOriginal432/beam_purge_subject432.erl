-module(beam_purge_subject432).
-export([version/0,hold/1,fun_value/0,held_fun/1]).
-ifdef(PURGE_ON_LOAD).
-on_load(init/0).
init() -> rejected432.
-endif.
-ifdef(PURGE_V2).
-define(VERSION,2).
-else.
-define(VERSION,1).
-endif.
version() -> ?VERSION.
fun_value() -> fun() -> ?VERSION end.
hold(Parent) -> Parent!{ready432,self()},hold_loop(Parent).
hold_loop(Parent) -> receive stop432 -> Parent!{stopped432,self(),?VERSION},ok end.
held_fun(Parent) ->
 fun() ->
  Parent!{ready432,self()},
  receive stop432 -> Parent!{stopped432,self(),?VERSION},ok end
 end.
