namespace Erlang.Differential;

public static class MapTemplateOrderCases
{
    private static Term Success(params string[] markers) => Term.Tuple(Term.A(OracleOutcomeTags.Success), Cons.From(markers.Select(Term.A)));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new(
            "mc-safe-keys-reversed",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>1,(begin put(log,[k2|get(log)]),b end)=>1 || true},get(log) end",
            Success("k2","k1")
        ),
        new(
            "mc-three-safe-keys",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>1,(begin put(log,[k2|get(log)]),b end)=>1,(begin put(log,[k3|get(log)]),c end)=>1 || true},get(log) end",
            Success("k3","k2","k1")
        ),
        new(
            "mc-first-fallback",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>(begin put(log,[v1|get(log)]),1 end),(begin put(log,[k2|get(log)]),b end)=>(begin put(log,[v2|get(log)]),2 end) || true},get(log) end",
            Success(
                "v2",
                "k2",
                "v1",
                "k1"
            )
        ),
        new(
            "mc-second-fallback",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>1,(begin put(log,[k2|get(log)]),b end)=>(begin put(log,[v2|get(log)]),2 end) || true},get(log) end",
            Success("v2","k2","k1")
        ),
        new(
            "mc-distinct-safe-values",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>1,(begin put(log,[k2|get(log)]),b end)=>2 || true},get(log) end",
            Success("k2","k1")
        ),
        new(
            "mc-incoming-safe-value",
            "begin X=1,put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>X,(begin put(log,[k2|get(log)]),b end)=>X || true},get(log) end",
            Success("k2","k1")
        ),
        new(
            "mc-generator-value-disables-prefix",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>X,(begin put(log,[k2|get(log)]),b end)=>X || X <- [1]},get(log) end",
            Success("k2","k1")
        ),
        new(
            "mc-filter-value-disables-prefix",
            "begin put(log,[]),#{(begin put(log,[k1|get(log)]),a end)=>Y,(begin put(log,[k2|get(log)]),b end)=>Y || begin Y=1,true end},get(log) end",
            Success("k2","k1")
        )
    ];

    private static Term CompiledExpected(string name) => name switch
    {
        "mc-safe-keys-reversed" or "mc-distinct-safe-values" or "mc-incoming-safe-value" => Success("k1", "k2"),
        "mc-three-safe-keys" => Success("k1", "k2", "k3"),
        "mc-first-fallback" => Success(
            "v2",
            "k2",
            "k1",
            "v1"
        ),
        "mc-second-fallback" => Success("k1", "k2", "v2"),
        "mc-generator-value-disables-prefix" or "mc-filter-value-disables-prefix" => Success("k2", "k1"),
        _ => throw new NotSupportedException()
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_mc_order_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        CompiledExpected(fixture.Name)
    )).ToArray();
}
