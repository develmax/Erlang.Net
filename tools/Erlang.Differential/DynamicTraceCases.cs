namespace Erlang.Differential;

public static class DynamicTraceCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("dt_get_tag", 0) => "erlang-dynamic-trace-get-tag",
        ("dt_get_tag_data", 0) => "erlang-dynamic-trace-get-tag-data",
        ("dt_put_tag", 1) => "erlang-dynamic-trace-put-0",
        ("dt_append_vm_tag_data", 1) => "erlang-dynamic-trace-append-0",
        ("dt_prepend_vm_tag_data", 1) => "erlang-dynamic-trace-prepend-0",
        ("dt_spread_tag", 1) => "erlang-dynamic-trace-spread-false",
        ("dt_restore_tag", 1) => "erlang-dynamic-trace-restore-true",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-dynamic-trace-" + name,
            "-module(dynamic_trace_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("get-tag", "undefined=erlang:dt_get_tag()");
        Add("get-tag-data", "undefined=erlang:dt_get_tag_data()");
        Add("spread-true", "true=erlang:dt_spread_tag(true)");
        Add("spread-false", "true=erlang:dt_spread_tag(false)");
        Add("restore-true", "true=erlang:dt_restore_tag(true)");
        int index = 0;
        foreach (string value in new[] { "undefined", "false", "42", "[]", "[a|b]", "<<1:3>>", "#{key=>42}", "{true,<<>>}" })
        {
            Add("put-" + index, "undefined=erlang:dt_put_tag(" + value + "),undefined=erlang:dt_get_tag(),undefined=erlang:dt_get_tag_data()");
            Add("append-" + index, "V = " + value + ",V=erlang:dt_append_vm_tag_data(V)");
            Add("prepend-" + index, "V = " + value + ",V=erlang:dt_prepend_vm_tag_data(V)");
            Add("restore-invalid-" + index, "ok=try erlang:dt_restore_tag(" + value + ") of _->unexpected catch error:badarg->ok end");
            Add(
                "spread-invalid-" + index,
                value == "false" ? "true=erlang:dt_spread_tag(false)" : "ok=try erlang:dt_spread_tag(" + value + ") of _->unexpected catch error:badarg->ok end"
            );
            index++;
        }
        Add(
            "restore-spread-return",
            "true=erlang:dt_restore_tag(erlang:dt_spread_tag(true)),true=erlang:dt_restore_tag(erlang:dt_spread_tag(false))"
        );

        return cases;
    }
}
