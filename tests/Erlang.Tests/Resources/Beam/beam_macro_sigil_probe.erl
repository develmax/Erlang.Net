-module(beam_macro_sigil_probe).
-export([run/0]).
-define(BINARY, ~b"abc").
-define(TEXT, ~s"abc").
run() -> {?BINARY, ?TEXT}.
