-module(list_head_arity_probe).
-export([entry/1, wildcard/1]).

entry([separator|Tail]) -> reply(separator,Tail);
entry([{Name,_N,cascade,Items}|Tail]) -> reply(cascade,Name,Items,Tail);
entry([{Name,Position}|Tail]) -> reply(plain,Name,Position,Tail);
entry([{Name,N,check}|Tail]) when is_integer(N) -> reply(checked,Name,N,Tail);
entry([{Name,N,check}|Tail]) -> reply(check_fallback,Name,N,Tail);
entry([{Name,N,radio}|Tail]) -> reply(radio,Name,N,Tail);
entry([]) -> empty.

wildcard([separator|Tail]) -> reply(separator,Tail);
wildcard([Head|Tail]) when is_atom(Head) -> reply(Head,Tail);
wildcard([{A,B,C}|_]) -> reply(A,B,C);
wildcard([{A,B}|_]) -> reply(A,B);
wildcard([]) -> empty.

reply(A,B) -> {A,B}.
reply(A,B,C) -> {A,B,C}.
reply(A,B,C,D) -> {A,B,C,D}.
