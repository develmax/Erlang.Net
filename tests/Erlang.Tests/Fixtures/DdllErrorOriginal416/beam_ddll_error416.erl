-module(beam_ddll_error416).
-export([run/0, run_stacks/0, child/3]).

cases() -> [atom_inconsistent, atom_linked_in_driver, atom_permanent, atom_not_loaded, atom_not_loaded_by_this_process, atom_not_pending, atom_already_loaded, atom_pending_reload, atom_pending_process, atom_no_error, atom_no_driver_init, atom_driver_init_failed, atom_bad_driver_name, atom_driver_name_to_long, atom_driver_incorrect_version, atom_no_ddll_available, atom_unspecified_error, atom_symbol_not_found, code_0, code_1, code_2, code_3, code_4, code_5, code_6, code_7, code_8, code_gap, system_zero, system_one, system_two, system_five, system_126, narrow_negative, narrow_zero, small_min, small_max, bad_large_low, bad_large_high, bad_unknown, bad_nil, bad_tuple, bad_float, bad_tag].

run() -> [capture(Name, default) || Name <- cases()].

run_stacks() ->
    [{Name, [{Depth, capture(Name, Depth)} || Depth <- [default, 0, 1, 2, 8]]}
     || Name <- cases()].

capture(Name, default) -> capture_child(Name);
capture(Name, Depth) ->
    Previous = erlang:system_flag(backtrace_depth, Depth),
    try capture_child(Name)
    after erlang:system_flag(backtrace_depth, Previous)
    end.

capture_child(Name) ->
    {Pid, Ref} = spawn_monitor(?MODULE, child, [self(), Name, undefined]),
    receive
        {Pid, Outcome} ->
            receive {'DOWN', Ref, process, Pid, normal} -> {Name, Outcome} end;
        {'DOWN', Ref, process, Pid, Reason} -> erlang:error({ddll_child_failed, Name, Reason})
    after 5000 -> erlang:error({ddll_child_timeout, Name})
    end.

child(Parent, Name, _) ->
    Outcome = try probe(Name) of
                  Value -> {value, Value}
              catch Class:Reason:Trace -> {exception, Class, Reason, Trace}
              end,
    Parent ! {self(), Outcome}.

probe(atom_inconsistent) -> erl_ddll:format_error_int(inconsistent);
probe(atom_linked_in_driver) -> erl_ddll:format_error_int(linked_in_driver);
probe(atom_permanent) -> erl_ddll:format_error_int(permanent);
probe(atom_not_loaded) -> erl_ddll:format_error_int(not_loaded);
probe(atom_not_loaded_by_this_process) -> erl_ddll:format_error_int(not_loaded_by_this_process);
probe(atom_not_pending) -> erl_ddll:format_error_int(not_pending);
probe(atom_already_loaded) -> erl_ddll:format_error_int(already_loaded);
probe(atom_pending_reload) -> erl_ddll:format_error_int(pending_reload);
probe(atom_pending_process) -> erl_ddll:format_error_int(pending_process);
probe(atom_no_error) -> erl_ddll:format_error_int(no_error);
probe(atom_no_driver_init) -> erl_ddll:format_error_int(no_driver_init);
probe(atom_driver_init_failed) -> erl_ddll:format_error_int(driver_init_failed);
probe(atom_bad_driver_name) -> erl_ddll:format_error_int(bad_driver_name);
probe(atom_driver_name_to_long) -> erl_ddll:format_error_int(driver_name_to_long);
probe(atom_driver_incorrect_version) -> erl_ddll:format_error_int(driver_incorrect_version);
probe(atom_no_ddll_available) -> erl_ddll:format_error_int(no_ddll_available);
probe(atom_unspecified_error) -> erl_ddll:format_error_int(unspecified_error);
probe(atom_symbol_not_found) -> erl_ddll:format_error_int(symbol_not_found);
probe(code_0) -> erl_ddll:format_error_int({open_error,0});
probe(code_1) -> erl_ddll:format_error_int({open_error,-1});
probe(code_2) -> erl_ddll:format_error_int({open_error,-2});
probe(code_3) -> erl_ddll:format_error_int({open_error,-3});
probe(code_4) -> erl_ddll:format_error_int({open_error,-4});
probe(code_5) -> erl_ddll:format_error_int({open_error,-5});
probe(code_6) -> erl_ddll:format_error_int({open_error,-6});
probe(code_7) -> erl_ddll:format_error_int({open_error,-7});
probe(code_8) -> erl_ddll:format_error_int({open_error,-8});
probe(code_gap) -> erl_ddll:format_error_int({open_error,-9});
probe(system_zero) -> erl_ddll:format_error_int({open_error,-10});
probe(system_one) -> erl_ddll:format_error_int({open_error,-11});
probe(system_two) -> erl_ddll:format_error_int({open_error,-12});
probe(system_five) -> erl_ddll:format_error_int({open_error,-15});
probe(system_126) -> erl_ddll:format_error_int({open_error,-136});
probe(narrow_negative) -> erl_ddll:format_error_int({open_error,-4294967297});
probe(narrow_zero) -> erl_ddll:format_error_int({open_error,4294967296});
probe(small_min) -> erl_ddll:format_error_int({open_error,-576460752303423488});
probe(small_max) -> erl_ddll:format_error_int({open_error,576460752303423487});
probe(bad_large_low) -> erl_ddll:format_error_int({open_error,-576460752303423489});
probe(bad_large_high) -> erl_ddll:format_error_int({open_error,576460752303423488});
probe(bad_unknown) -> erl_ddll:format_error_int(missing_ddll_error416);
probe(bad_nil) -> erl_ddll:format_error_int([]);
probe(bad_tuple) -> erl_ddll:format_error_int({open_error});
probe(bad_float) -> erl_ddll:format_error_int({open_error,1.0});
probe(bad_tag) -> erl_ddll:format_error_int({system_error,-1}).
