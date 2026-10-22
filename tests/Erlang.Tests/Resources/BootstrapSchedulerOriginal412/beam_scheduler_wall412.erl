-module(beam_scheduler_wall412).
-export([run/0,run_stacks/0,worker/2]).
cases()->[disable_twice,enable_twice,enable_disable,atom_input,integer_input,list_input,tuple_input,binary_input,pid_input,reference_input,wrong_arity_zero,wrong_arity_two].
run()->[{C,capture(C,default)}||C<-cases()].
run_stacks()->[{D,[{C,capture(C,D)}||C<-cases()]}||D<-[default,0,1,2,8]].
capture(C,D)->P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},{C,D}]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},{C,D})->OldDepth=case D of default->default;_->erlang:system_flag(backtrace_depth,D) end,
 V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,
 case OldDepth of default->ok;_->erlang:system_flag(backtrace_depth,OldDepth) end,P!{R,V}.
with_wall(F)->Old=erts_internal:scheduler_wall_time(false),try F() after erts_internal:scheduler_wall_time(Old) end.
nontrue(Value)->with_wall(fun()->false=erts_internal:scheduler_wall_time(true),
 A=erts_internal:scheduler_wall_time(Value),B=erts_internal:scheduler_wall_time(false),{A,B} end).
probe(disable_twice)->with_wall(fun()->{erts_internal:scheduler_wall_time(false),erts_internal:scheduler_wall_time(false)} end);
probe(enable_twice)->with_wall(fun()->{erts_internal:scheduler_wall_time(true),erts_internal:scheduler_wall_time(true)} end);
probe(enable_disable)->with_wall(fun()->{erts_internal:scheduler_wall_time(true),erts_internal:scheduler_wall_time(false),erts_internal:scheduler_wall_time(false)} end);
probe(atom_input)->nontrue(scheduler412_other);
probe(integer_input)->nontrue(1);
probe(list_input)->nontrue([]);
probe(tuple_input)->nontrue({true});
probe(binary_input)->nontrue(<<"true">>);
probe(pid_input)->nontrue(self());
probe(reference_input)->nontrue(make_ref());
probe(wrong_arity_zero)->with_wall(fun()->apply(erts_internal,scheduler_wall_time,[]) end);
probe(wrong_arity_two)->with_wall(fun()->apply(erts_internal,scheduler_wall_time,[true,false]) end).
