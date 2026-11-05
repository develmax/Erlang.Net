using System.Security.Cryptography;
using static Erlang.Tests.StringBackendNames;

namespace Erlang.Tests;

internal static class StringBackendChecks
{
    internal static Task Run(bool managed) => Run(managed, false);

    internal static Task Jaro(bool managed) => Run(managed, true);

    internal static Task ManagedUnicode() => Run(true, false, true);

    private static async Task Run(bool managed, bool jaro, bool managedUnicode = false)
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(Selection(managed, managedUnicode), RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single(module => module.Module == StringModule.Name);
        Require(selected.Backend == (managed ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam));
        Require(selected.InputSha256 == (managed ? null : Digests[StringModule.Name]));
        Require(selected.ManagedAssemblies.Count > 0 && selected.ManagedAssemblies.All(value => value.ModuleVersionId != Guid.Empty));
        Require(runtime.NativePrimitives.Selection.HasFlag(RuntimeNativePrimitives.InternalScalars413));
        Require(StringModule.Exports.ToHashSet().SetEquals(selected.Exports.Where(export => export.Name != "module_info").Select(export => (export.Name, export.Arity))));
        foreach (var export in StringModule.Exports)
        {
            Require(managed ? runtime.Modules.SourceOwner(StringModule.Name, export.Name, export.Arity) is null
                : runtime.Modules.SourceOwner(StringModule.Name, export.Name, export.Arity) is PreparedBeamImage);
        }
        if (managedUnicode)
        {
            Require(runtime.LibrarySelection.Modules.All(module => module.Backend == RuntimeModuleBackend.Managed && module.InputSha256 is null));
        }
        using PreparedBeamImage observer = runtime.Modules.PrepareBeam(
            Read(Observer + ".beam"),
            new BeamDependencySelection(Dependencies.Concat([StringModule.Name, "binary", "file", "filename", Observer]))
        );
        await runtime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published));
        string[] fixtures = jaro ? [JaroFixture] : ["original.etf", "audit-regressions.etf"];
        Term[] rows = fixtures.SelectMany(name => Cons.Items(ExternalTermFormat.Decode(Read(name))))
            .ToArray();
        if (!jaro)
            Require(rows.Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, Cons.Items(row.Items[1]).Count())).Distinct().Count() == StringModule.Exports.Count);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (Term[] batch in rows.Chunk(jaro ? 1 : CasesPerActorCommand))
        {
            await actor.InvokeAsync(async context =>
            {
                foreach (TupleTerm row in batch)
                {
                    var actual = (TupleTerm)await runtime.Modules.Call(
                        context,
                        Observer,
                        "run",
                        row.Items[0],
                        row.Items[1]
                    );
                    var expected = (TupleTerm)row.Items[2];
                    Require(expected.Items[0].Equals(actual.Items[0]) && expected.Items[1].Equals(actual.Items[1]));
                    if (expected.Items.Count > 2)
                    {
                        Require(expected.Items[2].Equals(actual.Items[2]) && StringResourceComparison.StacksEqual(expected, actual));
                        Require(StringResourceComparison.ControlsRejectAlterations(expected, actual));
                    }
                }

                return true;
            });
        }
        if (!jaro)
            await actor.InvokeAsync(async context =>
        {
            await StringBackendResourceChecks.Run(context, managed);

            return true;
        });
    }

    internal static async Task Refusals()
    {
        Require(typeof(RuntimeModuleSelection).GetMethod(nameof(RuntimeModuleSelection.Managed), [typeof(string)]) is not null);
        await using var ordinary = new ProcessRuntime();
        Require(ordinary.Modules.Exports.Count(export => export.Module == StringModule.Name) == 1);
        bool refused = false;
        try
        {
            _ = RuntimeModuleSelection.Managed(StringModule.Name);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        refused = false;
        try
        {
            _ = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(StringModule.Name, Dependencies)]);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused && ordinary.Modules.Exports.Count(export => export.Module == StringModule.Name) == 1);
    }

    internal static RuntimeLibrarySelection Selection(bool managed, bool managedUnicode = false) => new([
        RuntimeModuleSelection.Managed("erlang"),
        RuntimeModuleSelection.Managed("lists"),
        managedUnicode ? RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])
            : RuntimeModuleSelection.Beam("unicode", Original("unicode"), ["lists", "unicode_util"]),
        managedUnicode ? RuntimeModuleSelection.Managed("unicode_util")
            : RuntimeModuleSelection.Beam("unicode_util", Original("unicode_util"), ["lists"]),
        managed ? RuntimeModuleSelection.Managed(StringModule.Name, Dependencies)
            : RuntimeModuleSelection.Beam(StringModule.Name, Original(StringModule.Name), Dependencies)
    ]);

    private static byte[] Original(string module)
    {
        byte[] bytes = Read(module + ".beam");
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == Digests[module]);

        return bytes;
    }

    private static byte[] Read(string name)
    {
        using Stream input = typeof(StringBackendChecks).Assembly.GetManifestResourceStream(Prefix + name)!;
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
