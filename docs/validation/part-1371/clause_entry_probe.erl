-module(clause_entry_probe).
-export([dispatch/2, retain/2, conditional/2, test_last/2, only/2]).

dispatch(param, Value) ->
    pass(Value);
dispatch(Tag, _) ->
    exit({error, {asn1, {undefined_type, Tag}}}).

retain(ok, Value) ->
    pair({tag, Value}, Value);
retain(Tag, _) ->
    pair({unknown, Tag}, Tag).

conditional(plain, Value) ->
    pass(Value);
conditional(compute, Value) ->
    pass(probe(Value));
conditional(Tag, _) ->
    exit({unknown, Tag}).

test_last(Value, run) ->
    lists:reverse(Value);
test_last(_, Tag) ->
    throw({bad, Tag}).

only(one, Value) ->
    pass(Value);
only(two, Value) ->
    pair(two, Value);
only(three, Value) ->
    pair(three, Value).

pass(Value) ->
    {passed, Value}.

pair(A, B) ->
    {A, B}.

probe(Value) ->
    put(trace, Value),
    {probed, Value}.
