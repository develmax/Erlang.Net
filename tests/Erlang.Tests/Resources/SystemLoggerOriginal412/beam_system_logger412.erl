-module(beam_system_logger412).
-export([run/0,run_stacks/0,worker/2]).
cases()->[exchange_atoms,self_pid,dead_pid,death_does_not_reset,roundtrip_local_pid,
    invalid_atom,invalid_true,invalid_integer,invalid_list,invalid_tuple,invalid_binary,remote_pid].
run()->[{C,capture(C,default)} || C<-cases()].
run_stacks()->[{D,[{C,capture(C,D)}||C<-cases()]}||D<-[default,0,1,2,8]].
capture(C,D)->P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},{C,D}]),
    receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},{C,D})->OldDepth=case D of default->default;_->erlang:system_flag(backtrace_depth,D) end,
    V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,
    case OldDepth of default->ok;_->erlang:system_flag(backtrace_depth,OldDepth) end,P!{R,V}.
with_logger(F)->Old=erlang:system_flag(system_logger,logger),try F() after erlang:system_flag(system_logger,Old) end.
remote()->binary_to_term(<<131,88,119,16,"logger412@remote",0,0,0,123,0,0,0,0,0,0,0,7>>).
probe(exchange_atoms)->with_logger(fun()->A=erlang:system_flag(system_logger,undefined),B=erlang:system_info(system_logger),
    C=erlang:system_flag(system_logger,logger),{A,B,C,erlang:system_info(system_logger)} end);
probe(self_pid)->with_logger(fun()->P=self(),logger=erlang:system_flag(system_logger,P),
    A=erlang:system_info(system_logger)=:=P,B=erlang:system_flag(system_logger,undefined)=:=P,{A,B,erlang:system_info(system_logger)} end);
probe(dead_pid)->with_logger(fun()->{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,normal}->ok end,
    A=erlang:system_flag(system_logger,P),{A,erlang:system_info(system_logger)=:=P,erlang:is_process_alive(P)} end);
probe(death_does_not_reset)->with_logger(fun()->{P,M}=spawn_monitor(fun()->receive stop->ok end end),
    logger=erlang:system_flag(system_logger,P),P!stop,receive {'DOWN',M,process,P,normal}->ok end,
    A=erlang:system_info(system_logger)=:=P,B=erlang:system_flag(system_logger,logger)=:=P,{A,B} end);
probe(roundtrip_local_pid)->with_logger(fun()->P=binary_to_term(term_to_binary(self())),
    logger=erlang:system_flag(system_logger,P),{P=:=self(),erlang:system_info(system_logger)=:=self()} end);
probe(invalid_atom)->with_logger(fun()->erlang:system_flag(system_logger,logger412_target) end);
probe(invalid_true)->with_logger(fun()->erlang:system_flag(system_logger,true) end);
probe(invalid_integer)->with_logger(fun()->erlang:system_flag(system_logger,0) end);
probe(invalid_list)->with_logger(fun()->erlang:system_flag(system_logger,[]) end);
probe(invalid_tuple)->with_logger(fun()->erlang:system_flag(system_logger,{logger}) end);
probe(invalid_binary)->with_logger(fun()->erlang:system_flag(system_logger,<<>>) end);
probe(remote_pid)->with_logger(fun()->erlang:system_flag(system_logger,remote()) end).
