-module(otp_apply477_observer).
-export([worker/1,run/0,tail/3,tail_frame/3,non_tail/3,fixed_tail/3,fixed_non_tail/3,fixed/5,fixed_value/5]).
-file("otp_apply477_observer.erl",1).
worker(Parent) -> Parent ! {finished,run()}.
run() ->
    [observe(Route,M,F,A) || Route<-[tail,tail_frame,non_tail,fixed_tail,fixed_non_tail],{M,F,A}<-cases()].
tail(M,F,A) -> apply(M,F,A).
tail_frame(M,F,A) -> put(apply_marker,true),apply(M,F,A).
non_tail(M,F,A) -> {returned,apply(M,F,A)}.
fixed_tail(M,F,A) -> fixed(erlang,apply,M,F,A).
fixed_non_tail(M,F,A) -> fixed_value(erlang,apply,M,F,A).
fixed(OuterM,OuterF,M,F,A) -> OuterM:OuterF(M,F,A).
fixed_value(OuterM,OuterF,M,F,A) -> {returned,OuterM:OuterF(M,F,A)}.
cases() ->
    [{odbc,code_change,[old,state,extra]},
     {odbc,disconnect,[bad]},
     {erlang,hd,[[a,b]]},{erlang,hd,[bad]},
     {erlang,length,[[a,b]]},{erlang,length,[bad]},
     {erlang,error,[reason]},{erlang,error,[reason,[arg]]},
     {erlang,throw,[reason]},{erlang,exit,[reason]},
     {missing_module,missing,[]},{odbc,missing,[]},
     {bad,0,[]},{0,bad,[]},{odbc,disconnect,bad},
     {odbc,disconnect,[a|bad]},
     {erlang,apply,[odbc,disconnect,[bad]]},
     {erlang,apply,[erlang,apply,[odbc,disconnect,[bad]]]},
     {erlang,apply,[odbc,disconnect,bad]},
     {erlang,apply,[erlang,error,[reason]]},
     {erlang,apply,[erlang,throw,[reason]]},
     {erlang,apply,[bad,[]]},
     {odbc,missing,lists:duplicate(1024,argument)}].
observe(Route,M,F,A) ->
    try apply(?MODULE,Route,[M,F,A]) of V -> {{Route,M,F,A},{returned,V}}
    catch C:R:S -> {{Route,M,F,A},{caught,C,R,S}}
    end.
