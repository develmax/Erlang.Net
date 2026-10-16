-module(oracle_proc_lifecycle).
-export([run/0,target/0,awake/1,fail/2,log/2,adding_handler/1,removing_handler/1,changing_config/3]).
run()->F=fun()->{get('$ancestors'),get('$initial_call')} end,MFA=erlang:fun_info_mfa(F),{[parent|tail],MFA}=proc_lib:init_p(parent,tail,F),ok.

target()->{get('$ancestors'),get('$initial_call')}.
awake(P)->receive wake->P!{resumed,self(),get('$initial_call'),get('$process_label')} end.
fail(P,Mode)->proc_lib:set_label(worker_label),put(kept,42),P!{ready,self()},receive crash->crash(Mode) end.
crash(error)->erlang:error(boom);
crash(error_normal)->erlang:error(normal);
crash(throw)->throw(thrown);
crash(exit)->exit(custom_exit);
crash(explicit_empty)->erlang:raise(error,boom,[]);
crash(explicit_user)->erlang:raise(error,boom,[{sample,user,0,[]}]);
crash(cleared)->erase('$initial_call'),put('$ancestors',corrupt),erlang:error(boom).
log(Event,#{config:=#{owner:=Owner}})->Owner!{captured_log,Event},ok.
adding_handler(Config)->{ok,Config}.
removing_handler(_Config)->ok.
changing_config(_Operation,_Old,New)->{ok,New}.
trace_mfas(Trace)->[{M,F,arity(A)}||{M,F,A,_}<-Trace].
arity(A) when is_integer(A)->A;
arity(A)->length(A).
