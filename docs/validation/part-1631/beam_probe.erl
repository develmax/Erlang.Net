-module(beam_probe).
-export([setup/1]).
setup(Input) -> put(setup, Input), ok.
