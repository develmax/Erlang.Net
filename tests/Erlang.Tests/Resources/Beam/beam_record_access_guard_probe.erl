-module(beam_record_access_guard_probe).
-export([boolean/1, exact/1]).
-record(row, {flag}).
boolean(R) when R#row.flag -> accepted;
boolean(_) -> rejected.
exact(R) when R#row.flag =:= ok -> accepted;
exact(_) -> rejected.
