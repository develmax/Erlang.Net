-module(beam_move_five_probe).
-export([a/0,b/0,target/5]).
a() -> other:call(1,2,3,4,5).
b() -> target(1,2,3,4,5).
target(A,_,_,_,_) -> A.
