using static Erlang.Tests.OriginalAtomics588AdmissionReader;
using static Erlang.Tests.OriginalAtomics588Names;

namespace Erlang.Tests;

internal static class OriginalAtomics588Images
{
    internal static PreparedBeamImage[] Prepare(ProcessRuntime runtime, string repository, OriginalAtomics588Admission receipt)
    {
        var images = receipt.Stock.Select(row =>
 {
     byte[] bytes = File.ReadAllBytes(Resolve(repository, row.BeamPath));
     var image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(DynamicApplyBoundary520Imports.Read(bytes)));
     Require(image.Name == row.Module && image.OriginalBeamSha256 == row.BeamSha256 && !image.HasOnLoad);
     var actual = image.NativePrimitiveExports.Select(f => (f.Name, f.Arity)).ToHashSet();
     Require(row.Module == Atomics ? actual.SetEquals(Natives) : actual.Count == 0);

     return image;
 }).ToArray();
        RequireGenuineLoadReceipts590(repository, receipt, images);
        RequireOwnersMetadata(repository, receipt, images);

        return images;
    }

    internal static void RequireOwners(
        ProcessRuntime runtime,
        IReadOnlyList<PreparedBeamImage> images,
        string repository,
        OriginalAtomics588Admission receipt
    )
    {
        var rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Resolve(repository, receipt.StockOwnersBeforePath)))).ToArray();
        Require(rows.Length == images.Count);
        for (int index = 0; index < images.Count; index++)
        {
            var image = images[index];
            var original = (TupleTerm)rows[index];
            var expected = Cons.Items(original.Items[OriginalAtomics588MetadataNames.OwnerExportsIndex]).Select(x =>
 {
     var p = (TupleTerm)x;

     return (((Atom)p.Items[0]).Name, (int)((Integer)p.Items[1]).Value);
 }).ToHashSet();
            var exports = runtime.Modules.Exports.Where(e => e.Module == image.Name).ToArray();
            Require(exports.Length > 0 && image.IsConsumed && expected.SetEquals(exports.Select(e => (e.Function, e.Arity))));
            foreach (var entry in exports)
                Require(ReferenceEquals(runtime.Modules.SourceOwner(entry.Module, entry.Function, entry.Arity), image));
        }
    }

    internal static async Task CaptureOwners(
        ProcessContext context,
        ProcessRuntime runtime,
        IReadOnlyList<PreparedBeamImage> images,
        string output,
        string phase
    )
    {
        var owners = new List<Term>();
        foreach (var image in images)
        {
            var values = new List<Term>();
            foreach (string field in new[] { OriginalAtomics588MetadataNames.Md5, OriginalAtomics588MetadataNames.Compile, OriginalAtomics588MetadataNames.Attributes, OriginalAtomics588MetadataNames.Exports })
                values.Add(await runtime.Modules.Call(
                    context,
                    image.Name,
                    OriginalAtomics588MetadataNames.ModuleInfo,
                    Term.A(field)
                ));
            Require(values[0].Equals(image.NativeMd5) && values[1].Equals(image.Compilation) && values[2].Equals(image.Attributes));
            var actual = Cons.Items(values[3]).Select(x =>
{
    var pair = (TupleTerm)x;

    return (((Atom)pair.Items[0]).Name, (int)((Integer)pair.Items[1]).Value);
}).ToHashSet();
            Require(actual.SetEquals(runtime.Modules.Exports.Where(e => e.Module == image.Name).Select(e => (e.Function, e.Arity))));
            owners.Add(Term.Tuple(
                Term.A(image.Name),
                Term.String(image.OriginalBeamSha256),
                values[0],
                values[1],
                values[2],
                values[3]
            ));
        }
        Term minor = Term.Tuple(Term.A(OriginalAtomics588MetadataNames.MinorVersion), Term.I(OriginalAtomics588MetadataNames.EncodingMinorVersion));
        var encoded = (BitString)await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            OriginalAtomics588MetadataNames.Encode,
            Cons.From(owners),
            Term.List(minor)
        );
        await File.WriteAllBytesAsync(
            Path.Combine(output, OriginalAtomics588MetadataNames.OwnersFilePrefix + phase + OriginalAtomics588MetadataNames.EtfExtension),
            encoded.ToArray()
        );
    }

    internal static void RequireWorkers(string repository, OriginalAtomics588Admission receipt, IReadOnlyList<PreparedBeamImage> images)
    {
        var rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Resolve(repository, receipt.WorkerOwnersPath)))).ToArray();
        Require(rows.Length == images.Count);
        foreach (var image in images)
        {
            var matching = rows.OfType<TupleTerm>().Where(row => row.Items.Count == OriginalAtomics588MetadataNames.WorkerOwnerArity && row.Items[0].Equals(Term.A(image.Name))).ToArray();
            Require(matching.Length == 1);
            var row = matching[0];
            Require(row.Items[OriginalAtomics588MetadataNames.WorkerMd5Index].Equals(image.NativeMd5) && row.Items[OriginalAtomics588MetadataNames.WorkerCompilationIndex].Equals(image.Compilation) && row.Items[OriginalAtomics588MetadataNames.WorkerAttributesIndex].Equals(image.Attributes));
        }
    }

    private static void RequireGenuineLoadReceipts590(string repository, OriginalAtomics588Admission receipt, PreparedBeamImage[] images)
    {
        var initial = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Resolve(repository, receipt.InitialPreloadedOwnersPath)))).ToArray();
        var loads = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Resolve(repository, receipt.StockLoadReceiptsPath)))).ToArray();
        Require(initial.Length == images.Length && loads.Length == images.Length);
        for (int i = 0; i < images.Length; i++)
        {
            Require(initial[i] is TupleTerm { Items.Count: OriginalAtomics588MetadataNames.InitialOwnerRowArity } && loads[i] is TupleTerm { Items.Count: OriginalAtomics588MetadataNames.LoadedRowArity });
            var before = (TupleTerm)initial[i];
            var load = (TupleTerm)loads[i];
            var image = images[i];
            Require(before.Items[0].Equals(Term.A(image.Name)) && before.Items[OriginalAtomics588MetadataNames.PreloadedWhichIndex].Equals(Term.A(OriginalAtomics588MetadataNames.Preloaded))
                && before.Items[OriginalAtomics588MetadataNames.OwnerFileMd5Index].Equals(image.NativeMd5) && before.Items[OriginalAtomics588MetadataNames.OwnerRunningMd5Index].Equals(image.NativeMd5)
                && load.Items[0].Equals(Term.A(image.Name)) && load.Items[1].Equals(Term.A(ErlangBooleanAtoms.True))
                && load.Items[2].Equals(Term.Tuple(Term.A(OriginalAtomics588MetadataNames.LoadedModuleTag), Term.A(image.Name)))
                && load.Items[OriginalAtomics588MetadataNames.LoadedFirstMd5Index].Equals(image.NativeMd5) && load.Items[OriginalAtomics588MetadataNames.LoadedSecondMd5Index].Equals(image.NativeMd5)
                && load.Items[OriginalAtomics588MetadataNames.LoadedTraceRowsIndex] is Cons);
        }
    }

    private static void RequireOwnersMetadata(string repository, OriginalAtomics588Admission receipt, PreparedBeamImage[] images)
    {
        Term value = ExternalTermFormat.Decode(File.ReadAllBytes(Resolve(repository, receipt.StockOwnersBeforePath)));
        var rows = Cons.Items(value).ToArray();
        Require(rows.Length == images.Length);
        for (int i = 0; i < rows.Length; i++)
        {
            Require(rows[i] is TupleTerm { Items.Count: OriginalAtomics588MetadataNames.FileOwnerRowArity });
            var row = (TupleTerm)rows[i];
            var image = images[i];
            var stock = receipt.Stock[i];
            Require(row.Items[0].Equals(Term.A(image.Name))
                && row.Items[1].Equals(Term.String(stock.ProviderPath))
                && row.Items[2] is Cons
                && row.Items[3] is Cons
                && row.Items[4].Equals(image.NativeMd5) && row.Items[5].Equals(image.NativeMd5)
                && row.Items[OriginalAtomics588MetadataNames.OwnerCompilationIndex].Equals(image.Compilation) && row.Items[OriginalAtomics588MetadataNames.OwnerAttributesIndex].Equals(image.Attributes));
            var expected = Cons.Items(row.Items[OriginalAtomics588MetadataNames.OwnerExportsIndex]).Select(x =>
 {
     Require(x is TupleTerm { Items.Count: 2 } p && p.Items[0] is Atom && p.Items[1] is Integer);
     var pair = (TupleTerm)x;

     return (((Atom)pair.Items[0]).Name, (int)((Integer)pair.Items[1]).Value);
 }).ToHashSet();
            // Exact source exports are checked after publication as well;
            // actual runtime source owner cannot be substituted by a C# facet.
            Require(expected.Contains((OriginalAtomics588MetadataNames.ModuleInfo, 0)) && expected.Contains((OriginalAtomics588MetadataNames.ModuleInfo, 1)));
        }
    }
}
