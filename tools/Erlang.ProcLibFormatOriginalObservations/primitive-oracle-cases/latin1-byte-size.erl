-module(oracle_proc_format_primitive).
-export([run/0]).
run()->try iolist_size([<<16#C3>>,<<16#A9>>]) of Value->{value,Value} catch Class:Reason->{exception,Class,Reason} end.
