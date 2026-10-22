-module(beam_receive_callback_nested399).
-export([cases/0,run/0,run_stacks/0,worker/1]).

cases() -> [consume_current_accept,consume_current_reject].

run() ->
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

value(consume_current_accept) -> probe([a,b],fun(M)->visit(M),case M of a->receive a->ok after 0->ok end,chosen;_->chosen end end,0);
value(consume_current_reject) -> probe([a,b,c],fun(M)->visit(M),case M of a->receive a->ok after 0->ok end,nomatch;c->chosen;_->nomatch end end,0).

probe(Messages,Fun,Timeout) ->
    erase(visits), erase(nested), put(visits,[]),
    send_all(Messages),
    try prim_eval:'receive'(Fun,Timeout) of
        Value -> {returned,Value,lists:reverse(get(visits)),drain()}
    catch Class:Reason:Stack ->
        {caught,Class,Reason,Stack,lists:reverse(get(visits)),drain()}
    after erase(visits),erase(nested),drain()
    end.

visit(Message) ->
    Visits = get(visits),
    case length(Visits) >= 8 of
        true -> erlang:error(callback_visit_bound);
        false -> put(visits,[Message|Visits])
    end.

priority_probe(Mode) ->
    Alias = alias([priority]),
    try probe([a,b],fun(M) ->
        visit(M),
        case M of
            a -> erlang:send(Alias,priority_message,[priority]),erlang:yield(),
                 case Mode of match->chosen;reject->nomatch end;
            priority_message -> priority_chosen;
            _ -> nomatch
        end
    end,0)
    after unalias(Alias)
    end.

send_all([]) -> ok;
send_all([M|Rest]) -> self() ! M, send_all(Rest).
drain() -> receive M -> [M|drain()] after 0 -> [] end.
