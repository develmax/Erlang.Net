-module(load_private_observer557).
-export([run/2, worker/1, observe/2]).

%% Actual admitted BEAM bytes and DLL path enter from the existing development
%% native loader driver. Only erlang static imports; no code_server/file boot.
run(Beam, NativePath) ->
    {module, load_private557} = erlang:load_module(load_private557, Beam),
    Loaded = load_private557:load(NativePath),
    case Loaded of
        ok -> {returned557, load_private557:read()};
        _ -> {load_failed557, Loaded}
    end.

worker({Parent, Reference, Beam, NativePath}) ->
    Value = try run(Beam, NativePath) of
        Result -> Result
    catch Class:Reason:Stack ->
        {raised557, Class, Reason, Stack}
    end,
    Parent ! {Reference, Value}.

observe(Path, probe) ->
    case load_private557:load(Path) of
        ok -> {returned557, load_private557:read()};
        Other -> {load_failed557, Other}
    end.
