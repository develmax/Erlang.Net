%% Focused application callback; executed unchanged on both runtimes.
-module(application_port_callback).
-behaviour(application).
-export([start/2, stop/1, prep_stop/1, config_change/3, start_phase/3, loop/0]).
start(_Type, fail) -> {error, deliberate};
start(Type, Args) ->
    Parent = self(),
    Pid = spawn_link(fun() ->
        Parent ! {started, self(),
            {application:get_application(), application:get_env(marker),
             application:get_all_env(), application:get_key(vsn),
             application:get_all_key(), application:start_type(), Type, Args}},
        loop()
    end),
    receive {started, Pid, Snapshot} -> {ok, Pid, Snapshot} end.
stop(_State) -> ok.
prep_stop(State) -> State.
config_change(_Changed, _New, _Removed) -> ok.
start_phase(_Phase, _Type, _Args) -> ok.
loop() ->
    receive
        {From, snapshot} ->
            From ! {self(), application:get_application(), application:get_env(marker),
                application:get_all_env(), application:get_key(vsn),
                application:get_all_key(), application:start_type()},
            loop();
        stop -> ok;
        _ -> loop()
    end.
