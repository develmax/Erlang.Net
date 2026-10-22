using Erlang;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class ListBeamHostChecks
{
    internal static async Task NativeCaller()
    {
        Assembly assembly = typeof(ListBeamHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ListBeamHostNames.NativeCallerResource, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        var module = Erlang.Differential.GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), ListBeamHostNames.NativeCallerResource);
        await using var runtime = new ProcessRuntime();
        Select(runtime);
        module.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term stack = await runtime.Modules.Call(context, ListBeamHostNames.NativeCallerModule, ListBeamHostNames.NativeCallerRun);
            var frames = Cons.Items(stack).Cast<TupleTerm>().ToArray();
            Require(frames.Length >= 2);
            Require(frames[0].Items[0].Equals(Term.A(ListBeamHostNames.Module)) && frames[0].Items[1].Equals(Term.A(ListBeamHostNames.Reverse)));
            Require(frames[1].Items[0].Equals(Term.A(ListBeamHostNames.NativeCallerModule)) && frames[1].Items[1].Equals(Term.A(ListBeamHostNames.NativeCallerProbe)));

            return true;
        });
    }

    internal static async Task Export(string function, int arity, bool startupSelection = false)
    {
        var cases = ListsBeamSmokeCases.Create();
        Require(cases.Count == ListBeamHostNames.Exports - 2 && cases.Select(value => (value.Function, value.Arity)).Distinct().Count() == cases.Count);
        ListsBeamSmokeCase scenario = cases.Single(value => value.Function == function && value.Arity == arity);
        await using var runtime = startupSelection
            ? await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(ListBeamHostNames.Module, LibrarySelectionHostData.Lists())]))
            : new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        PreparedBeamImage image = startupSelection
            ? (PreparedBeamImage)runtime.Modules.SourceOwner(ListBeamHostNames.Module, function, arity)!
            : Select(runtime);
        Require(image.NativePrimitiveExports.Any(value => value.Name == function && value.Arity == arity) == scenario.NativePrimitive);
        await actor.InvokeAsync(async context =>
        {
            Term actual = await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                function,
                scenario.Arguments
            );
            Require(actual.Equals(scenario.Expected));

            return true;
        });
    }

    internal static PreparedBeamImage Select(ProcessRuntime runtime)
    {
        Assembly assembly = typeof(ListBeamHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ListBeamHostNames.Resource, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == ListBeamHostNames.Sha256);
        BeamModule standalone = BeamModule.Prepare(bytes, new BeamDependencySelection([ListBeamHostNames.Module]));
        Require(!standalone.Supports(ListBeamHostNames.Reverse, 2));
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([ListBeamHostNames.Module]));
        Require(image.NativePrimitiveExports.Count == ListBeamHostNames.NativeExports);
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        var exports = runtime.Modules.Exports.Where(value => value.Module == ListBeamHostNames.Module).ToArray();
        Require(exports.Length == ListBeamHostNames.Exports);
        foreach (var export in exports)
            Require(ReferenceEquals(runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity), image));

        return image;
    }

    internal static async Task NativeAndPure()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        PreparedBeamImage image = Select(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.List(Term.I(3), Term.I(1), Term.I(2));
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.Sort,
                input
            )).Equals(Term.List(Term.I(1), Term.I(2), Term.I(3))));
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.Reverse,
                input
            )).Equals(Term.List(Term.I(2), Term.I(1), Term.I(3))));
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.Member,
                Term.I(1),
                input
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Term record = Term.Tuple(Term.I(1), Term.I(2));
            Term records = Term.List(record);
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.KeyFind,
                Term.I(1),
                Term.I(1),
                records
            )).Equals(record));
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.KeyMember,
                Term.I(1),
                Term.I(1),
                records
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.KeySearch,
                Term.I(1),
                Term.I(1),
                records
            )).Equals(Term.Tuple(Term.A(ListBeamHostNames.Value), record)));

            return true;
        });
        Require(image.IsConsumed);
        await actor.InvokeAsync(async context =>
        {
            await runtime.Modules.Call(
                context,
                BeamPreparedHostNames.Erlang,
                BeamPreparedHostNames.PurgeModule,
                Term.A(ListBeamHostNames.Module)
            );

            return true;
        });
        PreparedBeamImage replacement = Select(runtime);
        Require(!ReferenceEquals(image, replacement));
        await actor.InvokeAsync(async context =>
        {
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.Reverse,
                Term.List(Term.I(1)),
                Nil.Value
            )).Equals(Term.List(Term.I(1))));

            return true;
        });
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ListBeamHostNames.Failure);
    }
}
