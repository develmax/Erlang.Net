-module(beam_match_spec_transform).
-include_lib("stdlib/include/ms_transform.hrl").
-export([static/0,dynamic/1]).
static() -> ets:fun2ms(fun({M,_}=T) when is_atom(M) -> T end).
dynamic(Key) ->
    ets:fun2ms(fun({K,V}) when K =:= Key, is_integer(V) -> {K,V};
                  ({_,_}) -> false
               end).
