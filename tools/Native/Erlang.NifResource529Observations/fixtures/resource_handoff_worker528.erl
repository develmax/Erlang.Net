-module(resource_handoff_worker528).
-export([hold/2]).

hold(Parent, Ref) ->
    Resource = native_resource_handoff528:make528(),
    Parent ! {Ref, ready, native_resource_handoff528:probe528(Resource)},
    receive {Ref, inspect} ->
        Parent ! {Ref, inspected, native_resource_handoff528:probe528(Resource)}
    after 10000 -> error(no_inspection528)
    end,
    receive {Ref, release} ->
        %% Preserve a real use until release without requiring old/new type equality.
        Parent ! {Ref, final_probe, native_resource_handoff528:probe528(Resource)},
        ok
    after 10000 -> error(no_release528)
    end.
