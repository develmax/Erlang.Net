-module(entry_case_sequence_probe).
-export([f/3, g/3]).
selector(V) -> V.
f(Selector, Expected, Retained) ->
    Selected = case selector(Selector) of
        Expected -> matched;
        Other -> {other, Other}
    end,
    {Selected, Retained}.
g(Selector, Expected, Retained) ->
    case selector(Selector) of
        Expected -> BranchValue = matched;
        Other -> BranchValue = {other, Other}
    end,
    {BranchValue, Retained}.
