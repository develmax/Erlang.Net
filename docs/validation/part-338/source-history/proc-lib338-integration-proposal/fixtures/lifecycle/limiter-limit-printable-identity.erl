-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,10),Value=lists:duplicate(40,65),Value=error_logger:limit_term(Value),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
