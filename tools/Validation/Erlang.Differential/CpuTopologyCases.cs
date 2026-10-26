namespace Erlang.Differential;

public static class CpuTopologyCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == CpuTopologyNames.Format && arity == 1 ? "erlang-cpu-format-undefined" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-cpu-format-" + name,
            "-module(cpu_format_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("undefined", "undefined=erlang:format_cpu_topology(undefined)");
        Add("empty", "[]=erlang:format_cpu_topology([])");
        Add("single-unspecified", "{logical,0}=erlang:format_cpu_topology([{cpu,-1,-1,-1,-1,-1,0}])");
        Add("single-processor", "[{processor,{logical,0}}]=erlang:format_cpu_topology([{cpu,-1,0,-1,-1,-1,0}])");
        Add(
            "core-siblings",
            "[{processor,[{core,{logical,0}},{core,{logical,1}}]}]=erlang:format_cpu_topology([{cpu,-1,0,-1,0,-1,0},{cpu,-1,0,-1,1,-1,1}])"
        );
        Add(
            "thread-siblings",
            "[{processor,[{thread,{logical,0}},{thread,{logical,1}}]}]=erlang:format_cpu_topology([{cpu,-1,0,-1,0,0,0},{cpu,-1,0,-1,0,1,1}])"
        );
        Add("processor-node", "[{processor,[{node,{logical,0}}]}]=erlang:format_cpu_topology([{cpu,-1,0,2,-1,-1,0}])");
        Add(
            "nodes",
            "[{node,[{processor,{logical,0}}]},{node,[{processor,{logical,1}}]}]=erlang:format_cpu_topology([{cpu,0,0,-1,-1,-1,0},{cpu,1,1,-1,-1,-1,1}])"
        );
        Add("duplicate-logical-collapse", "{logical,0}=erlang:format_cpu_topology([{cpu,-1,-1,-1,-1,-1,0},{cpu,-1,-1,-1,-1,-1,0}])");
        Add("unvalidated-logical-term", "{logical,{kept,42}}=erlang:format_cpu_topology([{cpu,-1,-1,-1,-1,-1,{kept,42}}])");
        foreach (string value in new[] { "wrong", "42", "{}", "[wrong]", "[{wrong,-1,-1,-1,-1,-1,0}]", "[{cpu,-1,-1,-1,-1,0}]", "[{cpu,-1,-1,-1,-1,-1,0}|tail]" })
            Add(
                "invalid-" + cases.Count,
                "try erlang:format_cpu_topology(" + value + ") of _->error(accepted) catch error:internal_error->ok end"
            );

        return cases;
    }
}
