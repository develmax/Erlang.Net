-module(beam_shared_local_tails).
-export([run/1, answer/0]).
run({tag, z}) -> String = lists:reverse("46"), erlang:list_to_integer(String);
run({tag, e}) -> 64;
run({tag, d}) -> 32;
run({tag, a}) -> answer();
run({tag, b}) -> answer();
run({tag, c}) -> answer();
run(Value) -> Value.
answer() -> chosen.
