-module(beam_literal_tuple_closures).
-export([pick/1, strict/1]).
pick({2,16,5}) -> fun(X) -> {five,X} end;
pick({2,16,9}) -> fun(X) -> {nine,X} end;
pick({2,16,1}) -> fun(X) -> {one,X} end;
pick(_) -> fun(X) -> {fallback,X} end.
strict({2,16,5}) -> fun() -> five end;
strict({2,16,9}) -> fun() -> nine end.
