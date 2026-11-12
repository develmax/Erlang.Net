-module(entry_list_heap_probe).
-export([f/2]).
take(X) -> X.
f(Input, Retained) ->
    Value = take([Input, fixed, 42]),
    {Value, Retained, take(Retained)}.
