-module(contract_nif509).
-export([errors/0, rejections/0, lifecycle/0]).

capture(Fun) -> contract_control509:capture(Fun).

errors() ->
    Cases = [42, not_a_path509, [], [contract_path_atom509], [97|bad_tail509],
             [0], [16#110000], <<0>>, <<255>>, "contract_nif_missing509"],
    #{caller => self(), node => node(), source_module => contract_nif_subject509,
      file_inputs => [{Path, capture(fun() -> contract_nif_subject509:load(Path, #{caller => self()}) end)} || Path <- Cases],
      direct_entry => capture(fun() -> erlang:load_nif("contract_nif_missing509", []) end),
      apply_entry => capture(fun() -> apply(erlang, load_nif, ["contract_nif_missing509", []]) end),
      function_entry => capture(fun() -> F = fun erlang:load_nif/2, F("contract_nif_missing509", []) end),
      code_provenance => provenance()}.

% Root builds these independent native variants before admitting this job.
% A missing fixture is a hard preparation error, never a passing skipped case.
rejections() ->
    Root = native_root(),
    Modes = [no_init, wrong_module, missing_function, not_declared,
             duplicate_function, illegal_flags, load_veto, future_abi, future_minor, obsolete_abi, experimental_abi, wrong_variant],
    ok = required_native_files(Root, [atom_to_list(Mode) || Mode <- Modes]),
    #{caller => self(), node => node(), code_provenance => provenance(),
      modes => [{Mode, capture(fun() -> contract_nif_subject509:load(
                       filename:join(Root, atom_to_list(Mode)), {self(), 509}) end)} || Mode <- Modes],
      after_rejections => capture(fun() -> contract_nif_subject509:ping() end)}.

lifecycle() ->
    Root = native_root(),
    ok = required_native_files(Root, ["valid", "upgrade_veto", "no_upgrade"]),
    Parent = self(), Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() -> contract_nif_subject509:hold(Parent, Token) end),
    try
        Ready = receive {old_code_ready509, Token, Pid, V} -> V
                after 5000 -> error({old_code_ready_timeout509, Pid}) end,
        First = capture(fun() -> contract_nif_subject509:load(filename:join(Root, "valid"), {self(), 1}) end),
        {returned, ok} = First,
        Ping = capture(fun() -> contract_nif_subject509:ping() end),
        Reload = capture(fun() -> contract_nif_subject509:load(filename:join(Root, "valid"), {self(), 1}) end),
        % Retain exact second-generation bytes separately for image provenance.
        {ok, contract_nif_subject509, Binary, Warnings} = compile:file("contract_nif_subject509.erl",
            [binary, return_errors, return_warnings, {d, 'CONTRACT509_VERSION', 2}]),
        ok = file:write_file("contract_nif_subject509-v2.beam", Binary),
        Publication = capture(fun() -> code:load_binary(contract_nif_subject509,
                                 "contract_nif_subject509-v2.beam", Binary) end),
        Ref = make_ref(), Pid ! {old_code_load509, self(), Ref, filename:join(Root, "valid")},
        Old = receive {old_code_result509, Ref, Pid, Result} -> Result
              after 5000 -> error({old_code_load_timeout509, Pid}) end,
        Version = capture(fun() -> contract_nif_subject509:version() end),
        UnsupportedUpgrade = capture(fun() -> contract_nif_subject509:load(filename:join(Root, "no_upgrade"), {self(), 2}) end),
        Upgrade = capture(fun() -> contract_nif_subject509:load(filename:join(Root, "upgrade_veto"), {self(), 2}) end),
        Recovery = capture(fun() -> contract_nif_subject509:load(filename:join(Root, "valid"), {self(), 2}) end),
        {returned, ok} = Recovery,
        After = capture(fun() -> contract_nif_subject509:ping() end),
        Cleanup = contract_control509:settle(Pid, Monitor),
        OldPurge = capture(fun() -> code:purge(contract_nif_subject509) end),
        OldUnload = receive {native_unload509, 1} -> {unloaded509, 1}
                    after 5000 -> error(old_native_unload_timeout509) end,
        Delete = capture(fun() -> code:delete(contract_nif_subject509) end),
        Purge = capture(fun() -> code:purge(contract_nif_subject509) end),
        Unload = receive {native_unload509, 2} -> {unloaded509, 2}
                 after 5000 -> error(native_unload_timeout509) end,
        #{caller => self(), node => node(), subject => Pid, monitor => Monitor, token => Token,
          old_version => Ready, initial_load => First, ping => Ping, reload => Reload,
          warnings => Warnings, publication => Publication, old_code_call => Old,
          new_version => Version, unsupported_upgrade => UnsupportedUpgrade, upgrade_veto => Upgrade, recovery => Recovery,
          after_upgrade => After, child_cleanup => Cleanup, delete => Delete, purge => Purge,
          old_purge => OldPurge, old_unload => OldUnload, unload => Unload, code_provenance => provenance()}
    after contract_control509:settle(Pid, Monitor)
    end.

native_root() ->
    case os:getenv("CONTRACT509_NATIVE_ROOT") of
        false -> error(required_native_fixture_root509);
        Root -> Root
    end.

required_native_files(Root, Names) ->
    Extension = case os:type() of {win32, _} -> ".dll"; _ -> ".so" end,
    Missing = [filename:join(Root, Name ++ Extension) || Name <- Names,
               not filelib:is_regular(filename:join(Root, Name ++ Extension))],
    case Missing of [] -> ok; _ -> error({required_native_fixtures_missing509, Missing}) end.

provenance() ->
    [{Module, code:which(Module), capture(fun() -> Module:module_info(md5) end)} ||
        Module <- [?MODULE, contract_nif_subject509, erlang, proc_lib]].
