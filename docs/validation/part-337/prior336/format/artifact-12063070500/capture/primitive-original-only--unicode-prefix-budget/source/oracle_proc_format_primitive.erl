-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try io_lib:format("~tp",[[101,16#301,102,16#301,103,16#301]],[{chars_limit,5}]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
