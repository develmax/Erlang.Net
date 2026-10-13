namespace Erlang.Differential;

public static class Part232CpuTraceRepairCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-part232-cpu-trace-" + name,
            "-module(cpu_trace_repair). -export([run/0,identity/1]). run()->" + body + ",done. identity(V)->V.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "fresh-internal-invalid-atom",
            "try erlang:system_info({internal_cpu_topology,unknown_selector}) of _->error(accepted) catch error:[]:Stack->[]=Stack end"
        );
        Add(
            "fresh-internal-invalid-nonatom",
            "try erlang:system_info({internal_cpu_topology,42}) of _->error(accepted) catch error:[]:Stack->[]=Stack end"
        );
        Add(
            "external-invalid-selectors",
            "try erlang:system_info({cpu_topology,unknown_selector}) of _->error(accepted) catch error:badarg->ok end,try erlang:system_info({cpu_topology,42}) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "valid-configured-views",
            "Defined=erlang:system_info({internal_cpu_topology,defined}),Detected=erlang:system_info({internal_cpu_topology,detected}),try erlang:set_cpu_topology({logical,12}),[{cpu,-1,0,-1,0,0,12}]=erlang:system_info({internal_cpu_topology,defined}),[{cpu,-1,0,-1,0,0,12}]=erlang:system_info({internal_cpu_topology,used}),Detected=erlang:system_info({internal_cpu_topology,detected}) after erlang:system_flag(internal_cpu_topology,Defined) end"
        );
        Add(
            "missing-mfa-keyed-values",
            "{traced,undefined}=erlang:trace_info({cpu_trace_repair,missing,1},traced),{match_spec,undefined}=erlang:trace_info({cpu_trace_repair,missing,1},match_spec),{traced,undefined}=erlang:trace_info({missing_cpu_trace_module,missing,1},traced),{match_spec,undefined}=erlang:trace_info({missing_cpu_trace_module,missing,1},match_spec)"
        );
        Add(
            "signed-small-mfa-boundaries",
            "{traced,undefined}=erlang:trace_info({cpu_trace_repair,identity,-576460752303423488},traced),{match_spec,undefined}=erlang:trace_info({cpu_trace_repair,identity,-1},match_spec),{traced,undefined}=erlang:trace_info({cpu_trace_repair,identity,576460752303423487},traced),try erlang:trace_info({cpu_trace_repair,identity,576460752303423488},traced) of _->error(accepted) catch error:badarg->ok end,{traced,false}=erlang:trace_info({cpu_trace_repair,identity,1},traced),{match_spec,false}=erlang:trace_info({cpu_trace_repair,identity,1},match_spec)"
        );

        return cases;
    }
}
