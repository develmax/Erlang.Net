-module(numeric372_nested_big_followup).
-export([cases/0,probe/1]).
cases() -> [nested_at_small_limit,nested_after_small_limit,binary_after_small_limit,binary_tail_after_small_limit,wide_after_small_limit,improper_after_small_limit,hex_nested_at_limit,hex_nested_after_limit,zero_prefix_nested_before_limit,zero_prefix_nested_after_limit].
probe(nested_at_small_limit) -> erlang:list_to_integer(lists:duplicate(17,$1)++[[$2]]);
probe(nested_after_small_limit) -> erlang:list_to_integer(lists:duplicate(18,$1)++[[$2]]);
probe(binary_after_small_limit) -> erlang:list_to_integer(lists:duplicate(18,$1)++[<<$2>>]);
probe(binary_tail_after_small_limit) -> erlang:list_to_integer(lists:duplicate(18,$1)++ <<$2>>);
probe(wide_after_small_limit) -> erlang:list_to_integer(lists:duplicate(18,$1)++[305]);
probe(improper_after_small_limit) -> erlang:list_to_integer(lists:duplicate(18,$1)++tail);
probe(hex_nested_at_limit) -> erlang:list_to_integer(lists:duplicate(14,$A)++[[$B]],16);
probe(hex_nested_after_limit) -> erlang:list_to_integer(lists:duplicate(15,$A)++[[$B]],16);
probe(zero_prefix_nested_before_limit) -> erlang:list_to_integer(lists:duplicate(20,$0)++lists:duplicate(17,$1)++[[$2]]);
probe(zero_prefix_nested_after_limit) -> erlang:list_to_integer(lists:duplicate(20,$0)++lists:duplicate(18,$1)++[[$2]]).
