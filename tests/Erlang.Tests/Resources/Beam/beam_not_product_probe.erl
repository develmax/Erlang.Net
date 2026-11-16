-module(beam_not_product_probe).
-export([neg/1, product/2, guard/1, field/2]).
neg(X) -> not X.
product(A,B) -> A*B.
guard(X) when not X -> yes;
guard(_) -> no.

field(N,T) when element(N,T) =:= ok -> yes;
field(_,_) -> no.
