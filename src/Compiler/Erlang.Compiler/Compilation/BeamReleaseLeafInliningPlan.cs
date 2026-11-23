namespace Erlang.Compiler;

internal readonly record struct BeamReleaseLeafInliningPlan(bool SuppressAutomatic, int? Size)
{
    internal static BeamReleaseLeafInliningPlan Read(ModuleDefinition module, BeamReleaseBuildProfile? profile)
    {
        if (profile is null)
        {
            return default;
        }

        // An explicit option list replaces the synthesized compile-info list.
        // Debug options only describe the stored abstract representation.
        var recorded = profile.RecordedCompileOptions;

        bool HasOption(string name) => recorded is not null
            ? recorded.Contains(Term.A(name)) : profile.WarningOptions.Contains(name);

        int? ReadOption(string name) => recorded?.OfType<TupleTerm>()
            .FirstOrDefault(item => item.Items is [Atom flag, Integer value]
                && flag.Name == name && value.Value >= int.MinValue && value.Value <= int.MaxValue)
            ?.Items[1] is Integer result ? Math.Max(1, (int)result.Value) : null;

        bool stopped = module.AutomaticInliningDisabled || HasOption(CompilerOptionNames.NoInline)
            || HasOption(CompilerOptionNames.DisableCoreOptimization);
        if (stopped || module.OnLoadFunction is not null || module.DisableModuleOptimization
            || !HasOption(CompilerOptionNames.Inline)
            || ReadOption(CompilerOptionNames.InlineEffort) is int effort && effort != BeamTargetedInlining.AutomaticInlineEffort)
        {
            return new(stopped, null);
        }

        int size = module.AutomaticInlineSize ?? ReadOption(CompilerOptionNames.InlineSize)
            ?? BeamTargetedInlining.DefaultAutomaticInlineSize;

        return new(false, size);
    }
}
