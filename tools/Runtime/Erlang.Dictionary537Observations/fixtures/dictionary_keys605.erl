-module(dictionary_keys605).
-export([run/0]).

run() ->
    Saved = get(),
    try
        erase(),
        A = atomics:new(1, []),
        undefined = put(stored, A),
        erlang:garbage_collect(),
        [stored] = get_keys(),
        [stored] = get_keys(get(stored)),
        [] = get_keys(not_the_value),
        A = erase(stored),
        atomics:put(A, 1, 60591),
        60591 = atomics:get(A, 1),
        [] = get_keys(undefined),
        [] = get_keys(),
        [{scalar_key_native_value, 2}, {erased_value_still_live, 60591},
         {exact_integer_comparison, exact_integer_case()}]
    after
        erase(),
        lists:foreach(fun({K,V}) -> put(K,V) end, Saved)
    end.

exact_integer_case() ->
    undefined = put(integer_value, 1),
    undefined = put(float_value, 1.0),
    [integer_value] = get_keys(1),
    [float_value] = get_keys(1.0),
    erase(integer_value),
    erase(float_value),
    true.
