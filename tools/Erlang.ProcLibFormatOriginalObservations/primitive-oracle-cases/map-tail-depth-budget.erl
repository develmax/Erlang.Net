-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try io_lib:format("~tP",[#{alpha=>{a,{b,c}},beta=>{d,{e,f}},gamma=>{g,{h,i}}},5],[{chars_limit,30}]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
