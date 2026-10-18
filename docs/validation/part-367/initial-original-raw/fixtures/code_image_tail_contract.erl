-module(code_image_tail_contract).
-export([cases/0,run/0,run_stacks/0,target/2]).
cases()->[tail,non_tail,held_local_tail,held_local_non_tail,held_external_tail,held_external_non_tail].
run()->[{Name,value(Name)} || Name<-cases()].
value(Name)->try execute(Name) of Value->{ok,Value} catch Class:Reason->{error,Class,Reason} end.
run_stacks()->
    Names=cases(),
    Default={default,[{Name,capture(Name)} || Name<-Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try [Default|[at_depth(Depth,Names) || Depth<-[0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Names)->erlang:system_flag(backtrace_depth,Depth),{Depth,[{Name,capture(Name)} || Name<-Names]}.
capture(Name)->try execute(Name) of Value->{ok,Value} catch Class:Reason:Stack->{error,Class,Reason,Stack} end.
target(Parent,Before)->Inside=erlang:check_process_code(self(),code_image_tail_subject),Parent!{observed,self(),{Before,Inside}},kept.
execute(Mode)->
    Beam=beam(),
    code:purge(code_image_tail_subject),code:delete(code_image_tail_subject),code:purge(code_image_tail_subject),
    {module,code_image_tail_subject}=code:load_binary(code_image_tail_subject,"code_image_tail_subject.erl",Beam),
    Parent=self(),
    {Pid,Monitor}=spawn_monitor(fun()->code_image_tail_subject:run(Parent,Mode) end),
    try
        receive {entered,Pid}->ok; {'DOWN',Monitor,process,Pid,Reason0}->erlang:error({enter_down,Reason0}) after 5000->erlang:error(enter_timeout) end,
        {module,code_image_tail_subject}=code:load_binary(code_image_tail_subject,"code_image_tail_subject.erl",Beam),
        BeforeResume=erlang:check_process_code(Pid,code_image_tail_subject),
        Pid!continue,
        Result=receive {observed,Pid,Observed}->Observed; {'DOWN',Monitor,process,Pid,Reason1}->erlang:error({observe_down,Reason1}) after 5000->erlang:error(observe_timeout) end,
        receive {'DOWN',Monitor,process,Pid,normal}->ok; {'DOWN',Monitor,process,Pid,Reason2}->erlang:error({exit_down,Reason2}) after 5000->erlang:error(exit_timeout) end,
        {BeforeResume,Result}
    after
        stop(Pid,Monitor),
        code:delete(code_image_tail_subject),code:purge(code_image_tail_subject)
    end.
stop(Pid,Monitor)->
    case is_process_alive(Pid) of
        true->exit(Pid,kill),receive {'DOWN',Monitor,process,Pid,_}->ok after 5000->erlang:error(cleanup_timeout) end;
        false->erlang:demonitor(Monitor,[flush]),ok
    end.
beam()->
    case get(code_image_tail_contract_beam) of
        undefined->
            {ok,Tokens,_}=erl_scan:string(subject_source()),
            Forms=forms(Tokens,[],[]),
            {ok,code_image_tail_subject,Beam}=compile:forms(Forms,[binary,deterministic]),
            put(code_image_tail_contract_beam,Beam),Beam;
        Beam->Beam
    end.
forms([],[],Acc)->lists:reverse(Acc);
forms([Dot={dot,_}|Rest],Part,Acc)->{ok,Form}=erl_parse:parse_form(lists:reverse([Dot|Part])),forms(Rest,[],[Form|Acc]);
forms([Token|Rest],Part,Acc)->forms(Rest,[Token|Part],Acc).
subject_source()->"-module(code_image_tail_subject). -export([run/2,version/0]).\nrun(Parent,Mode)->Parent!{entered,self()},receive continue->private(Parent,Mode) after 5000->erlang:error(subject_timeout) end.\nprivate(Parent,tail)->Before=erlang:check_process_code(self(),code_image_tail_subject),F=fun code_image_tail_contract:target/2,F(Parent,Before);\nprivate(Parent,non_tail)->Before=erlang:check_process_code(self(),code_image_tail_subject),F=fun code_image_tail_contract:target/2,Result=F(Parent,Before),{kept,Result};\nprivate(Parent,held_local_tail)->put(held,fun version/0),private(Parent,tail);\nprivate(Parent,held_local_non_tail)->put(held,fun version/0),private(Parent,non_tail);\nprivate(Parent,held_external_tail)->put(held,fun code_image_tail_subject:version/0),private(Parent,tail);\nprivate(Parent,held_external_non_tail)->put(held,fun code_image_tail_subject:version/0),private(Parent,non_tail).\nversion()->held.\n".
