using System.Reflection;
using System.Security.Cryptography;
using Erlang.Differential;
using static Erlang.Tests.BeamMixedNative580Names;

namespace Erlang.Tests;

internal static class BeamMixedNative580Checks
{
    internal static Task IdentityAndCapture() => WithImage(async (fixture, actor, value) =>
    {
        Check((await Invoke(
            fixture,
            actor,
            Identity,
            value
        )).Equals(Term.A(ErlangBooleanAtoms.True)));
        // Obtaining and adopting the original local-function recipe must succeed
        // before invoking its opaque primitive is tested independently below.
        Check((await Invoke(
            fixture,
            actor,
            CaptureOk,
            value
        )).Equals(Term.A(ErlangBooleanAtoms.True)));
        Check(value is ReferenceTerm);
    });

    internal static Task OpaqueDirectAndClosure() => WithImage(async (fixture, actor, value) =>
    {
        Check((await Invoke(
            fixture,
            actor,
            CaptureOk,
            value
        )).Equals(Term.A(ErlangBooleanAtoms.True)));
        await Refused(() => Invoke(
            fixture,
            actor,
            Opaque,
            value
        ));
        await Refused(() => Invoke(
            fixture,
            actor,
            Closure,
            value
        ));
    });

    internal static Task PublicReregister() => WithImage(async (fixture, actor, value) =>
    {
        Check((await Invoke(
            fixture,
            actor,
            Identity,
            value
        )).Equals(Term.A(ErlangBooleanAtoms.True)));
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(Functions, Members)!.GetValue(fixture.Runtime.Modules)!;
        ErlangFunction former = functions[(NativeModule, Identity, 1)];
        fixture.Runtime.Modules.DeleteModule(NativeModule);
        fixture.Runtime.Modules.Register(
            NativeModule,
            Identity,
            1,
            former
        );
        await Refused(() => Invoke(
            fixture,
            actor,
            Identity,
            value
        ));
    });

    internal static Task PublicPredicateReregister() => WithImage(async (fixture, actor, value) =>
    {
        Check((await Invoke(
            fixture,
            actor,
            CaptureOk,
            value
        )).Equals(Term.A(ErlangBooleanAtoms.True)));
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(Functions, Members)!.GetValue(fixture.Runtime.Modules)!;
        ErlangFunction former = functions[(ErlangModuleNames.Module, IsFunction, 2)];
        // Isolated runtime: PUBLIC republication must not preserve the exact
        // former canonical predicate's private reader capability.
        fixture.Runtime.Modules.DeleteModule(ErlangModuleNames.Module);
        fixture.Runtime.Modules.Register(
            ErlangModuleNames.Module,
            IsFunction,
            2,
            former
        );
        await Refused(() => Invoke(
            fixture,
            actor,
            CaptureOk,
            value
        ));
    });

    private static Task<Term> Invoke(
        NativeMagicReference576Fixture fixture,
        RuntimeNodeFoundationHostActor actor,
        string function,
        Term value
    )
        => actor.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            GuestModule,
            function,
            value
        ));

    private static Task WithImage(Func<NativeMagicReference576Fixture, RuntimeNodeFoundationHostActor, Term, Task> body)
        => NativeMagicReference576Fixture.Run(async fixture =>
        {
            object heap = await fixture.CreateHeap();
            object payload = fixture.CreatePayload();
            var actors = (List<RuntimeNodeFoundationHostActor>)typeof(NativeMagicReference576Fixture)
                .GetField(Actors, Members)!.GetValue(fixture)!;
            RuntimeNodeFoundationHostActor actor = actors.Single();
            byte[] bytes;
            using (Stream stream = typeof(BeamMixedNative580Checks).Assembly.GetManifestResourceStream(Resource)
                ?? throw new InvalidDataException(Resource))
            using (var output = new MemoryStream())
            {
                stream.CopyTo(output);
                bytes = output.ToArray();
            }
            var image = fixture.Runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([NativeModule, ErlangModuleNames.Module]));
            Check(image.Name == NativeModule && image.OriginalBeamSha256 == Convert.ToHexString(SHA256.HashData(bytes)));
            Check(fixture.Runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            using var guest = GeneratedModuleCompiler.Compile(Source);
            guest.Definition.Register(fixture.Runtime.Modules);
            object domain = NativeResource524Access.Property(fixture.Runtime, Domain);
            Term resource = (Term)NativeResource524Access.Call(domain, Make, payload)!;
            // Root before assertions;576 owns all cleanup on every failure path.
            IDisposable roots = fixture.EnterRoot(heap);
            int slot = (int)NativeResource524Access.Call(roots, Reserve)!;
            NativeResource524Access.Call(
                roots,
                Set,
                slot,
                resource
            );
            Term owned = (Term)NativeResource524Access.Call(roots, Read, slot)!;
            await body(fixture, actor, owned);
        });

    private static async Task Refused(Func<Task<Term>> body)
    {
        try
        {
            _ = await body();
        }
        catch (NotSupportedException error) when (error.Message == ScopeRequired)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
