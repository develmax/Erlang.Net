-module(binary_insert369_host).
-export([run/0]).
run() ->
    <<2>> = binary:replace(<<2>>,<<1>>,<<7>>,[{insert_replaced,true}]),
    true = bad_function(),
    true = pin_case(0,true,false),
    true = pin_case(-1,true,true),
    true = pin_case(258,true,true),
    true = pin_case([0],false,false),
    true = pin_case([],false,false),
    done.
bad_function() ->
    try binary:replace(<<1>>,<<1>>,<<7>>,[{insert_replaced,true}]) of
        _ -> false
    catch error:badarg -> true end.
pin_case(Insert,ExpectedPin,ExpectedError) ->
    Replacement = append_loop(257,<<>>),
    Before = binary:referenced_byte_size(Replacement),
    WasError = try binary:replace(<<1>>,<<1>>,Replacement,[{insert_replaced,Insert}]) of
        _ -> false
    catch error:badarg -> true end,
    After = binary:referenced_byte_size(Replacement),
    ExpectedAfter = case ExpectedPin of true -> 257; false -> Before end,
    Before > 257 andalso After =:= ExpectedAfter andalso WasError =:= ExpectedError.
append_loop(0,Binary) -> Binary;
append_loop(Count,Binary) -> append_loop(Count-1,<<Binary/binary,7>>).
