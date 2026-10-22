-module(beam_debug_sharing425).
-compile({no_inline, [dynamic_tuple/1]}).
-export([run/0, run_stacks/0, worker/2, sharing/0, layouts/0, literal/0, closure/0, capture/1]).

scenarios() ->
    [atom, nil, small, small_max, small_min, big_positive, big_negative,
     float, empty_tuple, literal_tuple, runtime_tuple, literal_list,
     runtime_list, improper_list, literal_binary, runtime_binary,
     large_binary, bits, external_fun, literal_map, runtime_map,
     nested_literal, nested_runtime, shared_children, distinct_children].

cases() ->
    [{Operation, Scenario} || Operation <- [flat, shared, copy_false, copy_true], Scenario <- scenarios()] ++
    [{bad_flag, Flag} || Flag <- [atom, integer, tuple, list]].

run() -> [{Name, observe(Name)} || Name <- cases()].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try [{default, Old, run()} | [depth(D) || D <- [0, 1, 2, 8]]]
    after erlang:system_flag(backtrace_depth, Old) end.

depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    Effective = erlang:system_flag(backtrace_depth, Depth),
    {Depth, Effective, run()}.

observe(Name) ->
    {Pid, Ref} = spawn_monitor(?MODULE, worker, [self(), Name]),
    receive
        {Name, Value} -> receive {'DOWN', Ref, process, Pid, normal} -> Value end
    after 15000 ->
        exit(Pid, kill),
        receive {'DOWN', Ref, process, Pid, _} -> ok end,
        exit({worker_timeout, Name})
    end.

worker(Parent, Name) ->
    Outcome = try {value, perform(Name)} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end,
    Parent ! {Name, Outcome}.

perform({bad_flag, Name}) -> erts_debug:copy_shared({runtime, 42}, bad_flag(Name));
perform({Operation, Name}) ->
    Term = term(Name),
    case Operation of
        flat -> erts_debug:flat_size(Term);
        shared -> erts_debug:size_shared(Term);
        copy_false -> erts_debug:copy_shared(Term, false);
        copy_true -> erts_debug:copy_shared(Term, true)
    end.

bad_flag(atom) -> wrong;
bad_flag(integer) -> 1;
bad_flag(tuple) -> {false};
bad_flag(list) -> [true].

term(atom) -> hello;
term(nil) -> [];
term(small) -> 42;
term(small_max) -> 576460752303423487;
term(small_min) -> -576460752303423488;
term(big_positive) -> binary_to_term(term_to_binary(1 bsl 130));
term(big_negative) -> binary_to_term(term_to_binary(-(1 bsl 130)));
term(float) -> binary_to_term(term_to_binary(1.25));
term(empty_tuple) -> {};
term(literal_tuple) -> {a, b};
term(runtime_tuple) -> dynamic_tuple({a, b});
term(literal_list) -> [a, b];
term(runtime_list) -> binary_to_term(term_to_binary([a, b]));
term(improper_list) -> binary_to_term(term_to_binary([a, b | tail]));
term(literal_binary) -> <<42, 43>>;
term(runtime_binary) -> binary:copy(<<42>>, 3);
term(large_binary) -> binary:copy(<<42>>, 128);
term(bits) -> binary_to_term(term_to_binary(<<17:5, 3:2>>));
term(external_fun) -> fun erlang:self/0;
term(literal_map) -> #{a => 1, b => 2};
term(runtime_map) -> binary_to_term(term_to_binary(#{a => 1, b => 2}));
term(nested_literal) -> {nested, [a, b], #{x => y}};
term(nested_runtime) -> binary_to_term(term_to_binary({nested, [a, b], #{x => y}}));
term(shared_children) -> Child = dynamic_tuple({42, 43}), {Child, Child};
term(distinct_children) -> {dynamic_tuple({42, 43}), dynamic_tuple({42, 43})}.

%% Real runtime allocation: preserve literal scenarios separately above.
dynamic_tuple(Value) -> binary_to_term(term_to_binary(Value)).

sharing() ->
    Child = dynamic_tuple({42, 43}),
    Original = {Child, Child},
    FalseCopy = erts_debug:copy_shared(Original, false),
    TrueCopy = erts_debug:copy_shared(Original, true),
    Literal = {a, [b, c]},
    LiteralFalse = erts_debug:copy_shared(Literal, false),
    LiteralTrue = erts_debug:copy_shared(Literal, true),
    Ref = make_ref(),
    RefCopy = erts_debug:copy_shared({Ref, Ref}, true),
    [{dynamic, Original =:= FalseCopy, erts_debug:same(Original, FalseCopy),
      erts_debug:same(element(1, FalseCopy), element(2, FalseCopy)),
      erts_debug:same(Child, element(1, FalseCopy))},
     {true_copy, Original =:= TrueCopy, erts_debug:same(Original, TrueCopy),
      erts_debug:same(element(1, TrueCopy), element(2, TrueCopy))},
     {literals, erts_debug:same(Literal, LiteralFalse), erts_debug:same(Literal, LiteralTrue),
      Literal =:= LiteralFalse, Literal =:= LiteralTrue},
     {references, element(1, RefCopy) =:= Ref, erts_debug:same(element(1, RefCopy), element(2, RefCopy)),
      erts_debug:same(Ref, element(1, RefCopy))},
     {sizes, erts_debug:flat_size(Original), erts_debug:size_shared(Original),
      erts_debug:flat_size(FalseCopy), erts_debug:size_shared(FalseCopy)}].

layouts() ->
    %% Preserve raw physical/layout observations separately from the portable
    %% immutable Term graph protocol. No physical numbers are normalized.
    EmptyMap = binary_to_term(term_to_binary(#{})),
    LargeMap = maps:from_list([{N, N} || N <- lists:seq(1, 40)]),
    Binary = binary:copy(<<42>>, 128),
    Views = {binary:part(Binary, 1, 80), binary:part(Binary, 2, 80)},
    [{Name, erts_debug:flat_size(Term), erts_debug:size_shared(Term)} ||
        {Name, Term} <- [{empty_map, EmptyMap}, {large_map, LargeMap}, {binary_views, Views},
                       {empty_tuple_copy, erts_debug:copy_shared({}, true)}]].

capture([Directory]) ->
    ok = prim_file:write_file(Directory ++ "/values.etf", term_to_binary(run(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/stacks.etf", term_to_binary(run_stacks(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/sharing.etf", term_to_binary(sharing(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/layouts.etf", term_to_binary(layouts(), [{minor_version, 2}])),
    erlang:halt(0).

literal() -> {alpha, [beta, gamma], #{delta => epsilon}}.

closure() ->
    Child = dynamic_tuple({42, 43}),
    Pair = {Child, Child},
    fun() -> Pair end.
