-module(beam_integer_conversion_probe).
-export([from_list/1, from_binary/1, from_hex_binary/1, shadow/1]).
-compile({no_auto_import,[binary_to_integer/1]}).

from_list(Digits) -> list_to_integer(Digits) + 1.
from_binary(Digits) -> erlang:binary_to_integer(Digits) - 1.
from_hex_binary(Digits) -> erlang:binary_to_integer(Digits, 16) + 1.
shadow(Digits) -> binary_to_integer(Digits) + 1.
binary_to_integer(_) -> 0.5.
