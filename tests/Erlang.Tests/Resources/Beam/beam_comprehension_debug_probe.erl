-module(beam_comprehension_debug_probe).
-export([rows/1, zipped/2, bits/1, packed/1, mapped/1]).
rows(Rows) -> [{X,Y} || #{x := X, ys := Ys} <- Rows, Y <- Ys, Y > X].
zipped(A,B) -> {[X || {ok,X} <- A && X <:- B], [{X,Y} || {ok,X} <- A && Y <- B, Y > 0]}.
bits(B) -> [X || <<1:1,X:2>> <:= B].
packed(Items) -> << Part || Part <- Items >>.
mapped(M) -> #{wanted => V || wanted := V <:- M}.
