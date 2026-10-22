-module(beam_proc_subject391).
-export([worker/2,probe/1,spawn_child/1,start_child/2,no_ack/0,
         metadata_leaf/0,value/1,hibernate_child/1,hibernated_leaf/1,system_child/1]).

worker(Parent,Case) ->
    erlang:process_flag(trap_exit,true),
    Outcome = try probe(Case) of Value -> {returned,Value}
              catch Class:Reason:Stack -> {caught,Class,Reason,Stack} end,
    Parent ! {Case,Outcome},
    ok.

probe(spawn1) -> spawn_case(spawn,1);
probe(spawn2) -> spawn_case(spawn,2);
probe(spawn3) -> spawn_case(spawn,3);
probe(spawn4) -> spawn_case(spawn,4);
probe(spawn_link1) -> spawn_case(spawn_link,1);
probe(spawn_link2) -> spawn_case(spawn_link,2);
probe(spawn_link3) -> spawn_case(spawn_link,3);
probe(spawn_link4) -> spawn_case(spawn_link,4);
probe(spawn_opt2) -> spawn_case(spawn_opt,2);
probe(spawn_opt3) -> spawn_case(spawn_opt,3);
probe(spawn_opt4) -> spawn_case(spawn_opt,4);
probe(spawn_opt5) -> spawn_case(spawn_opt,5);
probe(start3) -> start_case(start,3);
probe(start4) -> start_case(start,4);
probe(start5) -> start_case(start,5);
probe(start_link3) -> start_case(start_link,3);
probe(start_link4) -> start_case(start_link,4);
probe(start_link5) -> start_case(start_link,5);
probe(start_monitor3) -> start_case(start_monitor,3);
probe(start_monitor4) -> start_case(start_monitor,4);
probe(start_monitor5) -> start_case(start_monitor,5);
probe(init_ack1) -> erlang:put('$ancestors',[self()]), ok=proc_lib:init_ack(ack_value), Self=self(), receive {ack,Self,Value}->Value end;
probe(init_ack2) -> ok=proc_lib:init_ack(self(),ack_value), Self=self(), receive {ack,Self,Value}->Value end;
probe(init_fail2) -> erlang:put('$ancestors',[self()]), proc_lib:init_fail(failed,{exit,normal});
probe(init_fail3) -> proc_lib:init_fail(self(),failed,{exit,normal});
probe(init_p3) -> proc_lib:init_p(self(),[],fun metadata_leaf/0);
probe(init_p5) -> proc_lib:init_p(self(),[],?MODULE,metadata_leaf,[]);
probe(wake_up3) -> proc_lib:wake_up(?MODULE,value,[awake]);
probe(hibernate3) -> hibernate_case();
probe(initial_call1) -> proc_lib:initial_call([{{dictionary,'$initial_call'},{?MODULE,value,1}}]);
probe(translate_initial_call1) -> proc_lib:translate_initial_call([{{dictionary,'$initial_call'},{?MODULE,value,1}}]);
probe(set_label1) -> proc_lib:set_label({label,[1,2],#{a=>value}}), erlang:get('$process_label');
probe(get_label1) -> erlang:put('$process_label',{label,[1,2],#{a=>value}}), proc_lib:get_label(self());
probe(format1) -> proc_lib:format([[],[]]);
probe(format2) -> proc_lib:format([[],[]],unicode);
probe(format3) -> proc_lib:format([[],[]],latin1,4);
probe(report_cb2) -> proc_lib:report_cb(#{label=>{proc_lib,crash},report=>[[],[]]},#{});
probe(stop1) -> stop_case(1);
probe(stop3) -> stop_case(3);
probe(spawn_badfun) -> proc_lib:spawn(not_fun);
probe(spawn_mfa_badmodule) -> proc_lib:spawn(17,value,[]);
probe(spawn_opt_badopts) -> proc_lib:spawn_opt(?MODULE,value,[],not_list);
probe(start_monitor_option) -> proc_lib:start(?MODULE,start_child,[parent_marker,one],10,[monitor]);
probe(start_monitor_tuple_option) -> proc_lib:start_monitor(?MODULE,start_child,[parent_marker,one],10,[{monitor,[]}]);
probe(start_timeout) -> proc_lib:start(?MODULE,no_ack,[],0);
probe(initial_call_unknown) -> proc_lib:initial_call([]);
probe(translate_unknown) -> proc_lib:translate_initial_call([]);
probe(initial_call_bad_shape) -> proc_lib:initial_call(invalid_shape);
probe(init_ack_missing_ancestors) -> erlang:erase('$ancestors'), proc_lib:init_ack(ack_value);
probe(init_fail_raw_invalidclass) -> erlang:register(beam_proc391_parent,self()), proc_lib:init_fail(beam_proc391_parent,failed,{not_class,nested_reason,[]});
probe(wake_up_badmodule) -> proc_lib:wake_up(17,value,[]);
probe(hibernate_badmodule) -> proc_lib:hibernate(17,value,[]).

spawn_case(Function,Arity) ->
    Parent=self(), Fun=fun()->spawn_child(Parent) end,
    Args=case {Function,Arity} of
        {spawn_opt,2}->[Fun,[]];
        {spawn_opt,3}->[node(),Fun,[]];
        {spawn_opt,4}->[?MODULE,spawn_child,[Parent],[]];
        {spawn_opt,5}->[node(),?MODULE,spawn_child,[Parent],[]];
        {_,1}->[Fun]; {_,2}->[node(),Fun];
        {_,3}->[?MODULE,spawn_child,[Parent]];
        {_,4}->[node(),?MODULE,spawn_child,[Parent]]
    end,
    Pid=erlang:apply(proc_lib,Function,Args),
    Ref=erlang:monitor(process,Pid),
    Report=receive {started,Pid,Data}->Data end,
    Pid ! finish,
    receive {'DOWN',Ref,process,Pid,Reason}->{is_pid(Pid),Report,Reason} end.

spawn_child(Parent) ->
    Parent ! {started,self(),metadata(Parent)},
    receive finish->ok end.

start_case(Function,Arity) ->
    Args=case Arity of
        3->[?MODULE,start_child,[self(),one]];
        4->[?MODULE,start_child,[self(),two],5000];
        5->[?MODULE,start_child,[self(),one],5000,[{priority,normal}]]
    end,
    Result=erlang:apply(proc_lib,Function,Args),
    {Pid,Report}=receive {started,Child,Data}->{Child,Data} end,
    {Ref,Public}=case Function of
        start_monitor->{ready,Monitor}=Result,{Monitor,{ready,is_reference(Monitor)}};
        _->{erlang:monitor(process,Pid),Result}
    end,
    Pid ! finish,
    receive {'DOWN',Ref,process,Pid,Reason}->{Public,Report,Reason} end.

start_child(Parent,Ack) ->
    Parent ! {started,self(),metadata(Parent)},
    case Ack of one->proc_lib:init_ack(ready);two->proc_lib:init_ack(Parent,ready) end,
    receive finish->ok end.

metadata(Parent) ->
    [First|Rest]=erlang:get('$ancestors'),
    {First=:=Parent,length(Rest),proc_lib:initial_call(self()),
     proc_lib:translate_initial_call(self()),proc_lib:get_label(self())}.

metadata_leaf() -> metadata(self()).
value(Value) -> Value.
no_ack() -> receive never->ok end.

hibernate_case() ->
    {Pid,Ref}=erlang:spawn_monitor(?MODULE,hibernate_child,[self()]),
    receive {hibernating,Pid}->Pid ! wake end,
    Report=receive {awake,Pid,Value}->Value end,
    receive {'DOWN',Ref,process,Pid,Reason}->{Report,Reason} end.
hibernate_child(Parent) ->
    Parent ! {hibernating,self()},
    proc_lib:hibernate(?MODULE,hibernated_leaf,[Parent]).
hibernated_leaf(Parent) -> receive wake->Parent ! {awake,self(),awake} end.

stop_case(Arity) ->
    Pid=proc_lib:spawn(?MODULE,system_child,[self()]),
    receive {system_ready,Pid}->ok end,
    Ref=erlang:monitor(process,Pid),
    Result=case Arity of 1->proc_lib:stop(Pid);3->proc_lib:stop(Pid,normal,5000) end,
    receive {'DOWN',Ref,process,Pid,Reason}->{Result,Reason} end.
system_child(Parent) ->
    Parent ! {system_ready,self()},
    receive
        {system,{From,Tag},{terminate,Reason}} ->
            From ! {Tag,ok}, erlang:exit(Reason)
    end.
