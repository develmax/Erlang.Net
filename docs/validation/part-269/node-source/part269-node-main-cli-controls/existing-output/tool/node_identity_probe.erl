-module(node_identity_probe).
-export([run/2]).

run(Case, Output) ->
    put(output, Output),
    save("stage", <<"started">>),
    try
        observe(Case),
        save("stage", <<"complete">>),
        halt(0)
    catch Class:Reason:Stack ->
        term("exception", {Class, Reason, Stack}),
        save("stage", <<"exception">>),
        halt(1)
    end.

observe(Case) ->
    Steps = steps(Case),
    Owner = self(),
    {Child, Monitor} = spawn_monitor(fun child/0),
    try
        Ref = make_ref(),
        Terms = {Owner, Child, Ref},
        Map = #{Owner => owner, Child => child, Ref => reference},
        Standard = term_to_binary(Terms),
        Local = term_to_binary(Terms, [local]),
        Name = list_to_atom("erl_net_identity_" ++ os:getpid() ++ "_" ++ Case),
        Options = #{name_domain => shortnames, dist_listen => false},
        term("inputs", {Case, Steps, Name, Options, Standard, Local}),
        snapshot("before", Terms, Map, Standard, Local),
        lists:foreach(fun({Phase, start}) ->
                              Started = net_kernel:start(Name, Options),
                              term(Phase ++ "-operation", Started),
                              require_started(Started),
                              snapshot(Phase, Terms, Map, Standard, Local);
                         ({Phase, stop}) ->
                              Stopped = net_kernel:stop(),
                              term(Phase ++ "-operation", Stopped),
                              require_stopped(Stopped),
                              snapshot(Phase, Terms, Map, Standard, Local)
                      end, Steps)
    after
        try
            term("cleanup-kernel", outcome(fun net_kernel:stop/0))
        after
            cleanup_child(Child, Monitor)
        end
    end.

require_started({ok, Kernel}) when is_pid(Kernel) -> ok;
require_started(Other) -> error({probe_start_failed, Other}).

require_stopped(ok) -> ok;
require_stopped(Other) -> error({probe_stop_failed, Other}).

cleanup_child(Child, Monitor) ->
    Child ! stop,
    receive {'DOWN', Monitor, process, Child, Reason} ->
        term("cleanup-child", {graceful, Reason})
    after 5000 ->
        exit(Child, kill),
        receive {'DOWN', Monitor, process, Child, Reason} ->
            term("cleanup-child", {forced, Reason})
        after 5000 ->
            term("cleanup-child", timeout),
            error(probe_cleanup_timeout)
        end
    end.

steps("start") -> [{"started", start}];
steps("start-stop") -> [{"started", start}, {"stopped", stop}];
steps("start-stop-restart") ->
    [{"started", start}, {"stopped", stop}, {"restarted", start}].

snapshot(Phase, {Owner, Child, Ref} = Terms, Map, Standard, Local) ->
    save(Phase ++ "-standard.etf", term_to_binary(Terms)),
    save(Phase ++ "-local.etf", term_to_binary(Terms, [local])),
    Child ! {ping, self()},
    Ping = receive {pong, Child, ChildNode} -> {pong, ChildNode}
           after 5000 -> timeout
           end,
    Facts = #{node => node(), creation => erlang:system_info(creation),
              alive => erlang:is_alive(), pid_node => node(Owner),
              child_node => node(Child), reference_node => node(Ref),
              owner_same => Owner =:= self(),
              owner_lookup => maps:find(Owner, Map),
              child_lookup => maps:find(Child, Map),
              reference_lookup => maps:find(Ref, Map),
              hashes => {erlang:phash2(Owner), erlang:phash2(Child), erlang:phash2(Ref)},
              standard_decode => decode(Standard, Terms),
              local_decode => decode(Local, Terms), child_ping => Ping},
    term(Phase ++ "-facts", Facts).

decode(Bytes, Original) ->
    outcome(fun() ->
        {P, C, R} = binary_to_term(Bytes),
        #{same => {P, C, R} =:= Original,
          nodes => {node(P), node(C), node(R)},
          owner_alive => outcome(fun() -> erlang:is_process_alive(P) end),
          child_alive => outcome(fun() -> erlang:is_process_alive(C) end)}
    end).

outcome(Fun) ->
    try {result, Fun()}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

child() ->
    receive
        {ping, Owner} -> Owner ! {pong, self(), node()}, child();
        stop -> ok
    end.

term(Name, Value) -> save(Name ++ ".etf", term_to_binary(Value)).

save(Name, Bytes) ->
    ok = file:write_file(filename:join(get(output), Name), Bytes).
