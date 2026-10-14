-module(local_node_admission_probe).
-export([run/1]).

run(Output) ->
    put(output, Output),
    false = erlang:is_alive(),
    nonode@nohost = node(),
    term("before", {node(), erlang:system_info(creation), erlang:is_alive()}),
    term("missing-kernel", outcome(fun() -> erlang:setnode(admission_probe@localhost, 0) end)),
    {Kernel, Monitor} = spawn_monitor(fun kernel/0),
    true = register(net_kernel, Kernel),
    try
        Node = admission_probe@localhost,
        NodeBytes = term_to_binary(Node),
        <<131, NodeTerm/binary>> = NodeBytes,
        External = binary_to_term(<<131, 90, 3:16, NodeTerm/binary, 4:32, 173:32, 29:32, 53:32>>),
        term("retained-external", External),
        term("invalid-name", outcome(fun() -> erlang:setnode(invalid_node, 0) end)),
        term("invalid-type", outcome(fun() -> erlang:setnode(1, 0) end)),
        term("forbidden-nonode", outcome(fun() -> erlang:setnode(nonode@nohost, 0) end)),
        term("negative-creation", outcome(fun() -> erlang:setnode(Node, -1) end)),
        term("overflow-creation", outcome(fun() -> erlang:setnode(Node, 1 bsl 32) end)),
        ok = net_kernel:monitor_nodes(true),
        ok = net_kernel:monitor_nodes(true),
        term("admitted", outcome(fun() -> erlang:setnode(Node, 0) end)),
        term("active", {node(), erlang:system_info(creation), erlang:is_alive(), node(External)}),
        term("nodeup-first", event(nodeup, Node)),
        term("nodeup-second", event(nodeup, Node)),
        term("already-active", outcome(fun() -> erlang:setnode(other_admission@localhost, 19) end)),
        exit(Kernel, kill),
        term("kernel-down", receive {'DOWN', Monitor, process, Kernel, Reason} -> Reason after 5000 -> timeout end),
        term("nodedown-first", event(nodedown, Node)),
        term("nodedown-second", event(nodedown, Node)),
        wait_inactive(500),
        term("inactive", {node(), erlang:system_info(creation), erlang:is_alive()}),
        term("retained-after", External),
        save("stage", <<"complete">>),
        ok
    after
        exit(Kernel, kill)
    end.

kernel() -> receive stop -> ok end.

event(Tag, Node) ->
    receive {Tag, Node} = Event -> Event after 5000 -> timeout end.

wait_inactive(0) -> error(kernel_inactive_timeout);
wait_inactive(Remaining) ->
    case erlang:is_alive() of
        false -> ok;
        true -> receive after 10 -> wait_inactive(Remaining - 1) end
    end.

outcome(Fun) ->
    try {result, Fun()}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

term(Name, Value) -> save(Name ++ ".etf", term_to_binary(Value)).
save(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
