-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try io_lib:format("~tp",[[65,[66]]],[{chars_limit,3}]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
