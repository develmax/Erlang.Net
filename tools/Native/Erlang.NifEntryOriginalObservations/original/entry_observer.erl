-module(entry_observer).
-export([metadata/0,run/1]).

run(Case) ->
    {Module, Leaf, Info, Outcome} = spec(Case),
    case code:is_loaded(Module) of
        false -> {module, Module} = code:load_file(Module);
        {file, _} -> ok
    end,
    Path = filename:join(os:getenv("ENTRY_NATIVE_ROOT"), Leaf),
    Result = Module:load(Path, Info),
    Observed = case Outcome of
        {success, ExpectedMarker, ExpectedThird} ->
            ok = Result,
            Payload = {payload, #{alpha => [17, <<1,2,3>>]}},
            Payload = Module:identity(Payload),
            ExpectedMarker = Module:marker(),
            ExpectedThird = Module:third(),
            ordinary_untouched = Module:ordinary(),
            {entry_case, Case, Module, Result,
                {Payload, ExpectedMarker, ExpectedThird, ordinary_untouched}};
        {error_tag, Tag} ->
            {error, {Tag, Reason}} = Result,
            true = is_list(Reason),
            not_loaded = try Module:marker() of
                Value -> {unexpected_native_return, Value}
            catch error:Error -> Error end,
            {entry_case, Case, Module, Result, {stub_after_failure, not_loaded}}
    end,
    true = code:delete(Module),
    false = code:purge(Module),
    false = erlang:check_old_code(Module),
    false = code:is_loaded(Module),
    {Observed, {cleanup, deleted_and_purged}}.

metadata() ->
    ExpectedRoot = filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),
    ActualRoot = filename:absname(code:root_dir()),
    ExpectedRoot = ActualRoot,
    {entry_runtime_metadata, ActualRoot, erlang:system_info(otp_release),
        erlang:system_info(version), erlang:system_info(system_architecture),
        erlang:system_info(wordsize)}.

spec(alpha_no_declarations) -> {native_entry_alpha,"alpha",73,{success,41,"alpha"}};
spec(beta_explicit) -> {native_entry_beta,"beta",73,{success,73,233}};
spec(beta_absent_load) -> {native_entry_beta,"beta_absent_load",73,{success,-1,233}};
spec(beta_absent_unload) -> {native_entry_beta,"beta_absent_unload",73,{success,73,233}};
spec(beta_no_declarations) -> {native_entry_beta,"beta",73,{success,73,233}};
spec(alpha_duplicate) -> {native_entry_alpha,"alpha_duplicate",73,{error_tag,bad_lib}};
spec(beta_undeclared) -> {native_entry_beta,"beta_undeclared",73,{error_tag,bad_lib}};
spec(beta_missing) -> {native_entry_beta,"beta_missing",73,{error_tag,bad_lib}};
spec(module_mismatch) -> {native_entry_beta,"alpha",73,{error_tag,bad_lib}};
spec(load_veto) -> {native_entry_beta,"beta",veto,{error_tag,load}};
spec(latin1_module) -> {'native_entry_béta',"beta_latin1",73,{success,73,233}}.
