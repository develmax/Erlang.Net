-module(logger_failing_handler_removed).
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
    logger:add_handler(capture,?logger_failing_handler_removed,#{config=>#{owner=>self()}}).
emit(Meta) ->
    logger:macro_log(#{mfa=>{?logger_failing_handler_removed,run,0},line=>42},error,#{event=>crash},Meta).
event() -> receive {logged,E} -> E after 1000 -> missing end.
run() ->
    reset(),
    ok=logger:add_handler(broken,?MODULE,#{config=>#{owner=>self(),action=>fail}}),
    ok=emit(#{}), logger:get_handler_ids().
