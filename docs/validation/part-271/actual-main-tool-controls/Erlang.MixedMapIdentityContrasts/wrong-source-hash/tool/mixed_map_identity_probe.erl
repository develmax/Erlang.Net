-module(mixed_map_identity_probe).
-export([run/1]).

run(Output) ->
    put(output, Output),
    save("stage", <<"started">>),
    try
        observe(),
        save("stage", <<"complete">>),
        halt(0)
    catch Class:Reason:Stack ->
        term("exception", {Class, Reason, Stack}),
        save("stage", <<"exception">>),
        halt(1)
    end.

observe() ->
    false = erlang:is_alive(),
    nonode@nohost = node(),
    Owner = self(),
    LocalReference = make_ref(),
    ReferenceBytes = <<131, 90, 3:16, 119, 6, "a@host", 7:32,
                       1:32, 2:32, 3:32>>,
    ExternalReference = binary_to_term(ReferenceBytes),
    PidBytes = external_pid_bytes(Owner),
    ExternalPid = binary_to_term(PidBytes),
    ReferenceMap = #{LocalReference => local_reference,
                     ExternalReference => external_reference},
    PidMap = #{Owner => local_pid, ExternalPid => external_pid},
    Combined = maps:merge(ReferenceMap, PidMap),
    Pairs = [{reference, LocalReference, ExternalReference, ReferenceMap,
              #{ReferenceMap => retained_reference_map}},
             {pid, Owner, ExternalPid, PidMap,
              #{PidMap => retained_pid_map}}],
    CombinedOuter = #{Combined => retained_combined_map},
    Name = '0',
    Options = #{name_domain => shortnames, dist_listen => false},
    term("inputs", #{name => Name, options => Options,
                     external_reference_bytes => ReferenceBytes,
                     external_pid_bytes => PidBytes,
                     external_creation => 7}),
    try
        snapshot("before", Pairs, Combined, CombinedOuter),
        Started = net_kernel:start(Name, Options),
        term("started-operation", Started),
        require_started(Started),
        snapshot("started", Pairs, Combined, CombinedOuter),
        Stopped = net_kernel:stop(),
        term("stopped-operation", Stopped),
        require_stopped(Stopped),
        snapshot("stopped", Pairs, Combined, CombinedOuter)
    after
        term("cleanup-kernel", outcome(fun net_kernel:stop/0))
    end.

external_pid_bytes(Pid) ->
    <<131, 88, Body/binary>> = term_to_binary(Pid),
    <<131, AtomBytes/binary>> = term_to_binary(node(Pid)),
    AtomSize = byte_size(AtomBytes),
    <<_:AtomSize/binary, Number:32, Serial:32, _Creation:32>> = Body,
    <<131, 88, 119, 6, "a@host", Number:32, Serial:32, 7:32>>.

require_started({ok, Kernel}) when is_pid(Kernel) -> ok;
require_started(Other) -> error({probe_start_failed, Other}).

require_stopped(ok) -> ok;
require_stopped(Other) -> error({probe_stop_failed, Other}).

snapshot(Phase, Pairs, RetainedCombined, CombinedOuter) ->
    Rows = [pair_snapshot(Phase, Pair) || Pair <- Pairs],
    RebuiltCombined = maps:from_list(lists:append(
        [[{Local, local_value(Name)}, {External, external_value(Name)}]
         || {Name, Local, External, _Map, _Outer} <- Pairs])),
    term(Phase ++ "-combined-retained", RetainedCombined),
    term(Phase ++ "-combined-rebuilt", RebuiltCombined),
    term(Phase ++ "-facts",
         #{node => node(), alive => erlang:is_alive(),
           creation => erlang:system_info(creation), pairs => Rows,
           combined => map_facts(RetainedCombined, RebuiltCombined,
                                 CombinedOuter)}).

pair_snapshot(Phase, {Name, Local, External, Retained, Outer}) ->
    Rebuilt = #{Local => local_value(Name), External => external_value(Name)},
    Prefix = Phase ++ "-" ++ atom_to_list(Name),
    term(Prefix ++ "-identifiers", {Local, External}),
    term(Prefix ++ "-retained-map", Retained),
    term(Prefix ++ "-rebuilt-map", Rebuilt),
    #{name => Name, local_node => node(Local), external_node => node(External),
      identifier_order => {Local < External, External < Local,
                            Local =:= External},
      local_lookup => outcome(fun() -> maps:find(Local, Retained) end),
      external_lookup => outcome(fun() -> maps:find(External, Retained) end),
      map => map_facts(Retained, Rebuilt, Outer)}.

map_facts(Retained, Rebuilt, Outer) ->
    #{exact_equal => Retained =:= Rebuilt,
      numeric_equal => Retained == Rebuilt,
      order => {Retained < Rebuilt, Rebuilt < Retained},
      retained_list => maps:to_list(Retained),
      rebuilt_list => maps:to_list(Rebuilt),
      retained_sorted_list => lists:sort(maps:to_list(Retained)),
      rebuilt_sorted_list => lists:sort(maps:to_list(Rebuilt)),
      hashes => {erlang:phash2(Retained), erlang:phash2(Rebuilt)},
      retained_map_key_lookup => outcome(fun() -> maps:find(Retained, Outer) end),
      rebuilt_map_key_lookup => outcome(fun() -> maps:find(Rebuilt, Outer) end),
      retained_binary => term_to_binary(Retained),
      rebuilt_binary => term_to_binary(Rebuilt)}.

local_value(reference) -> local_reference;
local_value(pid) -> local_pid.

external_value(reference) -> external_reference;
external_value(pid) -> external_pid.

outcome(Fun) ->
    try {result, Fun()}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

term(Name, Value) -> save(Name ++ ".etf", term_to_binary(Value)).

save(Name, Bytes) ->
    ok = file:write_file(filename:join(get(output), Name), Bytes).

