-module(beam_receive_callback_observer399).
-export([cases/0,run/0,run_stacks/0,worker/1]).

cases() -> [first_match, skip_then_match, all_nomatch, empty, false_is_match, nil_is_match, tuple_nomatch_is_match, timeout_is_match, badfun_with_message, badfun_without_message, badarity_with_message, badarity_without_message, error_preserves_candidate, throw_preserves_candidate, exit_preserves_candidate, error_after_rejection, invalid_timeout_bypassed_on_match, invalid_timeout_after_nomatch, negative_timeout_empty, infinity_with_match, yield_then_match, send_then_match, send_then_reject, queue_visible_during_callback, priority_then_match, priority_then_reject, nested_receive_other, nested_timeout_then_match].

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

value(first_match) -> probe([a,b],fun(M)->visit(M),{chosen,M}end,0);
value(skip_then_match) -> probe([a,b,c],fun(M)->visit(M),case M of b->{chosen,b};_->nomatch end end,0);
value(all_nomatch) -> probe([a,b],fun(M)->visit(M),nomatch end,0);
value(empty) -> probe([],fun(M)->visit(M),chosen end,0);
value(false_is_match) -> probe([a,b],fun(M)->visit(M),false end,0);
value(nil_is_match) -> probe([a,b],fun(M)->visit(M),[] end,0);
value(tuple_nomatch_is_match) -> probe([a,b],fun(M)->visit(M),{nomatch}end,0);
value(timeout_is_match) -> probe([a,b],fun(M)->visit(M),timeout end,0);
value(badfun_with_message) -> probe([a],bad,0);
value(badfun_without_message) -> probe([],bad,0);
value(badarity_with_message) -> probe([a],fun()->chosen end,0);
value(badarity_without_message) -> probe([],fun()->chosen end,0);
value(error_preserves_candidate) -> probe([a,b],fun(M)->visit(M),erlang:error(callback_error)end,0);
value(throw_preserves_candidate) -> probe([a,b],fun(M)->visit(M),throw(callback_throw)end,0);
value(exit_preserves_candidate) -> probe([a,b],fun(M)->visit(M),exit(callback_exit)end,0);
value(error_after_rejection) -> probe([a,b,c],fun(M)->visit(M),case M of a->nomatch;_->erlang:error(callback_error)end end,0);
value(invalid_timeout_bypassed_on_match) -> probe([a],fun(M)->visit(M),chosen end,wrong);
value(invalid_timeout_after_nomatch) -> probe([a],fun(M)->visit(M),nomatch end,wrong);
value(negative_timeout_empty) -> probe([],fun(M)->visit(M),chosen end,-1);
value(infinity_with_match) -> probe([a],fun(M)->visit(M),chosen end,infinity);
value(yield_then_match) -> probe([a,b],fun(M)->visit(M),erlang:yield(),chosen end,0);
value(send_then_match) -> probe([a,b],fun(M)->visit(M),self()!injected,chosen end,0);
value(send_then_reject) -> probe([a,b],fun(M)->visit(M),case M of a->self()!injected,nomatch;injected->chosen;_->nomatch end end,0);
value(queue_visible_during_callback) -> probe([a,b],fun(M)->visit(M),{messages,Messages}=process_info(self(),messages),{snapshot,Messages}end,0);
value(priority_then_match) -> priority_probe(match);
value(priority_then_reject) -> priority_probe(reject);
value(nested_receive_other) -> probe([a,b,c],fun(M)->visit(M),case {M,get(nested)} of {a,undefined}->put(nested,done),receive b->ok after 0->ok end,nomatch;{c,_}->chosen;_->nomatch end end,0);
value(nested_timeout_then_match) -> probe([a,b],fun(M)->visit(M),receive unrelated->ignored after 0->chosen end end,0).

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
