-module(resource_takeover_observer529).
-export([run/6]).

run(Partition, FirstLibrary, NextBeam, SecondLibrary, FirstMarker, SecondMarker) ->
    try acquire_handoff_corrected529:run(Partition, FirstLibrary, NextBeam,
            SecondLibrary, FirstMarker, SecondMarker) of
        Value -> {returned529, Value}
    catch Class:Reason:Stack ->
        {raised529, Class, Reason, Stack}
    end.
