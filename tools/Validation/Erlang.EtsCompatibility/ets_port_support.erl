%% Empty real-supervisor callback for the test runtime's kernel_safe_sup.
%% The original disk_log and DETS modules install their own normal children.
-module(ets_port_support).
-export([init/1]).
init([]) -> {ok,{{one_for_one,10,60},[]}}.
