-module(beam_list_live_inputs).
-export([a/2,b/2,c/2,e/2]).
a(A,B) -> [tag(A),tag(B)].
b(A,B) -> [tag(A),tag(B),tag(3)].
c(A,B) -> [tag(A),tag(B),tag(B)].
e(A,B) -> [tag(A),tag(A),tag(B)].
tag(X) -> {tag,X}.
