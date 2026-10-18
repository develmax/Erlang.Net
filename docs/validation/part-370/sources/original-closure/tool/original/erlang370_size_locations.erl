-module(erlang370_size_locations).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [renamed_tuple_error,renamed_bits_error,renamed_bytes_error,renamed_map_error,tuple_guard_failure,bits_guard_failure,bytes_guard_failure,map_guard_failure].
run() -> erlang370_pure_support:run(erlang370_size_locations,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang370_size_locations,cases()).
probe(renamed_tuple_error) -> tuple_context(invalid);
probe(renamed_bits_error) -> bits_context(invalid);
probe(renamed_bytes_error) -> bytes_context(invalid);
probe(renamed_map_error) -> map_context(invalid);
probe(tuple_guard_failure) -> guard_tuple(invalid);
probe(bits_guard_failure) -> guard_bits(invalid);
probe(bytes_guard_failure) -> guard_bytes(invalid);
probe(map_guard_failure) -> guard_map(invalid).
tuple_context(Value) -> Size=erlang:tuple_size(Value), {returned,Size}.
bits_context(Value) -> Size=erlang:bit_size(Value), {returned,Size}.
bytes_context(Value) -> Size=erlang:byte_size(Value), {returned,Size}.
map_context(Value) -> Size=erlang:map_size(Value), {returned,Size}.
guard_tuple(Value) when erlang:tuple_size(Value) >= 0 -> true;
guard_tuple(_) -> false.
guard_bits(Value) when erlang:bit_size(Value) >= 0 -> true;
guard_bits(_) -> false.
guard_bytes(Value) when erlang:byte_size(Value) >= 0 -> true;
guard_bytes(_) -> false.
guard_map(Value) when erlang:map_size(Value) >= 0 -> true;
guard_map(_) -> false.

