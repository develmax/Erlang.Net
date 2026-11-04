using static Erlang.Tests.UnicodeUtilBackendNames;

namespace Erlang.Tests;

internal static class UnicodeUtilBackendChecks
{
    internal static async Task Run(bool managed, int partition)
    {
        Require(partition >= 0 && partition < Partitions);
        var choices = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists"),
            managed ? RuntimeModuleSelection.Managed(Module) : RuntimeModuleSelection.Beam(Module,Read(StringBackendNames.Prefix,"unicode_util.beam"),["lists"]),
            managed ? RuntimeModuleSelection.Managed("unicode",["lists",Module]) : RuntimeModuleSelection.Beam("unicode",Read(StringBackendNames.Prefix,"unicode.beam"),["lists",Module])]);
        await using var runtime = await ProcessRuntime.CreateAsync(choices, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        var receipt = runtime.LibrarySelection.Modules.Single(row => row.Module == Module);
        Require(receipt.Backend == (managed ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam));
        Term declared = ExternalTermFormat.Decode(Read(Prefix, "exports.etf"));
        Require(UnicodeUtilModule.Exports.ToHashSet().SetEquals(Cons.Items(declared).Cast<TupleTerm>()
            .Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value))));
        Require(UnicodeUtilModule.Exports.All(signature => receipt.Exports.Any(export => export.Name == signature.Name && export.Arity == signature.Arity)));
        if (managed)
        {
            Require(runtime.LibrarySelection.Modules.All(row => row.Backend == RuntimeModuleBackend.Managed && row.InputSha256 is null));
            Require(UnicodeUtilModule.Exports.All(signature => runtime.Modules.SourceOwner(Module, signature.Name, signature.Arity) is null));
        }
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term normalized = await runtime.Modules.Call(
                context,
                "unicode",
                "characters_to_nfc_list",
                Term.List(Term.I(65), Term.I(778))
            );
            Require(normalized.Equals(Term.List(Term.I(197))));
            if (managed)
            {
                Term nested = Term.List(Term.I(65));
                for (int depth = 0; depth < DeepNesting; depth++)
                {
                    nested = Term.List(nested);
                }
                foreach (string function in new[] { "cp", "gc", "nfc" })
                {
                    Require((await runtime.Modules.Call(
                        context,
                        Module,
                        function,
                        nested
                    )).Equals(Term.List(Term.I(65))));
                }
            }
            // Original OTP transition-detail rows: these Balinese consonants
            // lie inside a broad range overlapped by singleton clauses.
            foreach (int cp in UnicodeUtilBackendNames.BalineseConsonants)
            {
                Term cluster = Term.List(Term.I(cp), Term.I(UnicodeUtilBackendNames.DevanagariVirama), Term.I(UnicodeUtilBackendNames.DevanagariKa));
                Require((await runtime.Modules.Call(
                    context,
                    Module,
                    "gc",
                    cluster
                )).Equals(Term.List(cluster)));
            }

            return true;
        });
        using PreparedBeamImage observer = runtime.Modules.PrepareBeam(
            Read(Prefix, Observer + ".beam"),
            new BeamDependencySelection([Observer, Module, "unicode", "erlang", "lists", "binary", "file", "filename"])
        );
        await runtime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published));
        byte[] corpusBytes = Read(Prefix, "contracts.etf");
        Term[] allRows = await actor.InvokeAsync(context => Task.FromResult(Cons.Items(ExternalTermFormat.Decode(corpusBytes)).ToArray()));
        Require(allRows.Length == CoreCases);
        Term[] rows = allRows.Where((_, index) => index % Partitions == partition).ToArray();
        foreach (Term[] batch in rows.Chunk(Batch))
        {
            await actor.InvokeAsync(async context =>
            {
                foreach (TupleTerm row in batch)
                {
                    var sent = (TupleTerm)await runtime.Modules.Call(
                        context,
                        Observer,
                        "worker",
                        row.Items[0],
                        row.Items[1],
                        context.Self
                    );
                    Require(sent.Items[1].Equals(row.Items[2]));
                }

                return true;
            });
        }
    }

    private static byte[] Read(string prefix, string file)
    {
        using Stream input = typeof(UnicodeUtilBackendChecks).Assembly.GetManifestResourceStream(prefix + file)!;
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
