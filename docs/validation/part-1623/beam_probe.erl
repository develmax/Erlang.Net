-module(beam_probe).
-export([change/2]).
change(Id,Items) -> put(trace,Id), [Id|Items].
