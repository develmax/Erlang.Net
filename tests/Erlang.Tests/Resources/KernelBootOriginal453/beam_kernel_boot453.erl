-module(beam_kernel_boot453).
-export([run/0,run_stacks/0,worker/2]).

cases() -> [registered_kernel,registered_auth,registered_purger,node_identity,alive,state,longnames,
            ticktime,connecttime,allowed,nodes,nodes_info,own_node_info,own_node_info_type,
            monitor_true,monitor_false,monitor_map,monitor_invalid_flag,monitor_invalid_options,
            allow_invalid,allow_invalid_name,apply_kernel,stop_static,getopts_disconnected].

run() -> [{Case,capture(Case,default)} || Case <- cases()].
run_stacks() -> [{Depth,[{Case,capture(Case,Depth)} || Case <- cases()]} || Depth <- [default,0,1,2,8]].

capture(Case,Depth) ->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[Case,Depth]),
    receive
        {'DOWN',Ref,process,Pid,{observation,Value}} -> Value;
        {'DOWN',Ref,process,Pid,Reason} -> {unexpected_down,Reason}
    after 10000 -> error(observer_timeout)
    end.

worker(Case,Depth) ->
    case Depth of default -> ok; _ -> erlang:system_flag(backtrace_depth,Depth) end,
    Value=try probe(Case) of Result -> {ok,Result}
          catch Class:Reason:Stack -> {exception,Class,Reason,Stack}
          end,
    exit({observation,Value}).

probe(registered_kernel) -> is_pid(whereis(net_kernel));
probe(registered_auth) -> is_pid(whereis(auth));
probe(registered_purger) -> is_pid(whereis(erts_code_purger));
probe(node_identity) -> node();
probe(alive) -> erlang:is_alive();
probe(state) -> net_kernel:get_state();
probe(longnames) -> net_kernel:longnames();
probe(ticktime) -> net_kernel:get_net_ticktime();
probe(connecttime) -> net_kernel:connecttime();
probe(allowed) -> net_kernel:allowed();
probe(nodes) -> nodes(connected);
probe(nodes_info) -> net_kernel:nodes_info();
probe(own_node_info) -> net_kernel:node_info(node());
probe(own_node_info_type) -> net_kernel:node_info(node(),type);
probe(monitor_true) -> net_kernel:monitor_nodes(true);
probe(monitor_false) -> net_kernel:monitor_nodes(false);
probe(monitor_map) -> net_kernel:monitor_nodes(true,#{connection_id=>true,node_type=>all,nodedown_reason=>true});
probe(monitor_invalid_flag) -> net_kernel:monitor_nodes(invalid_flag);
probe(monitor_invalid_options) -> net_kernel:monitor_nodes(true,[invalid_option]);
probe(allow_invalid) -> net_kernel:allow(invalid_list);
probe(allow_invalid_name) -> net_kernel:allow([17]);
probe(apply_kernel) -> net_kernel:kernel_apply(erlang,length,[[a,b,c]]);
probe(stop_static) -> net_kernel:stop();
probe(getopts_disconnected) -> net_kernel:getopts('missing453@localhost',[nodelay]).
