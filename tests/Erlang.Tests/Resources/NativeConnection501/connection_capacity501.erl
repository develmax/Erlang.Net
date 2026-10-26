-module(connection_capacity501).
-export([run/0,run_stacks/0,probe/1]).
run()->ordsets_core_support:run(?MODULE,[pending_1025]).
run_stacks()->ordsets_core_support:profiles(?MODULE,[pending_1025]).
probe(pending_1025)->
 Results=[begin Name=list_to_atom("owned_capacity501_"++integer_to_list(I)++"@localhost"),
   try erlang:apply(erts_internal,opaque(new_connection),[Name]) of H->{ok,Name,H}
   catch C:R:S->{error,I,C,R,S} end
 end || I<-lists:seq(1,1025)],
 Errors=[E||E={error,_,_,_,_}<-Results],
 Clean=[erts_internal:abort_pending_connection(N,H)||{ok,N,H}<-Results],
 {length(Clean),Errors,lists:all(fun(B)->B=:=true end,Clean)}.
opaque(V)->put(connection_capacity501_input,V),erase(connection_capacity501_input).
