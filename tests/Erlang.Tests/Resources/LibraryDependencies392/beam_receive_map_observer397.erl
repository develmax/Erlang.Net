-module(beam_receive_map_observer397).
-export([cases/0,run/0,run_stacks/0,worker/1,rlog/0,rpair/0,rint/0]).

cases() -> [first_clause, second_clause, optional_missing, optional_nonmap, optional_type_missing, optional_tag_missing, std_info_falls_through, tag_falls_through, malformed_skipped, missing_timeout, time_missing, msg_missing, two_values, second_key_missing_skipped, exact_numeric_key, active_try, repeat, empty_map].

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

value(first_clause) -> mail([{log,#{meta=>#{error_logger=>#{tag=>info_report,type=>other}}}},tail],fun rlog/0);
value(second_clause) -> mail([{log,#{msg=>hello,meta=>#{time=>1}}},tail],fun rlog/0);
value(optional_missing) -> mail([{log,#{msg=>hello,meta=>#{time=>1,error_logger=>#{}}}},tail],fun rlog/0);
value(optional_nonmap) -> mail([{log,#{msg=>hello,meta=>#{time=>1,error_logger=>bad}}},tail],fun rlog/0);
value(optional_type_missing) -> mail([{log,#{msg=>hello,meta=>#{time=>1,error_logger=>#{tag=>info_report}}}},tail],fun rlog/0);
value(optional_tag_missing) -> mail([{log,#{msg=>hello,meta=>#{time=>1,error_logger=>#{type=>other}}}},tail],fun rlog/0);
value(std_info_falls_through) -> mail([{log,#{msg=>hello,meta=>#{time=>1,error_logger=>#{tag=>info_report,type=>std_info}}}},tail],fun rlog/0);
value(tag_falls_through) -> mail([{log,#{msg=>hello,meta=>#{time=>1,error_logger=>#{tag=>wrong,type=>other}}}},tail],fun rlog/0);
value(malformed_skipped) -> mail([bad,{log,bad},{log,#{}},{log,#{meta=>bad}},{log,#{msg=>hello,meta=>#{time=>1}}},tail],fun rlog/0);
value(missing_timeout) -> mail([{log,#{}},{log,#{meta=>#{}}},tail],fun rlog/0);
value(time_missing) -> mail([{log,#{msg=>hello,meta=>#{}}},tail],fun rlog/0);
value(msg_missing) -> mail([{log,#{meta=>#{time=>1}}},tail],fun rlog/0);
value(two_values) -> mail([#{tag=>left,type=>42},tail],fun rpair/0);
value(second_key_missing_skipped) -> mail([#{tag=>stale},#{tag=>fresh,type=>42},tail],fun rpair/0);
value(exact_numeric_key) -> mail([#{1.0=>float},#{1=>integer},tail],fun rint/0);
value(active_try) -> mail([bad,#{tag=>left,type=>42},tail],fun()->try rpair() after ok end end);
value(repeat) -> mail([#{tag=>stale},#{tag=>one,type=>1},#{tag=>two,type=>2},tail],fun()->{rpair(),rpair()}end);
value(empty_map) -> mail([#{},tail],fun rpair/0).

mail(Messages,Fun) ->
    send_all(Messages),
    try Fun() of Value -> {Value,drain()}
    after drain()
    end.
send_all([]) -> ok;
send_all([M|Rest]) -> self() ! M, send_all(Rest).
drain() -> receive M -> [M|drain()] after 0 -> [] end.

rlog() ->
    receive
        {log,#{meta:=#{error_logger:=#{tag:=info_report,type:=Type}}}=Log}
          when Type =/= std_info -> {buffer,Log};
        {log,#{msg:=_,meta:=#{time:=_}}=Log} -> {print,Log}
    after 0 -> timeout
    end.

rpair() -> receive #{tag:=Tag,type:=Type} -> {Tag,Type} after 0 -> timeout end.
rint() -> receive #{1:=Value} -> Value after 0 -> timeout end.
