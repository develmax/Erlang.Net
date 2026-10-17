-module(size_valid_values).
-export([run/0]).
run() ->
    lists:foreach(fun(N)->B= <<0:N>>,N=bit_size(B),Expected=(N+7) div 8,Expected=byte_size(B) end,lists:seq(0,65)),
    <<_:3,Slice:11/bitstring,_/bitstring>> = <<7:3,0:11,3:2>>,11=bit_size(Slice),2=byte_size(Slice),
    0=tuple_size({}),3=tuple_size({a,b,c}),256=tuple_size(list_to_tuple(lists:seq(1,256))),
    0=map_size(#{}),65=map_size(maps:from_list([{N,N} || N<-lists:seq(1,65)])),done.
