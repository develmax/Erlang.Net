-module(error_logger_live_depth).
-export([run/0,log/2,adding_handler/1,removing_handler/1,changing_config/3]).
adding_handler(Config) -> {ok,Config}.
removing_handler(_Config) -> ok.
changing_config(Mode,_Old,New) ->
    case maps:get(config,New,#{}) of
        #{owner := Owner} -> Owner ! {changed,Mode};
        _ -> ok
    end,
    {ok,New}.
log(Event,#{config := #{owner := Owner}=Config}) ->
    case maps:get(action,Config,deliver) of
        fail -> error(handler_failed);
        deliver -> Owner ! {logged,Event}
    end.
reset() ->
    lists:foreach(fun(Id) -> logger:remove_handler(Id) end,logger:get_handler_ids()),
    logger:set_primary_config(#{level=>notice}),
    logger:unset_module_level(),
    logger:unset_process_metadata(),
    ok.
attach() ->
    logger:add_handler(capture,?error_logger_live_depth,#{config=>#{owner=>self()}}).
emit(Meta) ->
    logger:macro_log(#{mfa=>{?error_logger_live_depth,run,0},line=>42},error,#{event=>crash},Meta).
event() -> receive {logged,E} -> E after 1000 -> missing end.
run() ->
    ok=application:unset_env(kernel,error_logger_format_depth),
    A=error_logger:get_format_depth(),
    ok=application:set_env(kernel,error_logger_format_depth,3),
    B=error_logger:get_format_depth(),
    ok=application:set_env(kernel,error_logger_format_depth,20),
    C=error_logger:get_format_depth(),
    ok=application:set_env(kernel,error_logger_format_depth,invalid),
    D=try error_logger:get_format_depth() catch error:{case_clause,invalid}->case_clause end,
    ok=application:unset_env(kernel,error_logger_format_depth),{A,B,C,D}.
