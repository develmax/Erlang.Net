-module(chain411_failure).
-on_load(initialize/0).
-export([run/0]).
initialize() -> error(callback_failed).
run() -> must_not_publish.
