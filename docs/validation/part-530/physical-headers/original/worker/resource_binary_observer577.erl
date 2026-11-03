-module(resource_binary_observer577).
-export([run/0, worker/2]).

%% Additive job only: existing family527 source/build and prior golden remain.
%% Run after that original job's genuine NIF load, in the same admitted worker.
run() ->
    Parent = self(), Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(?MODULE, worker, [Parent, Token]),
    {Value, Down} = try
        Value0 = receive {Token, Receipt} -> Receipt
            after 10000 -> error(missing_binary_receipt577) end,
        Down0 = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason
            after 10000 -> error(missing_binary_down577) end,
        {Value0, Down0}
    catch Class:Failure:Stack ->
        exit(Pid, kill),
        receive {'DOWN', Monitor, process, Pid, _} -> ok after 10000 -> ok end,
        demonitor(Monitor, [flush]),
        erlang:raise(Class, Failure, Stack)
    end,
    case {Value, Down} of
        {{returned, [{resource_is_reference, true}, {resource_still_live, _},
                     {before_gc, Sizes}, {after_gc, Sizes}]}, normal} ->
            {Value, Down};
        {{failed, FailureClass, FailureReason, FailureStack}, normal} ->
            erlang:raise(FailureClass, FailureReason, FailureStack);
        _ ->
            error({invalid_binary_witness577, Value, Down})
    end.

worker(Parent, Token) ->
    Receipt = try
        {Resource, Binary, Independent, Copy} = native_resource_family527:binary_aliases577(),
        A = binary:part(Binary, 1, 65),
        B = binary:part(Binary, 2, 65),
        Before = report(Binary, A, B, Copy, Independent),
        true = erlang:garbage_collect(),
        After = report(Binary, A, B, Copy, Independent),
        {returned, [
            {resource_is_reference, is_reference(Resource)},
            {resource_still_live, native_resource_family527:inspect527(Resource)},
            {before_gc, Before},
            {after_gc, After}
        ]}
    catch Class:Reason:Stack ->
        {failed, Class, Reason, Stack}
    end,
    Parent ! {Token, Receipt},
    ok.

report(Binary, A, B, Copy, Independent) ->
    [
        {bytes, {byte_size(Binary), byte_size(A), byte_size(B), byte_size(Copy)}},
        {prefixes, {binary:part(Binary, 0, 4), binary:part(A, 0, 4), binary:part(B, 0, 4)}},
        {referenced_bytes, {binary:referenced_byte_size(Binary), binary:referenced_byte_size(A), binary:referenced_byte_size(B)}},
        {single, sizes577(Binary)},
        {aliased_pair, sizes577({Binary, Binary})},
        {different_slices, sizes577({A, B})},
        {source_and_slice, sizes577({Binary, A})},
        {copied, sizes577(Copy)},
        {independent, {sizes577(Independent), erts_debug:same(Binary, Independent), Binary =:= Independent}},
        {same, {erts_debug:same(Binary, Binary), erts_debug:same(Binary, Copy), erts_debug:same(A, B)}},
        {content_equal, Binary =:= Copy}
    ].

sizes577(Value) ->
    {erts_debug:size(Value), erts_debug:size_shared(Value), erts_debug:flat_size(Value)}.
