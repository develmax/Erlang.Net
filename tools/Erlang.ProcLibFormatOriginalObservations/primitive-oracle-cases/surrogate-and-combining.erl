-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try string:length([16#D800,16#301]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
