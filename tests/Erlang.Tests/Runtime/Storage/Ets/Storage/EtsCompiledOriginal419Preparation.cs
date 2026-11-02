namespace Erlang.Tests;

internal static class EtsCompiledOriginal419Preparation
{
    internal static PreparedBeamImage Prepare(ProcessRuntime owner, ModuleRegistry registry, BeamDependencySelection declared)
    {
        RuntimeBootstrapEtsCompiled419.Register(owner, registry);
        PreparedBeamImage facet = registry.PrepareBeam(EtsCompiledOriginal419Access.Resource(EtsCompiledOriginal419Names.FacetBeam), declared);
        EtsCompiledOriginal419Access.Require(facet.Name == EtsCompiledOriginal419Names.Ets && facet.NativePrimitiveExports.Count == 3);
        foreach (var s in EtsCompiledOriginal419Names.Signatures) EtsCompiledOriginal419Access.Require(facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(e => e.Module == EtsCompiledOriginal419Names.Ets).ToArray();
        EtsCompiledOriginal419Access.Require(exports.Length == 5);
        foreach (var e in exports) EtsCompiledOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), facet));
        foreach (var s in EtsCompiledOriginal419Names.Signatures) EtsCompiledOriginal419Access.Require(registry.HasExport(EtsCompiledOriginal419Names.Ets, s.Function, s.Arity));
    }

    internal static PreparedBeamImage Prepare171(
        ProcessRuntime owner,
        ModuleRegistry registry,
        BeamDependencySelection declared,
        global::Erlang.Tests.PreparedBeamFixtureScope171 scope
    )
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(EtsCompiledOriginal419Access.Resource(EtsCompiledOriginal419Names.FacetBeam), declared));
        EtsCompiledOriginal419Access.Require(facet.Name == EtsCompiledOriginal419Names.Ets && facet.NativePrimitiveExports.Count == 3);
        foreach (var s in EtsCompiledOriginal419Names.Signatures) EtsCompiledOriginal419Access.Require(facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return facet;
    }

    internal static void Register171(ProcessRuntime owner, ModuleRegistry registry, BeamDependencySelection declared)
    {
        RuntimeBootstrapEtsCompiled419.Register(owner, registry);
    }
}
