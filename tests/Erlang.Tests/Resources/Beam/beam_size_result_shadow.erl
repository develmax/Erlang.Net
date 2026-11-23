-module(beam_size_result_shadow).
-export([local/1, remote/1, length/1]).
-compile({no_auto_import, [length/1]}).

local(Value) -> length(Value) + 1.
remote(Value) -> beam_size_result_opaque:byte_size(Value) + 1.
length(_) -> 2.5.
