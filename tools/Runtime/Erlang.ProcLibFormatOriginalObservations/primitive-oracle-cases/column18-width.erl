-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try io_lib:format("~.18p",[{tag,lists:seq(1,40)}]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
