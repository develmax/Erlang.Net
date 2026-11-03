-module(build_profile521).
-export([run/0]).
run() ->
    Selectors = [build_type, emu_type, debug_compiled, smp_support,
                 otp_release, version, system_version, emu_flavor, system_architecture],
    Before = [{Selector, capture(fun() -> erlang:system_info(Selector) end)} || Selector <- Selectors],
    OriginalOnline = erlang:system_flag(schedulers_online, 1),
    try
        OneOnline = {erlang:system_info(schedulers_online), erlang:system_info(smp_support)},
        Invalid = [{Value, capture(fun() -> erlang:system_info(Value) end)} ||
                   Value <- [{build_type}, <<"build_type">>, 1, [build_type]]],
        {Before, OneOnline, Invalid}
    after
        erlang:system_flag(schedulers_online, OriginalOnline)
    end.
capture(Function) ->
    try Function() of Value -> {returned, Value}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack} end.
