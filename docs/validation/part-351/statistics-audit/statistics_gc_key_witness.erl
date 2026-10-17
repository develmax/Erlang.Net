-module(statistics_gc_key_witness).
-export([run/0]).
run()->try erlang:statistics(garbage_collection) of Value->{returned,Value} catch error:Reason->{error,Reason} end.
