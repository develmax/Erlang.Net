-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,1), 10=error_logger:get_format_depth(), ok
    after application:unset_env(kernel,error_logger_format_depth) end.
