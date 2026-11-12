-module(beam_build_definitions).
-export([run/0]).
-ifdef(BUILD_VERSION).
run() -> {?BUILD_VERSION, ?mode, ?COUNT}.
-else.
run() -> missing.
-endif.
