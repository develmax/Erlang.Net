namespace Erlang.Differential;

public static class CpuTopologyStateCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        (CpuTopologyStateNames.Set, 1) => "erlang-cpu-state-logical",
        (CpuTopologyStateNames.SystemFlag, 2) => "erlang-cpu-state-raw-order",
        (CpuTopologyStateNames.SystemInfo, 1) => "erlang-cpu-state-queries",
        _ => null
    };

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-cpu-state-" + name,
            "-module(cpu_state_contract). -export([run/0]). run()->Old=erlang:system_info({internal_cpu_topology,used}),Defined=erlang:system_info({internal_cpu_topology,defined}),Detected=erlang:system_info({internal_cpu_topology,detected}),try " + body + ",Detected=erlang:system_info({internal_cpu_topology,detected}),done after erlang:system_flag(internal_cpu_topology,Defined) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "logical",
            "Previous=erlang:format_cpu_topology(Old),Previous=erlang:set_cpu_topology({logical,7}),[{cpu,-1,0,-1,0,0,7}]=erlang:system_info({internal_cpu_topology,used}),[{processor,{logical,7}}]=erlang:system_info(cpu_topology)"
        );
        Add(
            "queries",
            "erlang:set_cpu_topology({logical,8}),[{cpu,-1,0,-1,0,0,8}]=erlang:system_info({internal_cpu_topology,defined}),[{processor,{logical,8}}]=erlang:system_info({cpu_topology,defined}),erlang:format_cpu_topology(Detected)=:=erlang:system_info({cpu_topology,detected}) orelse error(detected_mismatch)"
        );
        Add(
            "raw-order",
            "Old=erlang:system_flag(internal_cpu_topology,[{cpu,-1,1,-1,0,0,9},{cpu,-1,0,-1,0,0,4}]),[{cpu,-1,0,-1,0,0,4},{cpu,-1,1,-1,0,0,9}]=erlang:system_info({internal_cpu_topology,used})"
        );
        Add(
            "threads",
            "erlang:set_cpu_topology([{processor,[{core,[{thread,{logical,4}},{thread,{logical,3}}]},{core,{logical,2}}]}]),[{cpu,-1,0,-1,0,0,4},{cpu,-1,0,-1,0,1,3},{cpu,-1,0,-1,1,0,2}]=erlang:system_info({internal_cpu_topology,defined})"
        );
        Add(
            "node-processors",
            "erlang:set_cpu_topology([{node,[{processor,{logical,4}},{processor,{logical,5}}]},{node,[{processor,{logical,6}}]}]),[{cpu,0,0,-1,0,0,4},{cpu,0,1,-1,0,0,5},{cpu,1,2,-1,0,0,6}]=erlang:system_info({internal_cpu_topology,defined})"
        );
        Add(
            "processor-nodes",
            "erlang:set_cpu_topology([{processor,[{node,[{core,{logical,4}},{core,{logical,5}}]},{node,[{core,{logical,6}}]}]}]),[{cpu,-1,0,0,0,0,4},{cpu,-1,0,0,1,0,5},{cpu,-1,0,1,2,0,6}]=erlang:system_info({internal_cpu_topology,defined})"
        );
        Add(
            "empty-info",
            "erlang:set_cpu_topology([{processor,[],[{core,[],{logical,3}}]}]),[{cpu,-1,0,-1,0,0,3}]=erlang:system_info({internal_cpu_topology,defined})"
        );
        Add(
            "clear",
            "erlang:set_cpu_topology({logical,1}),[{cpu,-1,0,-1,0,0,1}]=erlang:system_flag(internal_cpu_topology,undefined),undefined=erlang:system_info({internal_cpu_topology,defined}),Detected=erlang:system_info({internal_cpu_topology,used})"
        );
        Add(
            "boundary",
            "erlang:set_cpu_topology({logical,65535}),[{cpu,-1,0,-1,0,0,65535}]=erlang:system_info({internal_cpu_topology,defined})"
        );
        Add(
            "raw-negative",
            "erlang:system_flag(internal_cpu_topology,[{cpu,-1,-1,-1,-1,-1,-1}]),[{cpu,-1,-1,-1,-1,-1,-1}]=erlang:system_info({internal_cpu_topology,defined})"
        );
        foreach (string value in new[] { "wrong", "[]", "{logical,-1}", "{logical,65536}", "{logical,1.0}", "{logical,atom}", "[{logical,1}]", "[{processor,[]}]", "[{processor,[{core,{logical,1}},{thread,{logical,2}}]}]", "[{processor,[{core,{logical,1}},{core,{logical,1}}]}]", "[{processor,[{core,wrong,{logical,1}}]}]", "[{processor,{logical,1}}|tail]" })
            Add(
                "external-invalid-" + cases.Count,
                "try erlang:set_cpu_topology(" + value + ") of _->error(accepted) catch error:badarg->ok end,Old=erlang:system_info({internal_cpu_topology,used})"
            );
        foreach (string value in new[] { "wrong", "[]", "[wrong]", "[{wrong,-1,0,-1,0,0,1}]", "[{cpu,-1,0,-1,0,0,1}|tail]", "[{cpu,-2,0,-1,0,0,1}]", "[{cpu,-1,65536,-1,0,0,1}]", "[{cpu,-1,0,-1,0,0,65536}]", "[{cpu,-1,0,-1,0,0,1.0}]", "[{cpu,-1,0,-1,0,0,1},{cpu,-1,1,-1,0,0,1}]", "[{cpu,-1,0,-1,0,0,1},{cpu,-1,0,-1,0,0,2}]", "[{cpu,0,0,-1,0,0,1},{cpu,1,0,-1,1,0,2}]", "[{cpu,-1,0,-1,0,0,1},{cpu,1,1,-1,0,0,2}]" })
            Add(
                "internal-invalid-" + cases.Count,
                "try erlang:system_flag(internal_cpu_topology," + value + ") of _->error(accepted) catch error:badarg->ok end,Old=erlang:system_info({internal_cpu_topology,used})"
            );

        foreach (string query in new[] { "{cpu_topology,wrong}", "{internal_cpu_topology,wrong}", "{cpu_topology,0}", "{internal_cpu_topology}", "internal_cpu_topology" })
        {
            string reason = query == "{internal_cpu_topology,wrong}" ? "[]" : "badarg";
            Add("invalid-query-" + cases.Count, "try erlang:system_info(" + query + ") of _->error(accepted) catch error:" + reason + "->ok end");
        }

        return cases;
    }
}
