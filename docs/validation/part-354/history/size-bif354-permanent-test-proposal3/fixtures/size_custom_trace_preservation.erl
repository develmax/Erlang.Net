-module(size_custom_trace_preservation).
-compile({no_auto_import,[tuple_size/1,map_size/1,bit_size/1,byte_size/1]}).
-export([run/0,tuple_size/1,map_size/1,bit_size/1,byte_size/1]).
run() ->
    lists:foreach(fun({Name,Fun})->
        try Fun(bad) of _->error(missing_custom)
        catch error:{custom,Name}:Stack ->
            [{size_custom_trace_preservation,Name,[bad],Location}|_]=Stack,
            {error_info,#{module:=size_custom_errors,cause:=Name}}=lists:keyfind(error_info,1,Location),ok
        end
    end,[{tuple_size,fun tuple_size/1},{map_size,fun map_size/1},{bit_size,fun bit_size/1},{byte_size,fun byte_size/1}]),
    try erlang:raise(error,explicit_empty,[]) catch error:explicit_empty:Stack0 -> []=Stack0 end,done.
tuple_size(V)->erlang:error({custom,tuple_size},[V],[{error_info,#{module=>size_custom_errors,cause=>tuple_size}}]).
map_size(V)->erlang:error({custom,map_size},[V],[{error_info,#{module=>size_custom_errors,cause=>map_size}}]).
bit_size(V)->erlang:error({custom,bit_size},[V],[{error_info,#{module=>size_custom_errors,cause=>bit_size}}]).
byte_size(V)->erlang:error({custom,byte_size},[V],[{error_info,#{module=>size_custom_errors,cause=>byte_size}}]).
