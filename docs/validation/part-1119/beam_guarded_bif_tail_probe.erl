-module(beam_guarded_bif_tail_probe).
-export([run/2, arithmetic/2]).
run(A,B) when is_float(A) -> run(erlang:floor(A), B);
run(A,B) when is_float(B) -> run(A, erlang:floor(B));
run(A,B) -> {A,B}.
arithmetic(A,B) when A >= 0, B > 0 -> A div B;
arithmetic(A,B) when A < 0, B < 0 -> A rem B;
arithmetic(A,B) -> {A,B}.

