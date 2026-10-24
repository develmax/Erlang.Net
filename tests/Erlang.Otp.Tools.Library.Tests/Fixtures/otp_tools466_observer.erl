-module(otp_tools466_observer).
-file("otp_tools466_observer.erl", 1).
-export([worker/1, run/0]).

worker(Parent) ->
    Parent ! {finished, run()}.

run() ->
    [observe(string_empty, fun() -> xref_utils:is_string([], 31) end),
     observe(string_text, fun() -> xref_utils:is_string("hello", 31) end),
     observe(string_control, fun() -> xref_utils:is_string([10], 31) end),
     observe(string_improper, fun() -> xref_utils:is_string([65 | invalid], 31) end),
     observe(path_valid, fun() -> xref_utils:is_path(["src", "include"]) end),
     observe(path_empty_component, fun() -> xref_utils:is_path(["src", []]) end),
     observe(path_improper, fun() -> xref_utils:is_path(["src" | invalid]) end),
     observe(fun_apply, fun() -> xref_utils:is_funfun(erlang, apply, 3) end),
     observe(fun_other, fun() -> xref_utils:is_funfun(other, missing, 7) end),
     observe(static_module_info, fun() -> xref_utils:is_static_function(module_info, 1) end),
     observe(static_new, fun() -> xref_utils:is_static_function(new, 5) end),
     observe(static_other, fun() -> xref_utils:is_static_function(other, 0) end),
     observe(abstract_true, fun() -> xref_utils:is_abstract_module([{abstract, true}]) end),
     observe(abstract_false, fun() -> xref_utils:is_abstract_module([{abstract, false}]) end),
     observe(msacc_empty, fun() -> msacc:stats(system_realtime, []) end),
     observe(msacc_realtime, fun() -> msacc:stats(system_realtime, data()) end),
     observe(msacc_runtime, fun() -> msacc:stats(system_runtime, data()) end),
     observe(msacc_type_empty, fun() -> msacc:stats(type, []) end),
     observe(msacc_bad_analysis, fun() -> msacc:stats(invalid, []) end),
     observe(msacc_bad_list, fun() -> msacc:stats(system_realtime, invalid) end),
     observe(msacc_bad_type, fun() -> msacc:stats(type, invalid) end),
     observe(supervisor_spec, fun() -> runtime_tools_sup:init([]) end),
     observe(application_stop, fun() -> runtime_tools:stop(any) end),
     observe(abstract_bad, fun() -> xref_utils:is_abstract_module(invalid) end)].

data() ->
    [#{counters => #{sleep => 10, emulator => 20, gc => {5, 2}}},
     #{counters => #{sleep => 7, emulator => 8}}].

observe(Name, Function) ->
    try Function() of
        Value -> {Name, {returned, Value}}
    catch
        Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
