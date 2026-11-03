using System.Collections;
using System.Reflection;
using System.Text.Json;
using Erlang.Compiler;
using static Erlang.Tests.CanonicalOsEnv401Names;

namespace Erlang.Tests;

internal static class CanonicalOsEnv401Checks
{
    internal static async Task Run()
    {
        if (!OperatingSystem.IsWindows()) return;
        await using ProcessRuntime runtime = await LibraryDependencyOriginalHostChecks.StartRetainedSuite(LibraryDependencyOriginalNames.Os399);
        await runtime.InitializeModulesAsync(registry => new Parser(Source).ParseModule().Register(registry));
        var image = (PreparedBeamImage)runtime.Modules.SourceOwner(Os, Env, 0)!;
        ErlangFunction captured = RequireEnvironmentCapability(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Require((await runtime.Modules.Call(
                context,
                Os,
                PutEnv,
                Term.String(EnvironmentKey),
                Term.String(EnvironmentValue)
            ))
                .Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(NullableProperty(context, CurrentOwned) is null);
            Term expected = await captured(context, []);
            Require(expected is Cons && NullableProperty(expected, Storage) is null);
            Term coldHost = await runtime.Modules.Call(context, Os, Env);
            Require(coldHost.Equals(expected) && NullableProperty(coldHost, Storage) is null);
            _ = await runtime.Modules.Call(context, Probe, Keep);
            Term stored = Stored(context);
            Require(stored is Cons && !ReferenceEquals(stored, expected) && stored.Equals(expected));
            object storage = Property(stored, Storage);
            Require(NullableProperty(Property(storage, Owner), LiteralDomain) is null);

            return true;
        });
        await using (var foreign = new ProcessRuntime())
        await using (var foreignActor = new RuntimeNodeFoundationHostActor(foreign))
        {
            await foreignActor.InvokeAsync(async context =>
            {
                bool rejected = false;
                try
                {
                    _ = await captured(context, []);
                }
                catch (InvalidOperationException error) when (error.Message == ForeignDiagnostic)
                {
                    rejected = true;
                }
                Require(rejected);

                return true;
            });
        }
        string[] declared = JsonSerializer.Deserialize<string[]>(LibraryDependencyOriginalHostChecks.ReadRetainedResource(
            LibraryDependencyOriginalNames.Dependencies)) ?? throw new InvalidOperationException(Failure);
        PreparedBeamImage? next = null;
        // Real preparation is deliberately outside runtime initialization/R.
        // The existing helper owns partial preparation cleanup independently.
        await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
            runtime,
            [LibraryDependencyOriginalHostChecks.ReadRetainedResource(OsBeam)],
            new BeamDependencySelection(declared.Concat([ErlangModuleNames.Module, Os, Probe])),
            (registry, images) =>
            {
                Require(images.Length == 1);
                next = images[0];
                Require(!ReferenceEquals(next, image) && next.OriginalBeamSha256 == image.OriginalBeamSha256);
                Require(ReferenceEquals(RequireEnvironmentCapability(next), captured));
                Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
            }
        );
        Require(next is not null && ReferenceEquals(runtime.Modules.SourceOwner(Os, Env, 0), next));
        Require(runtime.Modules.HasOldCode(Os));
        await actor.InvokeAsync(async context =>
        {
            Term expected = await captured(context, []);
            _ = await runtime.Modules.Call(context, Probe, Keep);
            Term actual = Stored(context);
            Require(actual is Cons && actual.Equals(expected) && NullableProperty(actual, Storage) is not null);
            Require(NullableProperty(Property(Property(actual, Storage), Owner), LiteralDomain) is null);
            Require((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                CodePurgeHostNames.Purge,
                Term.A(Os)
            ))
                .Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(!runtime.Modules.HasOldCode(Os));
            Require(ReferenceEquals(runtime.Modules.SourceOwner(Os, Env, 0), next));
            Require(ReferenceEquals(RequireEnvironmentCapability(next!), captured));

            return true;
        });
        Term host = Term.List(Term.Tuple(Term.String(HostKey), Term.String(HostValue)));
        int calls = 0;
        await runtime.InitializeModulesAsync(registry =>
        {
            Require(registry.DeleteModule(Os).Equals(Term.A(ErlangBooleanAtoms.True)));
            registry.Register(
                Os,
                Env,
                0,
                (context, arguments) =>
            {
                calls++;
                Require(arguments.Count == 0);
                Require(Property(Property(context, CurrentOwned), BindingKind).ToString() == Opaque);

                return ValueTask.FromResult(host);
            }
            );
        });
        await actor.InvokeAsync(async context =>
        {
            _ = await runtime.Modules.Call(context, Probe, Keep);
            Term actual = Stored(context);
            Require(calls == 1 && !ReferenceEquals(actual, host) && actual.Equals(host));
            Require(NullableProperty(Property(Property(actual, Storage), Owner), LiteralDomain) is null);
            // Retaining an old captured delegate creates no new source-image
            // authority: cold direct body keeps its legacy portable snapshot.
            Term untouched = await captured(context, []);
            Require(untouched is Cons && NullableProperty(untouched, Storage) is null && !untouched.Equals(host));

            return true;
        });
    }

    private static ErlangFunction RequireEnvironmentCapability(PreparedBeamImage image)
    {
        object execution = Property(image, CanonicalOsEnv401Names.Execution);
        var primitives = (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)Property(execution, Primitives);
        ErlangFunction captured = primitives[(Env, 0)];
        object capabilities = Property(execution, Capabilities);
        int grants = 0;
        foreach (object pair in (IEnumerable)capabilities)
        {
            grants++;
            Require(ReferenceEquals(Property(pair, Key), captured));
            Require(Property(pair, Value).ToString() == Kind);
        }
        Require(grants == 1);

        return captured;
    }

    private static Term Stored(ProcessContext context)
    {
        object dictionary = context.Dictionary;
        lock (Field(dictionary, Gate))
            return ((Dictionary<Term, Term>)Field(dictionary, Data))[Term.A(DictionaryKey)];
    }

    private static object Property(object owner, string name) => NullableProperty(owner, name)!;

    private static object? NullableProperty(object owner, string name) => owner.GetType().GetProperty(name, Members)!.GetValue(owner);

    private static object Field(object owner, string name) => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
