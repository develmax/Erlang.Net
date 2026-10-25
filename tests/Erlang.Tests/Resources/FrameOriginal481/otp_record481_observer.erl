-module(otp_record481_observer).
-export([worker/1,run/0]).
-record(state,{errors=[]}).
-file("otp_record481_observer.erl",1).
worker(Parent)->Parent!{finished,run()}.
run()->[one(M)||M<-[normal,reject_one,reject_many,wrong_mode]].
one(Mode)->
 put(cleanup,[]),
 Result=try nested(Mode) of V->{returned,V} catch C:R:S->{caught,C,R,S} end,
 Effects=erase(cleanup),{Mode,Result,Effects}.
nested(Mode)->
 try
  try
   case Mode of
    normal->kept;
    reject_one->reject(boom,#state{});
    reject_many->reject(next,#state{errors=[old]})
   end
  after put(cleanup,[inner|get(cleanup)]) end
 after put(cleanup,[outer|get(cleanup)]) end.
reject(Error,State)->throw(State#state{errors=[Error|State#state.errors]}).
