using Erlang.Differential;

namespace Erlang.Tests;

public static class FunctionalMfaContracts
{
    public static string? FixtureName(string module, string function, int arity) => (module, function, arity) switch
    {
        ("maps", "intersect", 2) => "maps-completion-intersect",
        ("maps", "intersect_with", 3) => "maps-completion-intersect-with",
        ("maps", "merge_with", 3) => "maps-completion-merge-with",
        ("maps", "groups_from_list", 2) => "maps-completion-groups-two",
        ("maps", "groups_from_list", 3) => "maps-completion-groups-three",
        ("maps", "get", 3) => "maps-operations-get-default",
        ("maps", "find", 2) => "maps-operations-find",
        ("maps", "from_list", 1) => "maps-operations-from-list",
        ("maps", "from_keys", 2) => "maps-operations-from-keys",
        ("maps", "is_key", 2) => "maps-operations-is-key",
        ("maps", "keys", 1) or ("maps", "values", 1) => "maps-operations-keys-values",
        ("maps", "merge", 2) => "maps-operations-merge",
        ("maps", "new", 0) => "maps-operations-new",
        ("maps", "put", 3) => "maps-operations-put",
        ("maps", "remove", 2) => "maps-operations-remove",
        ("maps", "take", 2) => "maps-operations-take",
        ("maps", "to_list", 1) => "maps-operations-to-list",
        ("maps", "update", 3) => "maps-operations-update",
        ("maps", "update_with", 3) => "maps-operations-update-with-three",
        ("maps", "update_with", 4) => "maps-operations-update-with-four",
        ("maps", "with", 2) => "maps-operations-with",
        ("maps", "without", 2) => "maps-operations-without",
        ("maps", "filter", 2) => "maps-operations-filter",
        ("maps", "filtermap", 2) => "maps-operations-filtermap",
        ("maps", "fold", 3) => "maps-operations-fold",
        ("maps", "foreach", 2) => "maps-operations-foreach",
        ("maps", "map", 2) => "maps-operations-map",
        ("maps", "is_iterator_valid", 1) => "maps-operations-valid-iterator",
        ("lists", "foldl", 3) => "lists-functional-foldl",
        ("lists", "foldr", 3) => "lists-functional-foldr",
        ("lists", "foreach", 2) => "lists-functional-foreach",
        ("lists", "partition", 2) => "lists-functional-partition",
        ("lists", "all", 2) => "lists-functional-all",
        ("lists", "any", 2) => "lists-functional-any",
        ("lists", "takewhile", 2) => "lists-functional-takewhile",
        ("lists", "dropwhile", 2) => "lists-functional-dropwhile",
        ("lists", "filtermap", 2) => "lists-functional-filtermap",
        ("lists", "mapfoldl", 3) => "lists-functional-mapfoldl",
        ("lists", "mapfoldr", 3) => "lists-functional-mapfoldr",
        ("erlang", "send_after", 3) => "runtime-timer-send-after",
        ("erlang", "start_timer", 3) => "runtime-timer-start-timer",
        ("erlang", "read_timer", 1) => "runtime-timer-read-cancel",
        ("erlang", "cancel_timer", 1) => "runtime-timer-read-cancel",
        _ => module == "lists" ? ListCompletionCases.FixtureName(function, arity) : module == "erlang" ? ErlangBasicCases.FixtureName(function, arity) ?? ErlangDataCases.FixtureName(function, arity) ?? ErlangTextCases.FixtureName(function, arity) ?? ErlangClockCases.FixtureName(function, arity) ?? ErlangMd5Cases.FixtureName(function, arity) ?? ErlangRaiseCases.FixtureName(function, arity) ?? ErlangProcessQueryCases.FixtureName(function, arity) ?? ExternalFunctionCases.FixtureName(function, arity) ?? ErrorMetadataCases.FixtureName(function, arity) ?? ErlangFunctionInfoCases.FixtureName(function, arity) ?? ErlangEtfCases.FixtureName(function, arity) ?? ErlangHashCases.FixtureName(function, arity) ?? ProcessAliasCases.FixtureName(function, arity) ?? ProcessMonitorCases.FixtureName(function, arity) ?? DynamicTraceCases.FixtureName(function, arity) ?? ProcessSpawnCases.FixtureName(function, arity) ?? ProcessExitCases.FixtureName(function, arity) ?? ProcessInfoCases.FixtureName(function, arity) ?? RecordPredicateCases.FixtureName(module, function, arity) ?? ProcessIdentifierTextCases.FixtureName(function, arity) ?? PacketDecodeCases.FixtureName(function, arity) ?? ModuleLoadingCases.FixtureName(function, arity) ?? ProcessTableCases.FixtureName(function, arity) ?? ReferenceTextCases.FixtureName(function, arity) ?? PreloadedModuleCases.FixtureName(function, arity) ?? ModuleInformationCases.FixtureName(function, arity) : null
    };

    public static async Task Run(string name)
    {
        var fixture = CompiledModuleCases.All.Single(fixture => fixture.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException(actual.ToString());
    }
}
