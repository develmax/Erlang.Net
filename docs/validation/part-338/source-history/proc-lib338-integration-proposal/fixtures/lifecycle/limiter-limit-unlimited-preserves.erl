-module(oracle_proc_limiter).
-export([run/0]).
run()->
    try ok=application:set_env(kernel,error_logger_format_depth,unlimited),Value={a,[b|c],#{d=>{e,f}},<<7:3>>},Value=error_logger:limit_term(Value),ok
    after application:unset_env(kernel,error_logger_format_depth) end.
