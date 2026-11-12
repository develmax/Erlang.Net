-module(beam_frame_probe).
-export([keep/2, mixed/2, literal/0]).
keep(A,B) ->
    target(B,A),
    A.
mixed(A,B) -> target(ok,A).
literal() -> target(42, ok).
target(A,B) -> A.
