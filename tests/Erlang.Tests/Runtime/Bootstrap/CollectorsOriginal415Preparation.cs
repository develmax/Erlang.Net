using Erlang;
namespace Erlang.Tests;

internal static class CollectorsOriginal415Preparation
{
    internal static void Prepare(ModuleRegistry registry)
    {
        var declared = new BeamDependencySelection([CollectorsOriginal415Names.Runtime,CollectorsOriginal415Names.Internal,
            CollectorsOriginal415Names.Literal,CollectorsOriginal415Names.Trace]);
        var literal = registry.PrepareBeam(CollectorsOriginal415Access.Resource(CollectorsOriginal415Names.LiteralBeam), declared);
        var trace = registry.PrepareBeam(CollectorsOriginal415Access.Resource(CollectorsOriginal415Names.TraceBeam), declared);
        var worker = registry.PrepareBeam(CollectorsOriginal415Access.Resource(CollectorsOriginal415Names.WorkerBeam), declared);
        CollectorsOriginal415Access.Require(literal.Name == CollectorsOriginal415Names.Literal && trace.Name == CollectorsOriginal415Names.Trace
            && worker.Name == CollectorsOriginal415Names.Module && literal.NativePrimitiveExports.Count == 2
            && trace.NativePrimitiveExports.Count == 2 && worker.NativePrimitiveExports.Count == 0);
        CollectorsOriginal415Access.Require(registry.PublishPreparedBeam([literal, trace, worker]).Status == BeamPublicationStatus.Published);
        foreach (var image in new[] { literal, trace, worker })
            foreach (var export in registry.Exports.Where(e => e.Module == image.Name))
                CollectorsOriginal415Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
        // This ordinary-entry witness does not spawn designated start/0 roles.
        // The unchanged compiled child/3 remains an ordinary runtime process.
    }
}
