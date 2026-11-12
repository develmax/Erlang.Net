-module(beam_on_load_marker).
-on_load(init/0).
-export([run/0]).
init() -> ok.
run() -> ready.
