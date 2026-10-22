-module(beam_debug_code427).
-export([run/0, run_stacks/0, worker/2, profile/0, witness/0, capture/1]).

names() -> [atom, nil, integer, negative, oversized_address, float, binary, bits,
            map, empty_tuple, tuple, exported_mfa, missing_mfa, wildcard_mfa,
            bad_arity_mfa, improper_list, pid, reference, external_fun, local_fun].
cases() -> [{disassemble, Name} || Name <- names()] ++ [{interpreter_size, no_args}].

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
        exit(Pid, kill), receive {'DOWN', Ref, process, Pid, _} -> ok end,
        exit({worker_timeout, Name})
    end.

worker(Parent, Name) ->
    Outcome = try {value, perform(Name)} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end,
    Parent ! {Name, Outcome}.

perform({interpreter_size, no_args}) -> erts_debug:interpreter_size();
perform({disassemble, Name}) -> erts_debug:disassemble(argument(Name)).

argument(atom) -> hello;
argument(nil) -> [];
argument(integer) -> 0;
argument(negative) -> -1;
argument(oversized_address) -> 1 bsl 130;
argument(float) -> 1.25;
argument(binary) -> <<1, 2>>;
argument(bits) -> <<1:1>>;
argument(map) -> #{a => b};
argument(empty_tuple) -> {};
argument(tuple) -> {one};
argument(exported_mfa) -> {?MODULE, witness, 0};
argument(missing_mfa) -> {missing_debug_code427, missing, 0};
argument(wildcard_mfa) -> {'_', '_', '_'};
argument(bad_arity_mfa) -> {erlang, self, -1};
argument(improper_list) -> [a | b];
argument(pid) -> self();
argument(reference) -> make_ref();
argument(external_fun) -> fun erlang:self/0;
argument(local_fun) -> fun() -> witness() end.

profile() -> {erlang:system_info(emu_flavor), erlang:system_info(build_type), erlang:system_info(otp_release)}.
witness() -> {managed_execution_witness, 13}.

capture([Directory]) ->
    ok = prim_file:write_file(Directory ++ "/profile.etf", term_to_binary(profile(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/values.etf", term_to_binary(run(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/stacks.etf", term_to_binary(run_stacks(), [{minor_version, 2}])),
    erlang:halt(0).
