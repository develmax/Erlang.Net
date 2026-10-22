-module(chain411_b).
-on_load(initialize/0).
-export([run/0]).
initialize() -> ready = chain411_ordinary:ready(), ok.
run() -> b_ready.
