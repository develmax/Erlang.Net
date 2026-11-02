using System.Security.Cryptography;
using Erlang;
using static Erlang.Tests.LibraryProcessNames;

namespace Erlang.Tests;

internal static class LibraryProcessHostChecks
{
    internal static Task Construction() => Compare(
        ConstructionObserver,
        [ConstructionObserver],
        ConstructionPrefix,
        4,
        []
    );

    internal static Task Fields() => Compare(
        FieldsObserver,
        [FieldsObserver],
        FieldsPrefix,
        31,
        []
    );

    internal static Task GenSys() => Compare(
        GenSysObserver,
        [GenSysObserver, GenServer, StatusCallback, StatusLegacy],
        GenPrefix,
        72,
        [Term.A(All)]
    );

    internal static Task Process() => Compare(
        ProcessObserver,
        [ProcessObserver, ProcessSubject],
        ProcPrefix,
        52,
        []
    );

    internal static async Task OwnPid()
    {
        await using ProcessRuntime runtime = await Start([PidObserver, PidWorker]);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            CheckOwnPid(runtime.ImportExternalTerm(Read(PidPrefix + Values)).Term);
            CheckOwnPid(await runtime.Modules.Call(context, PidObserver, Run));
            Term[] original = Cons.Items(runtime.ImportExternalTerm(Read(PidPrefix + Trace)).Term).ToArray();
            Term[] actual = Cons.Items(await runtime.Modules.Call(context, PidObserver, Stacks)).ToArray();
            Require(original.Length == 5 && actual.Length == 5);
            foreach ((Term first, Term second) in original.Zip(actual))
            {
                var left = (TupleTerm)first;
                var right = (TupleTerm)second;
                Require(left.Items.Count == 3 && right.Items.Count == 3
                    && left.Items[0].Equals(right.Items[0]) && left.Items[1].Equals(right.Items[1]));
                CheckOwnPid(left.Items[2]);
                CheckOwnPid(right.Items[2]);
            }

            return true;
        });
    }

    private static async Task Compare(
        string observer,
        string[] fixtures,
        string prefix,
        int count,
        Term[] arguments
    )
    {
        await using ProcessRuntime runtime = await Start(fixtures);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            CheckRows(runtime.ImportExternalTerm(Read(prefix + Values)).Term,
                await runtime.Modules.Call(
                    context,
                    observer,
                    Run,
                    arguments
                ), count);
            Term[] original = Cons.Items(runtime.ImportExternalTerm(Read(prefix + Trace)).Term).ToArray();
            Term[] actual = Cons.Items(await runtime.Modules.Call(
                context,
                observer,
                Stacks,
                arguments
            )).ToArray();
            Require(original.Length == 5 && actual.Length == 5);
            foreach ((Term first, Term second) in original.Zip(actual))
            {
                var left = (TupleTerm)first;
                var right = (TupleTerm)second;
                Require(left.Items.Count == right.Items.Count && left.Items.Count is 2 or 3);
                for (int index = 0; index < left.Items.Count - 1; index++)
                    Require(left.Items[index].Equals(right.Items[index]));
                CheckRows(left.Items[^1], right.Items[^1], count);
            }

            return true;
        });
    }

    private static void CheckRows(Term expected, Term actual, int count)
    {
        Term[] left = Cons.Items(expected).ToArray();
        Term[] right = Cons.Items(actual).ToArray();
        Require(left.Length == count && right.Length == count);
        foreach ((Term first, Term second) in left.Zip(right))
        {
            var original = (TupleTerm)first;
            var current = (TupleTerm)second;
            Require(original.Items[0].Equals(current.Items[0]));
            if (!original.Items[0].Equals(Term.A(CaseWithOwnPid)))
                Require(original.Equals(current));
        }
    }

    private static void CheckOwnPid(Term rows)
    {
        Term[] list = Cons.Items(rows).ToArray();
        Require(list.Length == 1);
        var row = (TupleTerm)list[0];
        var pair = (TupleTerm)row.Items[1];
        var check = (TupleTerm)pair.Items[0];
        var raw = (TupleTerm)pair.Items[1];
        Require(check.Items.Count == 5 && check.Items[0].Equals(Term.A(SourceCheck))
            && check.Items.Skip(2).All(value => value.Equals(Term.A(ErlangBooleanAtoms.True))));
        Require(raw.Items.Count == 5 && raw.Items[0].Equals(Term.A(CompleteRaw))
            && raw.Items[1] is Pid && raw.Items[2].Equals(Term.A(ErlangExceptionClasses.Error))
            && raw.Items[3].Equals(Term.A(ErlangErrorReasons.BadArgument)) && raw.Items[4] is Cons);
    }

    private static async Task<ProcessRuntime> Start(string[] fixtures)
    {
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    Erlang.Otp.OtpModules.Register(registry);
                });
                PreparedBeamImage[] images = Libraries.Concat(fixtures).Select(module => scope.Prepare(() => registry.PrepareBeam(
                    Read(module + Beam), new BeamDependencySelection(Dependencies)))).ToArray();

                return _ =>
                {
                    Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                    foreach (PreparedBeamImage image in images)
                    {
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            Require(ReferenceEquals(registry.SourceOwner(image.Name, export.Function, export.Arity), image));
                        Require(image.NativePrimitiveExports.Count == (image.Name == StringModule ? 1 : 0));
                    }
                };
            }
            );

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    internal static byte[] Read(string name)
    {
        using Stream input = typeof(LibraryProcessHostChecks).Assembly.GetManifestResourceStream(Prefix + name)!;
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == Digests[name]);

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
