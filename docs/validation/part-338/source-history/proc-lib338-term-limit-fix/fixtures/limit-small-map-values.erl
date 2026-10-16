-module(oracle_limit_small_map).
-export([run/0]).
run()->
    ok=application:set_env(kernel,error_logger_format_depth,10),
    Value=#{first=>{second,[third,fourth]}},
    try Value=error_logger:limit_term(Value),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
