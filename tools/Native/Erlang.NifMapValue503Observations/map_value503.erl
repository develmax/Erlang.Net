-module(map_value503).
-export([load/1, query/3, independent/2, pending_binary_key/1, run/0]).
load(Path) -> erlang:load_nif(Path, []).
query(_, _, _) -> erlang:nif_error(not_loaded).
independent(_, _) -> erlang:nif_error(not_loaded).
pending_binary_key(_) -> erlang:nif_error(not_loaded).
run() ->
    Numeric = #{1 => integer_key, 1.0 => float_key},
    Compound = #{{a, [1 | tail]} => #{answer => <<1, 2>>}, <<1:1>> => bits_value},
    Large = maps:from_list([{I, {value, I}} || I <- lists:seq(1, 65)]),
    Ref = make_ref(),
    Pid = self(),
    Fun = fun erlang:length/1,
    Identity = #{Ref => reference_value, Pid => pid_value, Fun => function_value},
    Cases = [
        {empty_missing, #{}, absent},
        {atom_present, #{key => [nested, #{a => 1}]}, key},
        {atom_missing, #{key => 1}, absent},
        {integer_exact, Numeric, 1},
        {float_exact, Numeric, 1.0},
        {compound_present, Compound, {a, [1 | tail]}},
        {bitstring_present, Compound, <<1:1>>},
        {large_first, Large, 1},
        {large_last, Large, 65},
        {large_missing, Large, 66},
        {reference_present, Identity, Ref},
        {pid_present, Identity, Pid},
        {function_present, Identity, Fun}
    ],
    Ordinary = [{Name, query(0, Map, Key)} || {Name, Map, Key} <- Cases],
    Hints = [{Mode, query(Mode, #{a => #{b => [1, 2]}}, a)} || Mode <- [1, 2]],
    NonMaps = [a, 1, 1.0, [], [1 | tail], {#{}}, <<>>, <<1:1>>, Ref, Pid, Fun],
    Refusals = [{I, query(0, Value, key), query(3, Value, key)} || {I, Value} <- lists:zip(lists:seq(1, length(NonMaps)), NonMaps)],
    NullMissing = query(3, #{a => 1}, absent),
    Lifetime = independent(#{key => {nested, #{a => [1, 2]}}}, key),
    Pending = pending_binary_key(#{<<1, 2>> => first_value, <<3, 4>> => second_value}),
    {Ordinary, Hints, Refusals, NullMissing, Lifetime, Pending}.
