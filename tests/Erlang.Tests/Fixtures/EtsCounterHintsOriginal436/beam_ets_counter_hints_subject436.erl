-module(beam_ets_counter_hints_subject436).
-export([cases/0,worker/2]).
cases()->[c1,c2,c3,c4,c5,c6,c7,c8,c9,c10,c11,c12,c13,c14,c15,c16,c17,c18,c19,c20,c21,c22,c23,c24,c25,c26,c27,c28,c29,c30,c31,c32].
worker(Parent,Case)->
 Outcome=try probe(options(Case)) of V->{returned,V} catch K:R:S->
  case Case of c29->{own_pid_error,K,R,own_pid_stack(S,self())}; _->{raised,K,R,S} end end,
 Parent!{Case,Outcome}.
probe(Options)->
 T=ets:new(counter_hints436,Options),
 try true=ets:insert(T,{key,1}),
 {ets:info(T,decentralized_counters),ets:info(T,size),ets:lookup(T,key)}
 after true=ets:delete(T) end.
options(c1)->[set,{decentralized_counters,true}];
options(c2)->[set,{decentralized_counters,false}];
options(c3)->[ordered_set,{decentralized_counters,true}];
options(c4)->[ordered_set,{decentralized_counters,false}];
options(c5)->[bag,{decentralized_counters,true}];
options(c6)->[bag,{decentralized_counters,false}];
options(c7)->[duplicate_bag,{decentralized_counters,true}];
options(c8)->[duplicate_bag,{decentralized_counters,false}];
options(c9)->[private,{decentralized_counters,true}];
options(c10)->[private,{decentralized_counters,false}];
options(c11)->[public,{decentralized_counters,true}];
options(c12)->[public,{decentralized_counters,false}];
options(c13)->[{read_concurrency,true},{decentralized_counters,true}];
options(c14)->[{read_concurrency,true},{decentralized_counters,false}];
options(c15)->[{write_concurrency,true},{decentralized_counters,true}];
options(c16)->[{write_concurrency,true},{decentralized_counters,false}];
options(c17)->[{write_concurrency,auto},{decentralized_counters,true}];
options(c18)->[{write_concurrency,auto},{decentralized_counters,false}];
options(c19)->[{write_concurrency,false},{decentralized_counters,true}];
options(c20)->[{write_concurrency,false},{decentralized_counters,false}];
options(c21)->[{decentralized_counters,true},{decentralized_counters,false}];
options(c22)->[{decentralized_counters,false},{decentralized_counters,true}];
options(c23)->[{decentralized_counters,bad}];
options(c24)->[{decentralized_counters,[]}];
options(c25)->[{decentralized_counters,1}];
options(c26)->[{decentralized_counters,1.0}];
options(c27)->[{decentralized_counters,{}}];
options(c28)->[{decentralized_counters,{true}}];
options(c29)->[{decentralized_counters,self()}];
options(c30)->[{decentralized_counters}];
options(c31)->[{decentralized_counters,true,extra}];
options(c32)->[{decentralized_counters,true}|bad].

own_pid_stack([{ets,new,[counter_hints436,[{decentralized_counters,P}]],Info}|_],P)->
 case Info of [{error_info,#{module:=erl_stdlib_errors}}]->true;_->false end;
own_pid_stack(_,_)->false.
