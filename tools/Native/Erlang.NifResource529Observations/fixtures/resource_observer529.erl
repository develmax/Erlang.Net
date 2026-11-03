-module(resource_observer529).
-export([run/5]).

run(Kind, First, Second, FreshBeam, MarkerBase) ->
    try
        Value = case Kind of
            family527 ->
                ok = native_resource_family527:load(First, 0),
                {native_resource_family527:run(), magic_alias_observer576:run(), resource_binary_observer577:run()};
            handoff528 ->
                acquire_handoff528:run(First, FreshBeam, Second,
                    MarkerBase ++ "-first", MarkerBase ++ "-second")
        end,
        {returned529, Value}
    catch Class:Reason:Stack ->
        {raised529, Class, Reason, Stack}
    end.
