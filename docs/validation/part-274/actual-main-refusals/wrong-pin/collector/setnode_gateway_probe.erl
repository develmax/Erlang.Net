-module(setnode_gateway_probe).
-export([bad/2,start/2,watch/2,state/0]).
bad(N,C) -> try erlang:setnode(N,C) of _ -> unexpected catch error:badarg -> badarg end.
start(N,C) -> erlang:setnode(N,C).
watch(F,O) -> erlang:process_flag({monitor_nodes,O},F).
state() -> {erlang:is_alive(),erlang:node(),erlang:system_info(creation)}.
