-module(beam_deep_subject388).
-export([worker/2, configuration/1, probe/2, held/2, held_probe/3]).

configuration(success8192) -> {8192, success};
configuration(success16384) -> {16384, success};
configuration(throw16384) -> {16384, throw};
configuration(error16384) -> {16384, error};
configuration(nested16384) -> {16384, nested};
configuration(alternating16384) -> {16384, alternating}.

worker(Owner, Case) ->
    {Depth, Mode} = configuration(Case),
    Outcome = try dispatch(Depth, Mode) of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end,
    Owner ! {Case, Outcome},
    ok.

dispatch(Depth, nested) -> nested(Depth);
dispatch(Depth, alternating) -> alternate_a(Depth);
dispatch(Depth, Mode) -> descend(Depth, Mode).

descend(0, Mode) -> leaf(Mode);
descend(Depth, Mode) ->
    Value = descend(Depth - 1, Mode),
    Value + 1.

leaf(success) -> 0;
leaf(throw) -> erlang:throw({deep_throw, [first, second]});
leaf(error) -> erlang:error({deep_error, #{detail => preserved}}).

nested(Depth) ->
    try
        Inner = try descend(Depth, throw)
        catch throw:Reason:Stack -> {inner_caught, Reason, Stack}
        end,
        erlang:error({after_deep_inner, Inner})
    catch
        OuterClass:OuterReason:OuterStack -> {outer_caught, OuterClass, OuterReason, OuterStack}
    end.

alternate_a(0) -> erlang:error({alternating_error, 388});
alternate_a(Depth) ->
    Value = alternate_b(Depth - 1),
    Value + 1.

alternate_b(0) -> erlang:error({alternating_error, 388});
alternate_b(Depth) ->
    Value = alternate_a(Depth - 1),
    Value + 1.

probe(Depth, Callback) -> probe_descend(Depth, Callback).

probe_descend(0, Callback) ->
    Callback(),
    0;
probe_descend(Depth, Callback) ->
    Value = probe_descend(Depth - 1, Callback),
    Value + 1.

held(Owner, Depth) ->
    try held_descend(Depth, Owner, Depth)
    catch Class:Reason:Stack ->
        Owner ! {held_guest_catch, Class, Reason, Stack},
        held_guest_caught
    end.

held_descend(0, Owner, OriginalDepth) ->
    Owner ! {deep_held, self(), OriginalDepth},
    receive release -> 0 end;
held_descend(Depth, Owner, OriginalDepth) ->
    Value = held_descend(Depth - 1, Owner, OriginalDepth),
    Value + 1.

held_probe(Owner, Depth, Callback) ->
    try held_probe_descend(Depth, Owner, Depth, Callback)
    catch Class:Reason:Stack ->
        Owner ! {held_guest_catch, Class, Reason, Stack},
        held_guest_caught
    end.

held_probe_descend(0, Owner, OriginalDepth, Callback) ->
    Callback(),
    Owner ! {deep_held, self(), OriginalDepth},
    receive release -> 0 end;
held_probe_descend(Depth, Owner, OriginalDepth, Callback) ->
    Value = held_probe_descend(Depth - 1, Owner, OriginalDepth, Callback),
    Value + 1.
