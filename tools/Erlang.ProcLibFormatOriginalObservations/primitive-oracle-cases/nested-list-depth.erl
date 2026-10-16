-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try io_lib:format("~P",[[{a,[1,2]},[3,4]|tail],3]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
