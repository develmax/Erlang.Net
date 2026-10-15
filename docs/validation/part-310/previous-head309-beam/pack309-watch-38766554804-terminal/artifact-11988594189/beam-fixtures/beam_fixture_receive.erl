-module(beam_fixture_receive).
-export([run/0]).
run() -> self() ! ping, receive ping -> {ok,ping} after 1000 -> timeout end.
