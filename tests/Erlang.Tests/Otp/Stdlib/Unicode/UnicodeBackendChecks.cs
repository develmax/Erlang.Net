using System.Security.Cryptography;
using static Erlang.Tests.UnicodeBackendNames;

namespace Erlang.Tests;

internal static class UnicodeBackendChecks
{
    internal static async Task Run(bool managed)
    {
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed("lists"),
            RuntimeModuleSelection.Beam("unicode_util", Original("unicode_util"), ["lists"]),
            managed ? RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])
                : RuntimeModuleSelection.Beam("unicode", Original("unicode"), ["lists", "unicode_util"]),
            RuntimeModuleSelection.Managed("string", ["erlang", "lists", "unicode", "unicode_util"])
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        RuntimeLibraryModuleSnapshot receipt = runtime.LibrarySelection.Modules.Single(module => module.Module == "unicode");
        Require(receipt.Backend == (managed ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam));
        Require(receipt.NativePrimitives.Count == 3 && UnicodeModule.Exports.ToHashSet().SetEquals(
            receipt.Exports.Where(export => export.Name != "module_info").Select(export => (export.Name, export.Arity))));
        foreach (var export in UnicodeModule.Exports)
        {
            Require(managed ? runtime.Modules.SourceOwner("unicode", export.Name, export.Arity) is null
                : runtime.Modules.SourceOwner("unicode", export.Name, export.Arity) is PreparedBeamImage);
        }
        using PreparedBeamImage observer = runtime.Modules.PrepareBeam(Read(Observer + ".beam"),
            new BeamDependencySelection(["unicode", "erlang", "lists", "binary", "file", "filename", Observer]));
        await runtime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published));
        Term[] rows = Cons.Items(ExternalTermFormat.Decode(Read("contracts.etf"))).ToArray();
        Require(rows.Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, Cons.Items(row.Items[1]).Count())).ToHashSet().SetEquals(UnicodeModule.Exports));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (Term[] batch in rows.Chunk(32))
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
        await actor.InvokeAsync(async context =>
        {
            Term backing = await runtime.Modules.Call(context, Observer, "backing");
            Require(backing.Equals(ExternalTermFormat.Decode(Read("backing.etf"))));
            Term upper = await runtime.Modules.Call(
                context,
                "string",
                "uppercase",
                Term.String("Straße")
            );
            Require(upper.Equals(Term.String("STRASSE")));
            Term equal = await runtime.Modules.Call(
                context,
                "string",
                "equal",
                Term.List(Term.I(65), Term.I(778)),
                Term.List(Term.I(197)),
                Term.A("false"),
                Term.A("nfc")
            );
            Require(equal.Equals(Term.A("true")));

            return true;
        });
    }

    internal static async Task Refusals()
    {
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.Exports.Any(export => export.Module == "unicode"));
        bool refused = false;
        try
        {
            _ = RuntimeModuleSelection.Managed("unicode");
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        refused = false;
        try
        {
            _ = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])]);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused && !runtime.Modules.Exports.Any(export => export.Module == "unicode"));
    }

    private static byte[] Original(string module)
    {
        using Stream input = typeof(UnicodeBackendChecks).Assembly.GetManifestResourceStream(StringBackendNames.Prefix + module + ".beam")!;
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == StringBackendNames.Digests[module]);

        return bytes;
    }

    private static byte[] Read(string file)
    {
        using Stream input = typeof(UnicodeBackendChecks).Assembly.GetManifestResourceStream(ResourcePrefix + file)!;
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(Failure);
        }
    }
}
