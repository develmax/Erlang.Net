-module(beam_unary_bifs).
-export([run/3]).
run(A,B,C) ->
    {hd(mark(head,A)),tl(mark(tail,A)),node(mark(node,B)),tuple_size(mark(size,C))}.
mark(Tag,Value) -> put(last,Tag), Value.
