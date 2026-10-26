-module(proc_lib_remote_payload).
-export([child/1]).

child(Parent) ->
    Parent ! {remote_monitor_ready,self(),node()},
    receive release_monitor_child -> ok end.
