-module(otp_receive484_priority).
-export([run/1,child/2]).
run(Path)->
 process_flag(priority,high),
 L=seq(64017),Parent=self(),P=spawn(?MODULE,child,[Parent,L]),
 receive {ready,P,A}->
  1=erlang:trace(P,true,[running]),P!go,
  receive {trace,P,out,{length_host_guard,receive_length,1}}->
   true=erlang:suspend_process(P),
   {current_stacktrace,Stack}=process_info(P,current_stacktrace),
   true=lists:any(fun({length_host_guard,receive_length,1,_})->true;(_)->false end,Stack),
   ok=erlang:send(A,priority,[priority]),true=erlang:resume_process(P),
   receive {done,R}->
    {ok,[L]}=R,ok=file:write_file(Path,term_to_binary(R,[deterministic])),
    io:format("~p",[64017])
   after 5000->error(priority_result_timeout) end
  after 5000->error(priority_yield_timeout) end
 after 5000->error(priority_ready_timeout) end.
child(Parent,L)->
 A=alias([priority]),Parent!{ready,self(),A},receive go->ok end,
 R=length_host_guard:receive_length(L),Parent!{done,R}.
seq(0)->[];seq(N)->[[]|seq(N-1)].
