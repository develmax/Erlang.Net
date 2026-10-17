-module(default_load_nif_invalid_filename).
-export([run/0,check/1]).

run() ->
    [done = default_load_nif_invalid_filename:check(Value) ||
        Value <- [42,invalid_atom,1.0,{},<<1:1>>,[0],[-1],[16#d800],[16#110000],
                  [97|invalid_tail],[<<"nested_binary">>]]],
    done.

check(Value) ->
    try erlang:load_nif(Value,[]) of
        Returned -> {unexpected_return,Returned}
    catch error:badarg:Stack ->
        [{erlang,load_nif,[Value,[]],Info}|_] = Stack,
        {error_info,Details} = lists:keyfind(error_info,1,Info),
        #{module := erl_erts_errors} = Details,
        done
    end.
