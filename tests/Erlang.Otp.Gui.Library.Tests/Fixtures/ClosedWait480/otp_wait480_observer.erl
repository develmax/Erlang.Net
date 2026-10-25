-module(otp_wait480_observer).
-export([worker/1,run/0,one/2]).
-record(state,{count=0,other=retained}).
-file("otp_wait480_observer.erl",1).
worker(Parent)->Parent!{finished,run()}.
run()->[one(M,N)||M<-[basic,closure,record],N<-[0,1,2,8]].
one(Mode,Count)->
 erase(saved_callback),erase(saved_count),
 self()!ignored,
 lists:foreach(fun(I)->self()!{value,I} end,lists:seq(1,Count)),
 self()!{value,Count,9},self()!stop,
 Result=try loop(Mode,#state{}) of V->{returned,V} catch C:R:S->{caught,C,R,S} end,
 Callback=get(saved_callback),
 Effect={is_function(Callback,1),case is_function(Callback,1) of true->Callback(5);false->undefined end,get(saved_count)},
 erase(saved_callback),erase(saved_count),
 Retained=receive ignored->ignored after 0->missing end,
 {Mode,Count,Result,Effect,Retained}.
loop(Mode,S)->
 receive
  stop->exit({stopped,S});
  {value,V}->
   Next=case Mode of
    closure->put(saved_callback,fun(X)->X+V end),S;
    record->S#state{count=V};
    basic->S
   end,
   put(saved_count,V),loop(Mode,Next);
  {value,V,Extra}->put(saved_count,{V,Extra}),loop(Mode,S)
 end.
