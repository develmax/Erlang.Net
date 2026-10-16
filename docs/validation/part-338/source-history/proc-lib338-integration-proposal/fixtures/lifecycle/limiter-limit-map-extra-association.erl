-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,10),Map=maps:from_list([{N,N}||N<-lists:seq(1,20)]),11=map_size(error_logger:limit_term(Map)),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
