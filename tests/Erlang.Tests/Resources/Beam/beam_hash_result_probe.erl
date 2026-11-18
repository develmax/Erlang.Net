-module(beam_hash_result_probe).
-export([bounded/1, dynamic/2, one_arg/1, legacy/1, invalid/1, maximum/1,
         size_auto/1, size_remote/1, shadow/1]).

bounded(Key) -> erlang:phash2(Key, 16) + 1.
dynamic(Key, Range) -> erlang:phash2(Key, Range) + 1.
one_arg(Key) -> erlang:phash2(Key) + 1.
legacy(Key) -> erlang:phash(Key, 16) - 1.
invalid(Key) -> erlang:phash2(Key, 0) + 1.
maximum(Key) -> erlang:phash2(Key, 4294967296) + 1.
size_auto(Data) -> iolist_size(Data) + 1.
size_remote(Data) -> erlang:iolist_size(Data) - 1.
shadow(Key) -> phash2(Key, 16) + 1.
phash2(_, _) -> 0.5.
