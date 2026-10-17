-module(size_guard_matchspec).
-export([run/0]).
run() ->
    rejected=tuple_guard(bad),rejected=map_guard(bad),rejected=bits_guard(bad),rejected=bytes_guard(bad),
    accepted=tuple_guard({}),accepted=map_guard(#{}),accepted=bits_guard(<<1:1>>),accepted=bytes_guard(<<1:1>>),
    lists:foreach(fun(F)->
        {ok,false,[],[]}=erlang:match_spec_test({bad},[{{'$1'},[{'>',{F,'$1'},0}],[true]}],table),
        {ok,'EXIT',[],[]}=erlang:match_spec_test({bad},[{{'$1'},[],[{F,'$1'}]}],table)
    end,[tuple_size,map_size,bit_size,byte_size]),done.
tuple_guard(V) when tuple_size(V)>=0 -> accepted;tuple_guard(_)->rejected.
map_guard(V) when map_size(V)>=0 -> accepted;map_guard(_)->rejected.
bits_guard(V) when bit_size(V)>=0 -> accepted;bits_guard(_)->rejected.
bytes_guard(V) when byte_size(V)>=0 -> accepted;bytes_guard(_)->rejected.
