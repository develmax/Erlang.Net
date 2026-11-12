-module(boolean_case_lifetime_probe).
-export([f/3]).
identity(V) -> V.
f(Flag, A, B) ->
    Result = case identity(Flag) of
        true -> First = identity(A), Second = identity(First), {yes, Second};
        false -> First = identity(B), Second = identity(First), {no, Second}
    end,
    {Result, identity(A)}.
