-module(beam_record_guard_probe).
-export([guard/1, value/1, nested/1]).
-record(probe, {value}).
guard(X) when is_record(X, probe) -> yes;
guard(_) -> no.
value(X) -> is_record(X,probe,2).
nested(X) when is_record(X,probe) orelse is_integer(X) -> yes;
nested(_) -> no.
