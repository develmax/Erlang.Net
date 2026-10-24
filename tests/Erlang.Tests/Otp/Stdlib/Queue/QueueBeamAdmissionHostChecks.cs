using Erlang;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class QueueBeamAdmissionHostChecks
{
    internal static async Task Direct()
    {
        byte[] bytes = Resource();
        BeamModule beam = BeamModule.Prepare(bytes, new BeamDependencySelection([QueueBeamAdmissionHostNames.Lists]));
        Require(beam.RegistryModuleDependencies.SetEquals([QueueBeamAdmissionHostNames.Lists]));
        Require(beam.Name == QueueBeamAdmissionHostNames.Module);
        Require(beam.InputSha256 == QueueBeamAdmissionHostNames.Sha256);
        Require(beam.Supports(QueueBeamAdmissionHostNames.New, QueueBeamAdmissionHostNames.NewArity));
        Require(beam.Supports(QueueBeamAdmissionHostNames.In, QueueBeamAdmissionHostNames.InArity));
        Require(beam.Supports(QueueBeamAdmissionHostNames.Out, QueueBeamAdmissionHostNames.OutArity));
        object code = typeof(BeamModule).GetField(QueueBeamAdmissionHostNames.Code, QueueBeamAdmissionHostNames.Members)!.GetValue(beam)!;
        var instructions = (IEnumerable)code.GetType().GetProperty(QueueBeamAdmissionHostNames.Instructions, QueueBeamAdmissionHostNames.Members)!.GetValue(code)!;
        Require(instructions.Cast<object>().Count() == QueueBeamAdmissionHostNames.InstructionCount);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var sourceOwner = new object();
        int substitutions = 0;
        RegisterPoison(runtime.Modules, sourceOwner, () => substitutions++);
        await actor.InvokeAsync(async context =>
        {
            await RoundTrip((function, arguments) => beam.Invoke(context, function, arguments));
            Require(ReferenceEquals(
                runtime.Modules.SourceOwner(QueueBeamAdmissionHostNames.Module, QueueBeamAdmissionHostNames.New, QueueBeamAdmissionHostNames.NewArity),
                sourceOwner
            ));
            Require(substitutions == 0);

            return true;
        });
    }

    internal static async Task Published()
    {
        byte[] bytes = Resource();
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([QueueBeamAdmissionHostNames.Lists]));
        Require(prepared.Name == QueueBeamAdmissionHostNames.Module && !prepared.IsConsumed);
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        Require(prepared.IsConsumed);
        foreach (var signature in new[]
        {
            (QueueBeamAdmissionHostNames.New, QueueBeamAdmissionHostNames.NewArity),
            (QueueBeamAdmissionHostNames.In, QueueBeamAdmissionHostNames.InArity),
            (QueueBeamAdmissionHostNames.Out, QueueBeamAdmissionHostNames.OutArity)
        })
            Require(ReferenceEquals(runtime.Modules.SourceOwner(QueueBeamAdmissionHostNames.Module, signature.Item1, signature.Item2), prepared));
        await actor.InvokeAsync(async context =>
        {
            await RoundTrip((function, arguments) => runtime.Modules.Call(
                context,
                QueueBeamAdmissionHostNames.Module,
                function,
                arguments
            ));

            return true;
        });
    }

    private static async Task RoundTrip(Func<string, Term[], ValueTask<Term>> invoke)
    {
        Term empty = Term.Tuple(Nil.Value, Nil.Value);
        Term queue = await invoke(QueueBeamAdmissionHostNames.New, []);
        Require(queue.Equals(empty));
        Require((await invoke(QueueBeamAdmissionHostNames.Out, [queue])).Equals(Term.Tuple(Term.A(QueueBeamAdmissionHostNames.Empty), empty)));
        Term first = Term.A(QueueBeamAdmissionHostNames.First);
        Term second = Term.A(QueueBeamAdmissionHostNames.Second);
        Term one = await invoke(QueueBeamAdmissionHostNames.In, [first, queue]);
        Require(one.Equals(Term.Tuple(Term.List(first), Nil.Value)));
        Require((await invoke(QueueBeamAdmissionHostNames.Out, [one])).Equals(Term.Tuple(Term.Tuple(Term.A(QueueBeamAdmissionHostNames.Value), first), empty)));
        Term two = await invoke(QueueBeamAdmissionHostNames.In, [second, one]);
        Term popped = await invoke(QueueBeamAdmissionHostNames.Out, [two]);
        Require(popped is TupleTerm result && result.Items[0].Equals(Term.Tuple(Term.A(QueueBeamAdmissionHostNames.Value), first)));
        Term remainder = ((TupleTerm)popped).Items[1];
        Require((await invoke(QueueBeamAdmissionHostNames.Out, [remainder])).Equals(Term.Tuple(Term.Tuple(Term.A(QueueBeamAdmissionHostNames.Value), second), empty)));
        Require(queue.Equals(empty));
    }

    private static void RegisterPoison(ModuleRegistry registry, object owner, Action substituted)
    {
        _ = registry.DeleteModule(QueueBeamAdmissionHostNames.Module);
        foreach (var signature in new[]
        {
            (QueueBeamAdmissionHostNames.New, QueueBeamAdmissionHostNames.NewArity),
            (QueueBeamAdmissionHostNames.In, QueueBeamAdmissionHostNames.InArity),
            (QueueBeamAdmissionHostNames.Out, QueueBeamAdmissionHostNames.OutArity)
        })
            registry.RegisterSource(
                QueueBeamAdmissionHostNames.Module,
                signature.Item1,
                signature.Item2,
                (_, _) =>
            {
                substituted();
                throw new InvalidOperationException(QueueBeamAdmissionHostNames.Substitution);
            },
                owner
            );
    }

    private static byte[] Resource()
    {
        Assembly assembly = typeof(QueueBeamAdmissionHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(QueueBeamAdmissionHostNames.Resource, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(bytes.Length == QueueBeamAdmissionHostNames.InputBytes);
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == QueueBeamAdmissionHostNames.Sha256);

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueBeamAdmissionHostNames.Failure);
    }
}
