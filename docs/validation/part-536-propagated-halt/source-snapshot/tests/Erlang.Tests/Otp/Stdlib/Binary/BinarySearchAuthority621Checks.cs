using Erlang.Differential;
using static Erlang.Tests.BinarySearchAuthority621Names;

namespace Erlang.Tests;

internal static class BinarySearchAuthority621Checks
{
    internal static async Task Run()
    {
        var assembly = typeof(BinarySearchCompiledHostChecks).Assembly;
        string fixtureName = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BinarySearchHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(fixtureName)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), BinarySearchHostNames.Fixture);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        PreparedBeamImage? binary = null;
        await runtime.InitializeModulesAsync(registry =>
        {
            var dependencies = new BeamDependencySelection([LibraryBinaryNames.Module, LibraryBinaryNames.Lists, ErlangModuleNames.Module]);
            PreparedBeamImage[] images = new[] { LibraryBinaryNames.Module, LibraryBinaryNames.Lists }
                .Select(module => registry.PrepareBeam(Image(module), dependencies)).ToArray();
            Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
            binary = images.Single(image => image.Name == LibraryBinaryNames.Module);
            Require(binary.OriginalBeamSha256 == LibraryBinaryNames.Digests[LibraryBinaryNames.Module + LibraryBinaryNames.BeamExtension]);
            Require(binary.NativePrimitiveExports.Count == LibraryBinaryNames.NativePrimitiveCount);
            foreach (var export in registry.Exports.Where(export => export.Module == LibraryBinaryNames.Module))
                Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), binary));
        });
        object execution = Property(binary!, Execution);
        var primitives = (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)Property(execution, NativePrimitives);
        var exactBodies = new Dictionary<(string Function, int Arity), ErlangFunction>();
        foreach (var signature in Signatures)
        {
            Require(primitives.TryGetValue(signature, out var body));
            Require(ReferenceEquals(runtime.Modules.SourceOwner(LibraryBinaryNames.Module, signature.Function, signature.Arity), binary));
            Require(Kind(
                runtime.Modules,
                NativeKind,
                execution,
                body!
            ) == RuntimeRead);
            exactBodies.Add(signature, body!);
        }
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await InvokeExistingFixture(runtime, actor);

        // Same former internal native delegates, now registered through PUBLIC
        // APIs. No new synthetic trust/capability registration is performed.
        // Publishing the original image left the default implementation as old
        // code. Use the real purge API before deleting the current generation.
        Term purged = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ModuleCodeLifecycleNames.PurgeModule,
            Term.A(LibraryBinaryNames.Module)
        ).AsTask());
        Require(purged is Atom { Name: ErlangBooleanAtoms.True });
        runtime.Modules.DeleteModule(LibraryBinaryNames.Module);
        foreach (var pair in exactBodies)
            runtime.Modules.Register(
                LibraryBinaryNames.Module,
                pair.Key.Function,
                pair.Key.Arity,
                pair.Value
            );
        foreach (var pair in exactBodies)
        {
            Require(runtime.Modules.SourceOwner(LibraryBinaryNames.Module, pair.Key.Function, pair.Key.Arity) is null);
            Require(Kind(
                runtime.Modules,
                SelectedKind,
                (LibraryBinaryNames.Module, pair.Key.Function, pair.Key.Arity),
                pair.Value
            ) == Opaque);
        }
        // The seven valid operations still execute. The fixture's final native
        // error-metadata assertion must fail for an ordinary public body: it
        // must not recover the original image's formatter privileges either.
        await InvokeExistingFixture(runtime, actor, opaque: true);
    }

    private static async Task InvokeExistingFixture(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, bool opaque = false)
    {
        try
        {
            Term actual = await actor.InvokeAsync(context => runtime.Modules.Call(context, FixtureModule, BinarySearchAuthority621Names.Run).AsTask());
            Require(!opaque && actual.Equals(Term.Tuple(Term.A(BinarySearchHostNames.Success), Term.A(BinarySearchHostNames.Done))));
        }
        catch (ErlangException error) when (opaque)
        {
            string directory = Path.Combine(OpaqueEvidenceRoot, Guid.NewGuid().ToString(GuidFormat));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, OpaqueReasonFile), ExternalTermFormat.Encode(error.Reason));
            Require(IsOpaqueMetadataMismatch(error.Reason));
        }
    }

    private static bool IsOpaqueMetadataMismatch(Term reason)
    {
        if (reason is not TupleTerm { Items.Count: 2 } mismatch
            || mismatch.Items[0] is not Atom { Name: ErlangErrorReasons.BadMatch }
            || mismatch.Items[1] is not TupleTerm { Items.Count: 3 } caught
            || caught.Items[0] is not Atom { Name: ErlangExceptionClasses.Error }
            || caught.Items[1] is not Atom { Name: ErlangErrorReasons.BadArgument }
            || caught.Items[2] is not Cons { Head: TupleTerm { Items.Count: 4 } frame })
            return false;

        return frame.Items[0] is Atom { Name: LibraryBinaryNames.Module }
            && frame.Items[1] is Atom { Name: BinaryModuleNames.Match }
            && frame.Items[2].Equals(Term.List(Term.A(InvalidSubject), new BitString([FailurePatternByte])))
            && frame.Items[3] is Nil;
    }

    private static byte[] Image(string module)
        => (byte[])typeof(LibraryBinaryHostChecks).GetMethod(ReadImage, Members)!
            .Invoke(null, [module + LibraryBinaryNames.BeamExtension])!;

    private static object Property(object owner, string name)
        => owner.GetType().GetProperty(name, Members)!.GetValue(owner)!;

    private static string Kind(object owner, string method, params object[] arguments)
        => owner.GetType().GetMethod(method, Members)!.Invoke(owner, arguments)!.ToString()!;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
