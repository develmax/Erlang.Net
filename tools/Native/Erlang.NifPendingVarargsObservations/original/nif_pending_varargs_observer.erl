-module(nif_pending_varargs_observer).
-export([run/1, metadata/0]).

run(Case) ->
    {module, nif_pending_varargs_fixture} = code:load_file(nif_pending_varargs_fixture),
    Before = capture(fun nif_pending_varargs_fixture:tuple_empty/0),
    TaintsBefore = erlang:system_info(taints),
    LoadInfo = case Case of load_veto -> 17; _ -> 42 end,
    Load = capture(fun() -> nif_pending_varargs_fixture:load(os:getenv("NIF_FIXTURE_PATH"), LoadInfo) end),
    TaintsAfter = erlang:system_info(taints),
    Function = case Case of load_veto -> tuple_empty; _ -> Case end,
    Call = capture(fun() -> apply(nif_pending_varargs_fixture, Function, []) end),
    {nif_pending_varargs_observation, Case,
        {load_info, LoadInfo}, {before_load, Before}, {load_result, Load}, {call_result, Call},
        {taints_before, TaintsBefore}, {taints_after, TaintsAfter}}.

capture(Fun) ->
    try Fun() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

metadata() ->
    {nif_pending_varargs_metadata,
        {otp_release, erlang:system_info(otp_release)},
        {erts_version, erlang:system_info(version)},
        {system_version, erlang:system_info(system_version)},
        {system_architecture, erlang:system_info(system_architecture)},
        {wordsize, erlang:system_info(wordsize)},
        {emulator_type, erlang:system_info(emu_flavor)},
        {build_type, erlang:system_info(build_type)}}.
