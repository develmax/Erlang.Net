-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try string:length([<<101>>,<<16#CC,16#81>>]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
