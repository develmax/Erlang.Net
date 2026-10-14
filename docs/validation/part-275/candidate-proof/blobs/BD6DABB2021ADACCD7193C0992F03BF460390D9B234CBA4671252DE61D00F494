-module(setnode_compiled_driver).
-export([run/1]).
run(Output) ->
    put(output, Output),
    term("missing", setnode_gateway_probe:bad(epoch@host, 0)),
    term("inactive", setnode_gateway_probe:state()),
    term("watch", outcome(fun() -> setnode_gateway_probe:watch(true, #{}) end)),
    {Kernel, Ref} = spawn_monitor(fun() -> receive stop -> ok end end),
    true = register(net_kernel, Kernel),
    try
        term("started", outcome(fun() -> setnode_gateway_probe:start(epoch@host, 1) end)),
        term("active", setnode_gateway_probe:state()),
        term("already-active", setnode_gateway_probe:bad(changed@host, 0)),
        term("nodeup", event(nodeup)),
        exit(Kernel, kill),
        term("kernel-down", receive {'DOWN',Ref,process,Kernel,R}->R after 5000->timeout end),
        term("nodedown", event(nodedown)),
        wait_inactive(500),
        term("stopped", setnode_gateway_probe:state()),
        term("cleanup", {is_process_alive(Kernel),whereis(net_kernel),node(),erlang:is_alive()}),
        save("stage", <<"complete">>)
    after exit(Kernel, kill) end.
event(Tag) -> receive {Tag,Node,Info}=Value when is_atom(Node),is_map(Info)->Value after 5000->timeout end.
wait_inactive(0)->error(kernel_inactive_timeout);
wait_inactive(N)->case erlang:is_alive() of false->ok;true->receive after 10->wait_inactive(N-1) end end.
outcome(F)->try {result,F()} catch C:R:S->{exception,C,R,S} end.
term(Name,V)->save(Name++".etf",term_to_binary(V)).
save(Name,B)->ok=file:write_file(filename:join(get(output),Name),B).
