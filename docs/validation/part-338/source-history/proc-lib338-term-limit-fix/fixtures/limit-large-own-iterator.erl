-module(oracle_limit_large_map).
-export([run/0]).
run()->
    ok=application:set_env(kernel,error_logger_format_depth,10),
    Value=maps:from_list([{N,{keep,N}}||N<-lists:seq(1,64)]),
    Expected=maps:from_list(first(11,maps:iterator(Value))),
    try Expected=error_logger:limit_term(Value),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
first(0,_)->[];
first(N,Iterator)->
    case maps:next(Iterator) of
        none->[];
        {Key,Value,Next}->[{Key,Value}|first(N-1,Next)]
    end.
