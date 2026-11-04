-module(timer_port_cases).
-export([write/1, capture/2, worker/3, scenario/1, scenario_worker/2, init/1, action/2, identity/1]).

write(Directory) ->
    Cases = cases() ++ edges(),
    Rows = [{F,A,observe(F,A)} || {F,A} <- Cases],
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),
                        term_to_binary(timer:module_info(exports) -- [{module_info,0},{module_info,1}])),
    Effects = [{N,observe_scenario(N)} || N <- [start,callbacks,malformed,once,interval,repeatedly,overlap,owner_death,registered,tc,kill]],
    ok = file:write_file(filename:join(Directory,"effects.etf"),term_to_binary(Effects)),
    ok.

capture(F,A) ->
    try apply(timer,F,A) of
        {ok,{Type,Ref}} when is_reference(Ref) -> {ok,cancel}=timer:cancel({Type,Ref}),{ok,{ok,{Type,reference}}};
        V -> {ok,V}
    catch C:R:S -> timing_outcome(C,R,S)
    end.

worker(F,A,Parent) -> Parent!{self(),capture(F,A)}.
observe(F,A) -> Parent=self(),P=spawn(fun()->worker(F,A,Parent) end),receive {P,V}->V after 2000->error(observation_timeout) end.

scenario_worker(Name,Parent) -> Parent!{self(),scenario(Name)}.
observe_scenario(Name) ->
    P=spawn(?MODULE,scenario_worker,[Name,self()]),
    receive {P,Value}->Value after 5000->error({scenario_timeout,Name}) end.

timing_outcome(C,R,[{erlang,convert_time_unit,[T,native,U],Info}|Rest]) when is_integer(T),T>=0 ->
    {C,R,[{erlang,convert_time_unit,[nonnegative_elapsed,native,U],Info}|Rest]};
timing_outcome(C,R,S) -> {C,R,S}.

cases() ->
    Numbers = [0,1,-1,1.5,-0.0,1 bsl 100,1 bsl 2048,bad,[],{},<<>>],
    Arithmetic = [{F,[N]} || F <- [seconds,minutes,hours],N <- Numbers],
    BadApply = [{F,[T,V]} || F <- [apply_after,apply_interval,apply_repeatedly],
                            T <- [-1,bad,0,1], V <- [bad,[],{},42,fun identity/1]] ++
               [{F,[T,fun identity/1,A]} || F <- [apply_after,apply_interval,apply_repeatedly],
                                         T <- [-1,bad],A <- [[],[x],[x,y],[x|bad]]] ++
               [{F,[T,M,N,A]} || F <- [apply_after,apply_interval,apply_repeatedly],
                                {T,M,N,A} <- [{-1,erlang,abs,[-1]},
                                    {bad,erlang,abs,[-1]},{0,42,abs,[]},
                                    {1,erlang,42,[]},{1,erlang,abs,[x|bad]}]],
    Sends = [{F,[T,D,message]} || F <- [send_after,send_interval],
                T <- [-1,bad,0,1],D <- [42,[],{name,42},{42,node()}]] ++
            [{F,[-1,message]} || F <- [send_after,send_interval,exit_after]] ++
            [{kill_after,[-1]},{kill_after,[-1,self()]},{exit_after,[-1,self(),reason]}],
    Arithmetic ++ BadApply ++ Sends ++
    [{cancel,[V]} || V <- [bad,{},[],{instant,42},{once,42},{send_local,42},{interval,42}]] ++
    [{hms,A} || A <- [[1,2,3],[1.5,-2,3],[bad,1,2],[1,bad,2],[1,2,bad]]] ++
    [{now_diff,A} || A <- [[{1,2,3},{0,1,2}],[{0,0,0},{1,0,1}],
                           [{1.5,2,3},{0,0,0}],[bad,{0,0,0}],
                           [{bad,0,0},{0,0,0}],[{0,bad,0},{0,0,0}],
                           [{0,0,bad},{0,0,0}]]] ++
    [{sleep,[V]} || V <- [-1,bad,1.5,[],{},0,1]] ++
    [{tc,[bad]},{tc,[bad,nanosecond]},{tc,[fun identity/1]},
     {tc,[fun identity/1,[]]},{tc,[fun identity/1,[x,y]]},
     {tc,[fun identity/1,[x],bad]},
     {tc,[erlang,abs,[-1],bad]},{tc,[42,abs,[-1]]},
     {tc,[erlang,abs,[bad]]},{tc,[fun identity/1,bad,microsecond]},
     {tc,[fun identity/1,[bad],bad]},
     {init,[bad]},{code_change,[old,state,extra]},
     {handle_cast,[ignored,state]},{handle_info,[ignored,state]},
     {handle_call,[ignored,from,state]},{terminate,[reason,undefined]}].

identity(X) -> X.

edges() ->
    Times=[0,1,10000,16#ffffffff,16#100000000,1 bsl 40,1 bsl 64,1 bsl 100,-1,-0.0,1.0,bad,[],{}],
    Apply=[{F,[T,M,N,A]} || F<-[apply_after,apply_interval,apply_repeatedly],T<-Times,
             {M,N,A}<-[{erlang,abs,[-1]},{erlang,abs,[]},{erlang,abs,[x|bad]},
                         {missing_module,missing_function,[]},{erlang,42,[]},{42,abs,[]},{erlang,abs,bad}]],
    Fun=[{F,[T,fun identity/1,A]} || F<-[apply_after,apply_interval,apply_repeatedly],
            T<-Times,A<-[[],[value],[x,y],[x|bad],bad]],
    Tc=[{tc,A} || A<-[[bad,[]],[bad,[value]],[fun identity/1,[x|bad]],
                         [bad,[],second],[fun identity/1,[x|bad],second],
                         [erlang,abs,[x|bad]],[erlang,abs,bad,second],
                         [erlang,abs,[1],0],[erlang,abs,[1],-1],
                         [erlang,abs,[1],[]],[erlang,abs,[1],1.0]]],
    Apply++Fun++Tc.

init([]) -> {ok,{#{strategy=>one_for_one,intensity=>3,period=>5},[]}}.

ensure_server() ->
    case whereis(timer_server) of
        undefined -> {ok,P}=timer:start_link(),unlink(P),P;
        P -> P
    end.

scenario(callbacks) ->
    {ok,Tab}=timer:init([]),
    {reply,{ok,{once,R}},Tab}=timer:handle_call(
        {apply_once,{erlang:monotonic_time(millisecond),10000,{erlang,abs,[-1]}}},from,Tab),
    Before=ets:lookup(Tab,R),
    {reply,{ok,cancel},Tab}=timer:handle_call({cancel,{once,R}},from,Tab),
    Empty=ets:tab2list(Tab),
    {ok,Tab}=timer:code_change(old,Tab,extra),
    {noreply,Tab}=timer:handle_cast(ignored,Tab),
    {noreply,Tab}=timer:handle_info({timeout,R,{apply_once,{erlang,abs,[-1]}}},Tab),
    {noreply,Tab}=timer:handle_info({'DOWN',R,process,self(),normal},Tab),
    ok=timer:terminate(reason,Tab),
    {length(Before),Empty,ets:info(Tab)};
scenario(malformed) ->
    {ok,Tab}=timer:init([]),true=ets:insert(Tab,{17,malformed}),
    Result=try timer:handle_info({timeout,17,{apply_once,invalid}},Tab) of
        Value->{unexpected,Value}
    catch C:R:S->{C,R,S}
    end,
    true=ets:delete(Tab),Result;
scenario(once) ->
    _=ensure_server(),
    Self=self(),
    {ok,{instant,I}}=timer:send_after(0,Self,instant),
    Instant=receive instant -> true after 2000 -> timeout end,
    {ok,cancel}=timer:cancel({instant,I}),
    {ok,{send_local,R}}=timer:send_after(10,local),
    Local=receive local -> true after 2000 -> timeout end,
    {ok,cancel}=timer:cancel({send_local,R}),
    {ok,{send_local,C}}=timer:send_after(10000,cancelled),
    {ok,cancel}=timer:cancel({send_local,C}),
    {ok,cancel}=timer:cancel({send_local,C}),
    {ok,{once,A}}=timer:apply_after(1,erlang,send,[Self,applied]),
    Applied=receive applied -> true after 2000 -> timeout end,
    {ok,cancel}=timer:cancel({once,A}),
    {ok,{instant,_}}=timer:apply_after(0,fun()->Self!fun0 end),
    Fun0=receive fun0 -> true after 2000 -> timeout end,
    {ok,{once,_}}=timer:apply_after(1,fun erlang:send/2,[Self,fun2]),
    Fun2=receive fun2 -> true after 2000 -> timeout end,
    Victim=spawn(fun()->receive never->ok end end),
    Mon=monitor(process,Victim),
    {ok,_}=timer:exit_after(1,Victim,reason),
    Exit=receive {'DOWN',Mon,process,Victim,reason}->true after 2000->timeout end,
    {Instant,Local,Applied,Fun0,Fun2,Exit};
scenario(interval) ->
    _=ensure_server(),
    {ok,R}=timer:send_interval(3,tick),
    Ticks=[receive tick->ok after 2000->timeout end || _<-lists:seq(1,3)],
    {ok,cancel}=timer:cancel(R),
    {ok,cancel}=timer:cancel(R),
    flush(tick),
    NoMore=receive tick->false after 30->true end,
    {Ticks,NoMore};
scenario(repeatedly) ->
    _=ensure_server(),
    Self=self(),
    {ok,R}=timer:apply_repeatedly(3,?MODULE,action,[Self,hold]),
    First=receive {started,P}->P after 2000->timeout end,
    Overlap=receive {started,_}->true after 5->false end,
    First!release,
    Finished=receive {finished,First}->true after 2000->timeout end,
    Second=receive {started,P2}->P2 after 2000->timeout end,
    {ok,cancel}=timer:cancel(R),
    Second!release,
    {is_pid(First),Overlap,Finished,is_pid(Second)};
scenario(overlap) ->
    _=ensure_server(),
    {ok,R}=timer:apply_interval(20,fun action/2,[self(),hold]),
    First=receive {started,P}->P after 2000->timeout end,
    Second=receive {started,P2}->P2 after 2000->timeout end,
    {ok,cancel}=timer:cancel(R),
    First!release,Second!release,
    {is_pid(First),is_pid(Second),First=/=Second};
scenario(owner_death) ->
    Server=ensure_server(),Self=self(),
    Owner=spawn(fun()->{ok,R}=timer:apply_interval(1000,erlang,send,[Self,late]),Self!{ready,R} end),
    OwnerMon=monitor(process,Owner),
    R=receive {ready,Reference}->Reference after 2000->timeout end,
    receive {'DOWN',OwnerMon,process,Owner,_}->ok after 2000->error(owner_timeout) end,
    timer:sleep(20),
    {ok,cancel}=timer:cancel(R),
    {is_process_alive(Server),receive late->false after 10->true end};
scenario(registered) ->
    _=ensure_server(),register(timer_port_target,self()),
    {ok,R}=timer:send_after(5,timer_port_target,named),
    Named=receive named->true after 2000->timeout end,
    {ok,cancel}=timer:cancel(R),
    {ok,R2}=timer:send_interval(5,timer_port_target,named_interval),
    NamedInterval=receive named_interval->true after 2000->timeout end,
    {ok,cancel}=timer:cancel(R2),unregister(timer_port_target),
    {Named,NamedInterval};
scenario(start) ->
    case whereis(kernel_sup) of
        undefined -> {ok,K}=supervisor:start_link({local,kernel_sup},?MODULE,[]),unlink(K);
        _->ok
    end,
    ok=timer:start(), Pid=whereis(timer_server),
    ok=timer:start(),
    {error,{already_started,Pid}}=timer:start_link(),
    {is_pid(Pid),Pid=:=whereis(timer_server),already_started};
scenario(tc) ->
    Results=[timer:tc(fun()->value end),timer:tc(fun()->value end,millisecond),
             timer:tc(fun identity/1,[value]),timer:tc(fun identity/1,[value],nanosecond),
             timer:tc(?MODULE,identity,[value]),timer:tc(?MODULE,identity,[value],1000)],
    [{is_integer(T),T>=0,V} || {T,V}<-Results];
scenario(kill) ->
    _=ensure_server(),
    Parent=self(),
    Worker=spawn(fun()->{ok,_}=timer:kill_after(10),Parent!ready,receive never->ok end end),
    Mon=monitor(process,Worker),
    receive ready->ok after 2000->error(ready_timeout) end,
    First=receive {'DOWN',Mon,process,Worker,killed}->true after 2000->timeout end,
    Victim=spawn(fun()->receive never->ok end end),
    M=monitor(process,Victim),{ok,_}=timer:kill_after(1,Victim),
    Second=receive {'DOWN',M,process,Victim,killed}->true after 2000->timeout end,
    Exiter=spawn(fun()->{ok,_}=timer:exit_after(10,reason),receive never->ok end end),
    EM=monitor(process,Exiter),
    Third=receive {'DOWN',EM,process,Exiter,reason}->true after 2000->timeout end,
    {First,Second,Third}.

action(Parent,hold) -> Parent!{started,self()},receive release->ok end,Parent!{finished,self()};
action(Parent,Delay) -> Parent!{started,self()},timer:sleep(Delay),Parent!{finished,self()}.
flush(M) -> receive M->flush(M) after 0->ok end.
