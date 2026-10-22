-module(beam_net_kernel_observer400).
-export([cases/0,run/0,run_stacks/0,worker/1,kernel/1,controller/3]).

cases() -> [self_pid, dead_pid, local_epoch_zero, local_epoch_one, local_epoch_max, remote_zero, remote_one, remote_max, flags_off_lifecycle, flags_on_lifecycle, atom, integer, huge_integer, negative, float, nil, proper_list, improper_list, tuple, map, binary, bitstring, export_function].

run() ->
    ensure_kernel(),
    {Child, Ref} = erlang:spawn_monitor(?MODULE, worker, [self()]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Ref, process, Child, normal} -> Values;
                {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
            after 5000 -> erlang:error(child_not_settled)
            end;
        {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
    after 30000 -> erlang:exit(Child, kill), erlang:error(child_timeout)
    end.

worker(Owner) ->
    Owner ! {self(), rows(cases())},
    ok.

rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run()},
        Zero = at_depth(0),
        One = at_depth(1),
        Two = at_depth(2),
        Eight = at_depth(8),
        [Default, Zero, One, Two, Eight]
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run()}.

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

value(self_pid) -> net_kernel:dflag_unicode_io(self());
value(dead_pid) ->
    {Pid,Ref} = spawn_monitor(fun() -> ok end),
    receive {'DOWN',Ref,process,Pid,normal} -> net_kernel:dflag_unicode_io(Pid) end;
value(local_epoch_zero) -> net_kernel:dflag_unicode_io(pid(node(),0));
value(local_epoch_one) -> net_kernel:dflag_unicode_io(pid(node(),1));
value(local_epoch_max) -> net_kernel:dflag_unicode_io(pid(node(),16#ffffffff));
value(remote_zero) -> net_kernel:dflag_unicode_io(pid(net_kernel400_unknown@localhost,0));
value(remote_one) -> net_kernel:dflag_unicode_io(pid(net_kernel400_unknown@localhost,1));
value(remote_max) -> net_kernel:dflag_unicode_io(pid(net_kernel400_unknown@localhost,16#ffffffff));
value(flags_off_lifecycle) -> lifecycle(net_kernel400_off@localhost,flags());
value(flags_on_lifecycle) -> lifecycle(net_kernel400_on@localhost,flags() bor 16#1000);
value(atom) -> net_kernel:dflag_unicode_io(not_pid);
value(integer) -> net_kernel:dflag_unicode_io(0);
value(huge_integer) -> net_kernel:dflag_unicode_io(340282366920938463463374607431768211456);
value(negative) -> net_kernel:dflag_unicode_io(-1);
value(float) -> net_kernel:dflag_unicode_io(1.5);
value(nil) -> net_kernel:dflag_unicode_io([]);
value(proper_list) -> net_kernel:dflag_unicode_io([1]);
value(improper_list) -> net_kernel:dflag_unicode_io([1|tail]);
value(tuple) -> net_kernel:dflag_unicode_io({pid,1});
value(map) -> net_kernel:dflag_unicode_io(#{pid=>1});
value(binary) -> net_kernel:dflag_unicode_io(<<1>>);
value(bitstring) -> net_kernel:dflag_unicode_io(<<1:1>>);
value(export_function) -> net_kernel:dflag_unicode_io(fun erlang:self/0).

pid(Node, Creation) ->
    Name = atom_to_binary(Node,utf8),
    binary_to_term(<<131,88,119,(byte_size(Name)),Name/binary,123:32,0:32,Creation:32>>).

ensure_kernel() ->
    case whereis(net_kernel) of
        undefined ->
            Parent = self(),
            Pid = spawn(?MODULE,kernel,[Parent]),
            receive {kernel_ready,Pid} -> ok after 5000 -> error(kernel_setup_timeout) end;
        _ -> ok
    end.

kernel(Parent) ->
    true = register(net_kernel,self()),
    true = erlang:setnode(net_kernel400_local@localhost,1),
    Parent ! {kernel_ready,self()},
    receive stop -> ok end.

lifecycle(Node,Flags) ->
    Pid = pid(Node,1),
    Before = net_kernel:dflag_unicode_io(Pid),
    _ = erts_internal:new_connection(Node),
    Pending = net_kernel:dflag_unicode_io(Pid),
    {Controller,Ref} = spawn_monitor(?MODULE,controller,[self(),Node,Flags]),
    receive {controller_ready,Controller} -> ok after 5000 -> error(controller_setup_timeout) end,
    Connected = net_kernel:dflag_unicode_io(Pid),
    OldCreation = net_kernel:dflag_unicode_io(pid(Node,16#ffffffff)),
    Controller ! stop,
    receive {'DOWN',Ref,process,Controller,normal} -> ok after 5000 -> error(controller_exit_timeout) end,
    After = await_idle(Pid,1000),
    {Before,Pending,Connected,OldCreation,After}.

controller(Parent,Node,Flags) ->
    _ = erlang:setnode(Node,self(),{Flags,1}),
    Parent ! {controller_ready,self()},
    receive stop -> ok end.

await_idle(Pid,Remaining) ->
    case net_kernel:dflag_unicode_io(Pid) of
        false -> false;
        true when Remaining > 0 -> receive after 1 -> await_idle(Pid,Remaining-1) end;
        true -> error(disconnect_not_settled)
    end.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor 16#800 bor
    16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor 16#2000000 bor
    (16#04 bsl 32) bor (16#01 bsl 32) bor (16#08 bsl 32) bor 16#08 bor 16#20.
