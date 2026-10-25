-module(creation_boundary_probe).
-export([run/2]).

run(Case, Output) ->
    put(output, Output),
    false = erlang:is_alive(),
    nonode@nohost = node(),
    {Requested, RetainedCreation} = inputs(Case),
    Node = creation_boundary@localhost,
    {Kernel, Monitor} = spawn_monitor(fun kernel/0),
    true = register(net_kernel, Kernel),
    try
        Retained = retained(Node, RetainedCreation),
        term("inputs", {Case, Node, Requested, RetainedCreation}),
        term("runtime-profile", {erlang:system_info(wordsize), erlang:system_info(build_type),
                                 erlang:system_info(system_architecture)}),
        term("before", {node(), erlang:system_info(creation), erlang:is_alive()}),
        term("retained-before", Retained),
        term("call-outcome", outcome(fun() -> erlang:setnode(Node, Requested) end)),
        term("active", {node(), erlang:system_info(creation), erlang:is_alive()}),
        term("owner-after", self()),
        %% This later use keeps the external identifier alive throughout selection.
        term("retained-after", Retained),
        exit(Kernel, kill),
        receive {'DOWN', Monitor, process, Kernel, DownReason} ->
            term("kernel-down", DownReason)
        after 5000 -> error(kernel_down_timeout) end,
        wait_inactive(500),
        term("inactive", {node(), erlang:system_info(creation), erlang:is_alive()}),
        save("stage", <<"complete">>),
        ok
    after
        exit(Kernel, kill)
    end.

inputs(truncated_zero) -> {1 bsl 32, none};
inputs(truncated_one) -> {(1 bsl 32) + 1, none};
inputs(maximum_small) -> {(1 bsl 59) - 1, none};
inputs(first_big) -> {1 bsl 59, none};
inputs(retained_four) -> {1 bsl 32, 4};
inputs(retained_max_wrap) -> {(1 bsl 59) - 1, (1 bsl 32) - 1}.

retained(_Node, none) -> none;
retained(Node, Creation) ->
    <<131, NodeTerm/binary>> = term_to_binary(Node),
    binary_to_term(<<131, 90, 3:16, NodeTerm/binary, Creation:32, 173:32, 29:32, 53:32>>).

kernel() -> receive stop -> ok end.

wait_inactive(0) -> error(kernel_inactive_timeout);
wait_inactive(Remaining) ->
    case erlang:is_alive() of
        false -> ok;
        true -> receive after 10 -> wait_inactive(Remaining - 1) end
    end.

outcome(Fun) ->
    try {result, Fun()}
    catch CallClass:CallReason:CallStack -> {exception, CallClass, CallReason, CallStack}
    end.

term(Name, Value) -> save(Name ++ ".etf", term_to_binary(Value)).
save(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
