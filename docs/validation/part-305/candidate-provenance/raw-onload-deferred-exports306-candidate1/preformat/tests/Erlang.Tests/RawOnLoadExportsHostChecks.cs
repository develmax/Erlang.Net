using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

public static class RawOnLoadExportsHostChecks
{
    public static async Task Run()
    {
        using JsonDocument projection = JsonDocument.Parse(Resource(RawOnLoadExportsHostNames.Projection));
        foreach (JsonElement beam in projection.RootElement.GetProperty(RawOnLoadExportsHostNames.BeamResources).EnumerateArray())
        {
            byte[] bytes = Resource(beam.GetProperty(RawOnLoadExportsHostNames.Resource).GetString()!);
            Require(Convert.ToHexString(SHA256.HashData(bytes)) == beam.GetProperty(RawOnLoadExportsHostNames.Hash).GetString(), RawOnLoadExportsHostNames.Hash);
        }
        JsonElement cases = projection.RootElement.GetProperty(RawOnLoadExportsHostNames.Cases);
        Require(cases.GetArrayLength() == RawOnLoadExportsHostParameters.OriginalCases, RawOnLoadExportsHostNames.Cases);
        foreach (JsonElement test in cases.EnumerateArray())
            await Observe(test);
        await BeamAllocationHostChecks.Run();
    }

    private static async Task Observe(JsonElement test)
    {
        string name = test.GetProperty(RawOnLoadExportsHostNames.Name).GetString()!;
        bool old = !name.StartsWith(RawOnLoadExportsHostNames.Fresh, StringComparison.Ordinal);
        bool deleted = name.StartsWith(RawOnLoadExportsHostNames.DeletedOld, StringComparison.Ordinal);
        bool clear = !name.EndsWith(RawOnLoadExportsHostNames.DirectTrue, StringComparison.Ordinal);
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.HasExport(RawOnLoadExportsHostNames.OsModule, RawOnLoadExportsHostNames.GetEnvironment, 1), name);
        // A fixture-only callback for the exact getenv import, not a public API implementation.
        runtime.Modules.Register(
            RawOnLoadExportsHostNames.OsModule,
            RawOnLoadExportsHostNames.GetEnvironment,
            1,
            (_, arguments) =>
            {
                Require(arguments.Count == 1 && arguments[0].Equals(Term.String(RawOnLoadExportsHostNames.EnvironmentKey)), name);

                return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.False));
            }
        );
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                FunctionTerm? oldFun = null;
                if (old)
                {
                    Require(runtime.Modules.PublishPreparedBeam(runtime.Modules.PrepareBeam(Resource(RawOnLoadExportsHostNames.OldRole + RawOnLoadExportsHostNames.BeamExtension))).Status == BeamPublicationStatus.Published, name);
                    oldFun = (FunctionTerm)await runtime.Modules.Call(context, RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Capture);
                    if (deleted)
                        runtime.Modules.DeleteModule(RawOnLoadExportsHostNames.Module);
                }
                Term prepared = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    BeamLoaderAdmissionNames.Prepare,
                    Term.A(RawOnLoadExportsHostNames.Module),
                    new BitString(Resource(RawOnLoadExportsHostNames.NewRole + RawOnLoadExportsHostNames.BeamExtension))
                );
                await runtime.Modules.Call(context, ErlangModuleNames.Module, BeamLoaderAdmissionNames.Finish, Cons.From([prepared]));
                Require((await runtime.Modules.InvokeLoaderOnLoad(context, Term.A(RawOnLoadExportsHostNames.Module))).Equals(Term.A(BeamOnLoadHostNames.Ok)), name);
                var pendingFun = (FunctionTerm)context.Dictionary[Term.A(RawOnLoadExportsHostNames.Held)];
                if (clear)
                {
                    await Finish(context, false);
                    await Finish(context, false);
                    Require(runtime.Modules.IsModuleLoaded(RawOnLoadExportsHostNames.Module) == (old && !deleted), name);
                    if (old && !deleted)
                    {
                        JsonElement expectedOld = test.GetProperty(RawOnLoadExportsHostNames.Final).GetProperty(RawOnLoadExportsHostNames.OldFun);
                        await Capture(() => runtime.Modules.Call(context, RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Body), expectedOld, name);
                    }
                }
                await Finish(context, true);
                JsonElement expected = test.GetProperty(RawOnLoadExportsHostNames.Final);
                Require(runtime.Modules.IsModuleLoaded(RawOnLoadExportsHostNames.Module) == Boolean(expected, RawOnLoadExportsHostNames.ModuleLoaded), name);
                Require(runtime.Modules.HasOldCode(RawOnLoadExportsHostNames.Module) == Boolean(expected, RawOnLoadExportsHostNames.OldCode), name);
                Require(runtime.Modules.HasExport(RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Body, 0) == Boolean(expected, RawOnLoadExportsHostNames.BodyExported), name);
                Require(runtime.Modules.HasExport(RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Capture, 0) == Boolean(expected, RawOnLoadExportsHostNames.CaptureExported), name);
                var expectedExports = expected.GetProperty(RawOnLoadExportsHostNames.Exports)[1][1][1];
                var signatures = expectedExports.EnumerateArray().Select(entry => (entry[1][0][1].GetString()!, entry[1][1][1].GetInt32())).ToHashSet();
                var actualExports = Cons.Items(runtime.Modules.ModuleInformation(RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.ModuleExportsKey))
                    .Select(term =>
                    {
                        var entry = (TupleTerm)term;

                        return (((Atom)entry.Items[0]).Name, (int)((Integer)entry.Items[1]).Value);
                    }).ToHashSet();
                Require(signatures.SetEquals(actualExports), name);
                await Capture(() => runtime.Modules.Call(context, RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Body), expected.GetProperty(RawOnLoadExportsHostNames.Body), name);
                await Capture(() => pendingFun.Invoke(context, []), expected.GetProperty(RawOnLoadExportsHostNames.PendingFun), name);
                if (oldFun is not null)
                    await Capture(() => oldFun.Invoke(context, []), expected.GetProperty(RawOnLoadExportsHostNames.OldFun), name);
                else
                    Require(expected.GetProperty(RawOnLoadExportsHostNames.OldFun)[1].GetString() == RawOnLoadExportsHostNames.NoHolder, name);
                result.TrySetResult();
            }
            catch (Exception failure)
            {
                result.TrySetException(failure);
                throw;
            }

            return Nil.Value;
        });
        await result.Task.WaitAsync(RawOnLoadExportsHostParameters.Deadline);
        await actor.Completion.WaitAsync(RawOnLoadExportsHostParameters.Deadline);
        Console.WriteLine(RawOnLoadExportsHostNames.Passed + name);
    }

    private static bool Boolean(JsonElement snapshot, string key)
        => snapshot.GetProperty(key)[1].GetString() == ErlangBooleanAtoms.True;

    private static async ValueTask Finish(ProcessContext context, bool keep)
    {
        Term value = await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Term.A(RawOnLoadExportsHostNames.Module),
            Term.A(keep ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
        );
        Require(value.Equals(Term.A(ErlangBooleanAtoms.True)), BeamLoaderAdmissionNames.FinishOnLoad);
    }

    private static async Task Capture(Func<ValueTask<Term>> invoke, JsonElement expected, string name)
    {
        JsonElement fields = expected[1];
        try
        {
            Term actual = await invoke();
            Require(fields[0][1].GetString() == RawOnLoadExportsHostNames.Return && fields[1][0].GetString() == RawOnLoadExportsHostNames.Atom && actual.Equals(Term.A(fields[1][1].GetString()!)), name);
        }
        catch (ErlangException failure)
        {
            Require(fields[0][1].GetString() == RawOnLoadExportsHostNames.Exception && fields[1][1].GetString() == failure.ExceptionClass && fields[2][0].GetString() == RawOnLoadExportsHostNames.Atom && failure.Reason.Equals(Term.A(fields[2][1].GetString()!)), name);
        }
    }

    internal static byte[] Resource(string name)
    {
        Assembly assembly = typeof(RawOnLoadExportsHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(value => value.EndsWith(RawOnLoadExportsHostNames.ResourcePrefix + name, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    private static void Require(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(RawOnLoadExportsHostNames.Broken + name);
    }
}
