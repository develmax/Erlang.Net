-module(chain411_a).
-on_load(initialize/0).
-export([run/0]).
initialize() -> b_ready = chain411_ordinary:require_b(), ok.
run() -> a_ready.
