-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,unexpected),try error_logger:get_format_depth() of _->unexpected_success catch error:{case_clause,unexpected}->ok end
    after application:unset_env(kernel,error_logger_format_depth) end.
