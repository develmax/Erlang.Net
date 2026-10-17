-module(map_size_callback_contract).
-export([load/1,observe/2,widths/0,run/0]).
load(Path) -> erlang:load_nif(Path, []).
observe(_,_) -> erlang:nif_error(not_loaded).
widths() -> erlang:nif_error(not_loaded).
make_map(N) -> maps:from_list([{I,{value,I}} || I <- lists:seq(1,N)]).
wrong() -> [{atom,a},{integer,42},{big_integer,1 bsl 200},{float,1.0},
    {nil,[]},{list,[1|tail]},{tuple,{#{}}},{binary,<<1,2>>},{bits,<<1:1>>},
    {reference,make_ref()},{pid,self()},{local_fun,fun()->ok end},
    {external_fun,fun erlang:length/1}].
run() ->
    Sizes=[0,1,31,32,33,512,8193],
    Rows=[
      {size_sizes,[{N,observe(0,make_map(N))} || N <- Sizes]},
      {size_nonmaps,[{K,observe(2,V)} || {K,V} <- wrong()]},
      {size_null_env,[{N,observe(1,make_map(N))} || N <- Sizes]},
      {size_guards,[{N,observe(0,make_map(N))} || N <- [0,32,33,8193]]},
      {size_values,[observe(0,M) || M <- [#{a=>#{b=>[1,2]}},
        #{a=>erlang:list_to_binary(lists:duplicate(65537,7)),b=>1 bsl 200},#{1=>a,1.0=>b}]]},
      {size_independent,observe(3,make_map(33))},
      {size_foreign_thread,observe(4,unused)},
      {size_retained,observe(5,unused)}
    ],
    {map_size_callback_contract,Rows,widths()}.
