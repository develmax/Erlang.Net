-module(mutex_lifecycle_observer795).
-export([failed/2,upgrade/4,open_upgrade/4,open_a/2,upgrade_b/2,purge_old/0,probe_current/0,destroy_current/0,close_current/0]).

failed(Beam,Library) ->
    {module,mutex_lifecycle795}=erlang:load_module(mutex_lifecycle795,Beam),
    Result=outcome(fun()->mutex_lifecycle795:load(Library) end),
    Delete=erlang:delete_module(mutex_lifecycle795),
    Purge=erlang:purge_module(mutex_lifecycle795),
    {Result,Delete,Purge,erlang:module_loaded(mutex_lifecycle795)}.

upgrade(BeamA,BeamB,NativeA,NativeB) ->
    {module,mutex_lifecycle795}=erlang:load_module(mutex_lifecycle795,BeamA),
    ok=mutex_lifecycle795:load(NativeA),
    Before=mutex_lifecycle795:probe(),
    1=mutex_lifecycle795:marker(),
    {module,mutex_lifecycle795}=erlang:load_module(mutex_lifecycle795,BeamB),
    2=mutex_lifecycle795:marker(),
    ok=mutex_lifecycle795:load(NativeB),
    Upgraded=mutex_lifecycle795:probe(),
    OldPurge=erlang:purge_module(mutex_lifecycle795),
    %% This MUST use the transferred old mutex AFTER source-image purge.
    AfterPurge=mutex_lifecycle795:probe(),
    Destroy=mutex_lifecycle795:destroy(),
    AfterDestroy=mutex_lifecycle795:probe(),
    Delete=erlang:delete_module(mutex_lifecycle795),
    LastPurge=erlang:purge_module(mutex_lifecycle795),
    {Before,Upgraded,OldPurge,AfterPurge,Destroy,AfterDestroy,
        Delete,LastPurge,erlang:module_loaded(mutex_lifecycle795)}.

outcome(Fun) ->
    try Fun() of Value -> {returned,Value}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.

%% Split host-control seam: wait for the REAL retired old image after purge_old,
%% outside runtime gates, BEFORE probe_current. No synthetic phase fence.
open_upgrade(BeamA,BeamB,NativeA,NativeB) ->
    {open_a(BeamA,NativeA),upgrade_b(BeamB,NativeB)}.
open_a(BeamA,NativeA) ->
    {module,mutex_lifecycle795}=erlang:load_module(mutex_lifecycle795,BeamA),
    ok=mutex_lifecycle795:load(NativeA),
    mutex_lifecycle795:probe().
upgrade_b(BeamB,NativeB) ->
    {module,mutex_lifecycle795}=erlang:load_module(mutex_lifecycle795,BeamB),
    ok=mutex_lifecycle795:load(NativeB),
    mutex_lifecycle795:probe().
purge_old() -> erlang:purge_module(mutex_lifecycle795).
probe_current() -> mutex_lifecycle795:probe().
destroy_current() -> mutex_lifecycle795:destroy().
close_current() ->
    {erlang:delete_module(mutex_lifecycle795),erlang:purge_module(mutex_lifecycle795),erlang:module_loaded(mutex_lifecycle795)}.
