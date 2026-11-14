-module(beam_structural_capture_order).
-export([callback/1]).
callback([{entry, {Left, Right}} | _]) -> fun() -> {Left, Right} end;
callback([Head | Tail]) -> {Head, callback(Tail)}.
