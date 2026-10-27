-module(map_routes517).
-export([run/0, run/1, hold/1, hold_value/2, echo/1]).

run() -> run(517).

run(Token) -> run_inner(Token).
run_inner(Token) -> M = make(Token), {M, M, flat(Token), raw_probe(Token)}.

raw_bundle(Token) ->
    Map = fill526(65, #{}, Token),
    [Raw | _] = erts_internal:map_hashmap_children(Map),
    {Map, Raw}.

fill526(0, Map, _Token) -> Map;
fill526(N, Map, Token) -> fill526(N-1, Map#{N => {Token, [N], {}}}, Token).

raw_probe(Token) ->
    {Map, Raw} = raw_bundle(Token),
    [Again | _] = erts_internal:map_hashmap_children(Map),
    {erts_internal:term_type(Raw), erts_debug:same(Raw, Again),
     erts_debug:flat_size(Map), erts_debug:size_shared(Map),
     erts_debug:flat_size({Map, Raw}), erts_debug:size_shared({Map, Raw}),
     erts_debug:flat_size({Raw, Map}), erts_debug:size_shared({Raw, Map})}.

flat(Token) ->
    Map = #{1 => {Token}, 2 => Token},
    Keys = erts_internal:map_to_tuple_keys(Map),
    {Map, Keys, erts_internal:map_to_tuple_keys(Map)}.

echo(Value) -> echo_inner(Value).
echo_inner(Value) -> Value.

hold(Token) -> hold_inner(Token, make(Token)).
hold_value(Token, Value) -> hold_inner(Token, Value).

hold_inner(Token, Map) ->
    put(map_value517, Map),
    put(map_alias517, {Map, Map}),
    put(map_flat517, flat(Token)),
    put(map_raw526, raw_bundle(Token)),
    put(map_sizes526, raw_probe(Token)),
    put(map_phase517, Token),
    receive
        {release517, Token} ->
            erase(map_phase517),
            erase(map_alias517),
            erase(map_flat517),
            erase(map_raw526),
            erase(map_sizes526),
            erase(map_value517),
            {Map, Map}
    end.

make(Token) -> B = base(), B#{token517 => Token}.
base() ->
    #{0 => {0, [0], {}},
       1 => {1, [1], {}},
       2 => {2, [2], {}},
       3 => {3, [3], {}},
       4 => {4, [4], {}},
       5 => {5, [5], {}},
       6 => {6, [6], {}},
       7 => {7, [7], {}},
       8 => {8, [8], {}},
       9 => {9, [9], {}},
       10 => {10, [10], {}},
       11 => {11, [11], {}},
       12 => {12, [12], {}},
       13 => {13, [13], {}},
       14 => {14, [14], {}},
       15 => {15, [15], {}},
       16 => {16, [16], {}},
       17 => {17, [17], {}},
       18 => {18, [18], {}},
       19 => {19, [19], {}},
       20 => {20, [20], {}},
       21 => {21, [21], {}},
       22 => {22, [22], {}},
       23 => {23, [23], {}},
       24 => {24, [24], {}},
       25 => {25, [25], {}},
       26 => {26, [26], {}},
       27 => {27, [27], {}},
       28 => {28, [28], {}},
       29 => {29, [29], {}},
       30 => {30, [30], {}},
       31 => {31, [31], {}},
       32 => {32, [32], {}},
       33 => {33, [33], {}},
       34 => {34, [34], {}},
       35 => {35, [35], {}},
       36 => {36, [36], {}},
       37 => {37, [37], {}},
       38 => {38, [38], {}},
       39 => {39, [39], {}},
       40 => {40, [40], {}},
       41 => {41, [41], {}},
       42 => {42, [42], {}},
       43 => {43, [43], {}},
       44 => {44, [44], {}},
       45 => {45, [45], {}},
       46 => {46, [46], {}},
       47 => {47, [47], {}},
       48 => {48, [48], {}},
       49 => {49, [49], {}},
       50 => {50, [50], {}},
       51 => {51, [51], {}},
       52 => {52, [52], {}},
       53 => {53, [53], {}},
       54 => {54, [54], {}},
       55 => {55, [55], {}},
       56 => {56, [56], {}},
       57 => {57, [57], {}},
       58 => {58, [58], {}},
       59 => {59, [59], {}},
       60 => {60, [60], {}},
       61 => {61, [61], {}},
       62 => {62, [62], {}},
       63 => {63, [63], {}},
       64 => {64, [64], {}}}.
