-module(beam_eldap_pattern_probe).
-export([is_choice/1, primitive/1, octet/1, class/1, tail/1]).
is_choice({ref, Name}) -> is_choice(schema(Name));
is_choice({choice, _, _}) -> true;
is_choice(_) -> false.
schema(choice) -> {choice, true, []};
schema(_) -> {integer, 2}.
primitive(Value) when is_binary(Value) -> Value.
octet(<<Byte>>) -> Byte.
class(<<Class:2, _:1, _:5>>) -> Class.
tail(<<_:8, Tail/binary>>) -> Tail.
