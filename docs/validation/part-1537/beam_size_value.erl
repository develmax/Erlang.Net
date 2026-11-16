-module(beam_size_value).
-export([value/1, computed/1, shadow/1, byte_size/1]).
-compile({no_auto_import,[byte_size/1]}).

value(X) -> {size, erlang:byte_size(X)}.
computed(X) -> {size, bit_size(erlang:term_to_binary(X))}.
shadow(X) -> {local, byte_size(X)}.
byte_size(X) -> {shadow, X}.
