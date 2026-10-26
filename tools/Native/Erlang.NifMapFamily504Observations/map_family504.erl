-module(map_family504).
-export([load/1, run/0, shape/2, mutate/4, nonmap_null/1, from_arrays/1, iterator/2,
         pending_value/2, copy_boundary/1, pending_key/0, independent/1, alias_output/3, identity/3, mutable_key/3, replacement_key/4]).
load(Path) -> erlang:load_nif(Path, []).
shape(_, _) -> erlang:nif_error(not_loaded).
mutate(_, _, _, _) -> erlang:nif_error(not_loaded).
nonmap_null(_) -> erlang:nif_error(not_loaded).
from_arrays(_) -> erlang:nif_error(not_loaded).
iterator(_, _) -> erlang:nif_error(not_loaded).
pending_value(_, _) -> erlang:nif_error(not_loaded).
copy_boundary(_) -> erlang:nif_error(not_loaded).
pending_key() -> erlang:nif_error(not_loaded).
independent(_) -> erlang:nif_error(not_loaded).
alias_output(_, _, _) -> erlang:nif_error(not_loaded).
identity(_, _, _) -> erlang:nif_error(not_loaded).
mutable_key(_, _, _) -> erlang:nif_error(not_loaded).
replacement_key(_, _, _, _) -> erlang:nif_error(not_loaded).
run() ->
    Sizes = [0, 1, 2, 31, 32, 33, 65],
    Maps = [{N, maps:from_list([{I, {value, I}} || I <- lists:seq(1, N)])} || N <- Sizes],
    Ref = make_ref(),
    Pid = self(),
    Fun = fun erlang:length/1,
    NonMaps = [a, 1, 1.0, [], [1 | tail], {#{}}, <<>>, <<1:1>>, Ref, Pid, Fun],
    Shapes = [{N, Mode, shape(Mode, Map)} || {N, Map} <- Maps, Mode <- [0, 1, 2]],
    Numeric = #{1 => integer_key, 1.0 => float_key},
    Compound = #{{a, [1 | tail]} => old, <<1:1>> => bit_value},
    Operations = [{N, Mode, Which, mutate(Mode, Map, Which, replacement), Map} ||
        {N, Map} <- Maps, Mode <- [0, 1, 2], Which <- [1, N, N + 1]] ++
        [{numeric, Mode, Key, mutate(Mode, Numeric, Key, replaced), Numeric} || Mode <- [0, 1, 2], Key <- [1, 1.0]] ++
        [{compound, Mode, mutate(Mode, Compound, {a, [1 | tail]}, replaced)} || Mode <- [0, 1, 2]],
    Refusals = [{I, [shape(Mode, Value) || Mode <- [0, 1, 2]],
        [mutate(Mode, Value, key, replacement) || Mode <- [0, 1, 2]], nonmap_null(Value)} ||
        {I, Value} <- lists:zip(lists:seq(1, length(NonMaps)), NonMaps)],
    Arrays = [{N, from_arrays([{I, {value, I}} || I <- lists:seq(1, N)])} || N <- Sizes] ++
        [{numeric, from_arrays([{1, a}, {1.0, b}])},
         {duplicate_atom, from_arrays([{a, first}, {a, second}])},
         {duplicate_compound, from_arrays([{{a, [1 | tail]}, first}, {{a, [1 | tail]}, second}])}],
    Iterators = [{N, Direction, iterator(Direction, Map)} || {N, Map} <- Maps, Direction <- [1, 2, 11, 12, 21, 22]],
    IteratorErrors = [{I, iterator(1, Value)} || {I, Value} <- lists:zip(lists:seq(1, length(NonMaps)), NonMaps)] ++
        [{invalid_direction, Direction, iterator(Direction, #{a => b})} || Direction <- [0, 3]],
    PendingValues = [{N, Mode, pending_value(N, Mode)} || N <- [2, 63, 64, 65, 4096], Mode <- [0, 1, 2]],
    Copies = [{N, copy_boundary(N)} || N <- [63, 64, 65, 4096]],
    MutableKeys = [{N, Bytes, Target, mutable_key(N, Bytes, Target)} ||
        N <- [2, 32, 33, 65], Bytes <- [64, 65],
        Target <- [1, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143]],
    ReplacementKeys = [{N, Bytes, Mode, Same, replacement_key(N, Bytes, Mode, Same)} ||
        N <- [2, 32, 33, 65], Bytes <- [64, 65], Mode <- [0, 1], Same <- [0, 1]],
    {Shapes, Operations, Refusals, Arrays, Iterators, IteratorErrors,
     PendingValues, Copies, pending_key(), independent(#{old => kept}),
     alias_output(#{a => old}, a, new), identity(Ref, Pid, Fun), MutableKeys, ReplacementKeys}.
