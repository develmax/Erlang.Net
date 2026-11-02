using System.Security.Cryptography;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeRecordDefaults313Names;

namespace Erlang.Tests;

internal static class NativeRecordDefaults313BeamChecks
{
    internal static async Task Run(bool privateProfile, NativeRecordDefaults331Scope cleanup)
    {
        ProcessRuntime runtime = cleanup.Own(OwnedGuest506RuntimeFactory.Create(privateProfile));
        await runtime.InitializeModulesAsync(_ => { });
        var dependencies = new BeamDependencySelection([Owner, Consumer, Records, SourceLiteral819Names.DictionaryModule, NativeRecordDefaults331Names.Observer, NativeRecordDefaults331Names.Worker]);
        await PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            PreparedBeamImage first = scope.Prepare(() => registry.PrepareBeam(Resource(OwnerV1Image, OwnerV1Hash), dependencies));
            PreparedBeamImage consumer = scope.Prepare(() => registry.PrepareBeam(Resource(ConsumerImage, ConsumerHash), dependencies));
            PreparedBeamImage fieldObserver = scope.Prepare(() => registry.PrepareBeam(Resource(NativeRecordDefaults331Names.ObserverImage, NativeRecordDefaults331Names.ObserverHash), dependencies));
            PreparedBeamImage fieldWorker = scope.Prepare(() => registry.PrepareBeam(Resource(NativeRecordDefaults331Names.WorkerImage, NativeRecordDefaults331Names.WorkerHash), dependencies));

            return ValueTask.FromResult<Action<ModuleRegistry>>(target => Require(target.PublishPreparedBeam([first, consumer, fieldObserver, fieldWorker]).Status == BeamPublicationStatus.Published));
        }
        );
        PreparedBeamImage next = cleanup.Own(runtime.Modules.PrepareBeam(Resource(OwnerV2Image, OwnerV2Hash), dependencies));
        var actor = cleanup.Own(new RuntimeNodeFoundationHostActor(runtime));
        await NativeRecordField331Checks.Run(runtime, actor);
        Term? before = null;
        await actor.InvokeAsync(async context =>
        {
            await Store(context, OldKey, new Expr.Call(Owner, Make, []));
            await Store(context, FunKey, new Expr.Call(Owner, Capture, []));
            await Store(context, ExternalKey, new Expr.Call(Consumer, Make, []));
            before = await runtime.Modules.Call(
                context,
                Consumer,
                Payload,
                Read(context, ExternalKey)
            );
            Term actual = ((NativeRecordTerm)Read(context, OldKey)).Values[0];
            Require(ReferenceEquals(Domain(actual), Property(runtime.Modules.CurrentCodeImage(Owner)!, SourceLiteral819Names.ImageDomain)));

            return true;
        });
        await runtime.InitializeModulesAsync(target => Require(target.PublishPreparedBeam(next).Status == BeamPublicationStatus.Published));
        await actor.InvokeAsync(async context =>
        {
            await Store(context, NewKey, new Expr.Call(Owner, Make, []));
            await Store(context, ExternalKey, new Expr.Call(Consumer, Make, []));
            await Store(
                context,
                CapturedKey,
                new Expr.Apply(new Expr.Call(SourceLiteral819Names.DictionaryModule, Get, [new Expr.Literal(Term.A(FunKey))]), [])
            );
            Term current = await runtime.Modules.Call(
                context,
                Consumer,
                Payload,
                Read(context, NewKey)
            );
            Term external = await runtime.Modules.Call(
                context,
                Consumer,
                Payload,
                Read(context, ExternalKey)
            );
            Term captured = await runtime.Modules.Call(
                context,
                Consumer,
                Payload,
                Read(context, CapturedKey)
            );
            Term retained = await runtime.Modules.Call(
                context,
                Consumer,
                Payload,
                Read(context, OldKey)
            );
            Term explicitValue = Term.Tuple(Term.A(ExplicitTag), Term.List(Term.A(ValueTag)), new BitString(new byte[] { 7, 8 }));
            Term explicitRecord = await runtime.Modules.Call(
                context,
                Owner,
                MakeExplicit,
                explicitValue
            );
            Term explicitResult = await runtime.Modules.Call(
                context,
                Consumer,
                Payload,
                explicitRecord
            );
            Term definition = await runtime.Modules.Call(
                context,
                Records,
                GetDefinition,
                Term.A(Owner),
                Term.A(Record)
            );
            Term outcome = Term.Tuple(
                before!,
                current,
                external,
                captured,
                retained,
                explicitResult,
                definition
            );
            byte[] expected = Resource(OriginalImage, OriginalHash);
            Require(outcome.Equals(ExternalTermFormat.Decode(expected)) && ExternalTermFormat.Encode(outcome).AsSpan().SequenceEqual(expected));
            Term actualCurrent = ((NativeRecordTerm)Read(context, NewKey)).Values[0];
            Term actualOld = ((NativeRecordTerm)Read(context, CapturedKey)).Values[0];
            Require(ReferenceEquals(Domain(actualCurrent), Property(runtime.Modules.CurrentCodeImage(Owner)!, SourceLiteral819Names.ImageDomain)) && !ReferenceEquals(Domain(actualOld), Domain(actualCurrent)));
            foreach (string key in new[] { OldKey, NewKey, ExternalKey, FunKey, CapturedKey }) Require(context.Dictionary.Remove(Term.A(key)));

            return true;
        });
    }

    private static object Domain(Term value) => Property(
        Property(Property(value, SourceLiteral819Names.Storage), SourceLiteral819Names.StorageOwner),
        SourceLiteral819Names.HeapDomain
    );

    private static Term Read(ProcessContext context, string key)
    {
        const System.Reflection.BindingFlags members = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        object dictionary = context.Dictionary;
        object gate = dictionary.GetType().GetField(SourceLiteral819Names.DictionaryGate, members)!.GetValue(dictionary)!;
        lock (gate)
            return ((Dictionary<Term, Term>)dictionary.GetType().GetField(SourceLiteral819Names.DictionaryData, members)!.GetValue(dictionary)!)[Term.A(key)];
    }

    private static async Task Store(ProcessContext context, string key, Expr expression)
        => _ = await Execution.EvaluateAsync(
            new Expr.Call(SourceLiteral819Names.DictionaryModule, SourceLiteral819Names.Put, [new Expr.Literal(Term.A(key)), expression]),
            context
        );

    internal static byte[] Resource(string name, string hash)
    {
        using Stream source = typeof(NativeRecordDefaults313BeamChecks).Assembly.GetManifestResourceStream(ResourcePrefix + name)!;
        using var output = new MemoryStream();
        source.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == hash);

        return bytes;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
