-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,10),{1,2,3,4,5,6,7,8,9,'...'}=error_logger:limit_term(list_to_tuple(lists:seq(1,20))),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
