-module(beam_live_apply).
-export([run/3]).
run(_, Callback, _) -> Callback().
