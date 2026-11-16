-module(beam_probe).
-export([reply/0, accept/1]).
reply() -> get(reply).
accept(yes) -> ok;
accept(Value) -> Value.
