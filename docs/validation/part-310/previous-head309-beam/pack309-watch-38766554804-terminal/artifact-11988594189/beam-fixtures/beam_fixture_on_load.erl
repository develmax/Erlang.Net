-module(beam_fixture_on_load).
-on_load(init/0).
-export([run/0]).
init() -> put(beam_fixture_marker,loaded), ok.
run() -> {ok,on_load_fixture}.
