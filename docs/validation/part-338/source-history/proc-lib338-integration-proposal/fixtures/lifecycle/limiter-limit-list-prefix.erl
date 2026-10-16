-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,10),[x,x,x,x,x,x,x,x,x,'...']=error_logger:limit_term(lists:duplicate(20,x)),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
