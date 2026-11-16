-module(beam_body_fallback_probe).
-export([size/1, head/1, bytes/1, owner/1, number/1, absval/1]).
size(X) -> erlang:tuple_size(X).
head(X) -> erlang:hd(X).
bytes(X) -> erlang:byte_size(X).
owner(X) -> erlang:node(X).
number(X) -> erlang:is_number(X).
absval(X) -> erlang:abs(X).
