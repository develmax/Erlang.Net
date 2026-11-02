using Erlang.Compiler;
using System.Security.Cryptography;
using static Erlang.Tests.InlineInteger360Names;

namespace Erlang.Tests;

internal static class InlineInteger360Checks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        foreach (bool enabled in new[] { false, true })
            try
            {
                await Profile(enabled);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }

    private static async Task Profile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        await using var secondRuntime = OwnedGuest506RuntimeFactory.Create(enabled);
        await DebugSame420HostAccess.Initialize(runtime);
        await DebugSame420HostAccess.Initialize(secondRuntime);
        var dependencies = new BeamDependencySelection([InlineInteger360Names.Erlang, "erts_debug"]);
        await using var first = runtime.Modules.PrepareBeam(Resource(V1, V1Hash), dependencies);
        await using var next = runtime.Modules.PrepareBeam(Resource(V2, V2Hash), dependencies);
        await using var second = secondRuntime.Modules.PrepareBeam(Resource(V1, V1Hash), dependencies);
        await runtime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(first).Status == BeamPublicationStatus.Published));
        await secondRuntime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(second).Status == BeamPublicationStatus.Published));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var secondActor = new RuntimeNodeFoundationHostActor(secondRuntime);
        Integer old = await Store(actor, OldKey);
        Integer isolated = await Store(secondActor, SecondKey);
        object oldDomain = LiteralDomain(old), isolatedDomain = LiteralDomain(isolated);
        Require(!ReferenceEquals(old, isolated) && !ReferenceEquals(oldDomain, isolatedDomain));
        Require(ReferenceEquals(oldDomain, Property(runtime.Modules.CurrentCodeImage(TestModule)!, ImageDomain)));
        Task oldRetirement = (Task)Property(oldDomain, Retirement);
        Task isolatedRetirement = (Task)Property(isolatedDomain, Retirement);
        Term before = await Outcome(actor);
        await runtime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(next).Status == BeamPublicationStatus.Published));
        Integer current = await Store(actor, NewKey);
        object currentDomain = LiteralDomain(current);
        Task currentRetirement = (Task)Property(currentDomain, Retirement);
        Require(!ReferenceEquals(oldDomain, currentDomain) && !old.Equals(current));
        Require(ReferenceEquals(currentDomain, Property(runtime.Modules.CurrentCodeImage(TestModule)!, ImageDomain)));
        Term after = await Outcome(actor);
        Term actual = Term.Tuple(before, after);
        byte[] expectedBytes = Resource(Original, OriginalHash);
        Require(actual.Equals(ExternalTermFormat.Decode(expectedBytes)));
        Require(ExternalTermFormat.Encode(actual).AsSpan().SequenceEqual(expectedBytes));
        await actor.InvokeAsync(async context =>
        {
            Require(await PurgeOld(context));
            CollectMajor(context);
            Require(!oldRetirement.IsCompleted && old.Value != current.Value);
            Require(context.Dictionary.Remove(Term.A(OldKey)));
            CollectMajor(context);

            return Nil.Value;
        });
        await oldRetirement.WaitAsync(Deadline);
        Stale(old);
        runtime.Modules.DeleteModule(TestModule);
        await actor.InvokeAsync(async context =>
        {
            Require(await PurgeOld(context));
            CollectMajor(context);
            Require(!currentRetirement.IsCompleted && current.Value != isolated.Value);
            Require(context.Dictionary.Remove(Term.A(NewKey)));
            CollectMajor(context);

            return Nil.Value;
        });
        await currentRetirement.WaitAsync(Deadline);
        Stale(current);
        Require(!isolatedRetirement.IsCompleted && isolated.Value == ((Integer)((TupleTerm)before).Items[0]).Value);
        await secondActor.DisposeAsync();
        await secondRuntime.DisposeAsync();
        await isolatedRetirement.WaitAsync(Deadline);
        Stale(isolated);
    }

    private static Task<Integer> Store(RuntimeNodeFoundationHostActor actor, string key)
        => actor.InvokeAsync(async context =>
        {
            _ = await Execution.EvaluateAsync(
                new Expr.Call(InlineInteger360Names.Erlang, Put, [new Expr.Literal(Term.A(key)), new Expr.Call(TestModule, Value, [])]),
                context
            );
            Term actual;
            lock (Field(context.Dictionary, Gate))
                actual = ((Dictionary<Term, Term>)Field(context.Dictionary, Data))[Term.A(key)];
            Require(actual is Integer);
            CollectMajor(context);

            return (Integer)actual;
        });

    private static Task<Term> Outcome(RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync<Term>(async context => Term.Tuple(
            await context.Runtime.Modules.Call(context, TestModule, Value),
            await context.Runtime.Modules.Call(context, TestModule, Same)
        ));

    private static async ValueTask<bool> PurgeOld(ProcessContext context)
        => await (ValueTask<bool>)typeof(ProcessRuntime).GetMethod(Purge, Members)!.Invoke(context.Runtime, [context, TestModule])!;

    private static void CollectMajor(ProcessContext context)
    {
        object heap = Property(context, Heap);
        _ = heap.GetType().GetMethod(Collect, Members)!.Invoke(heap, [true, (ushort)0]);
    }

    private static object LiteralDomain(Integer value) => Property(Property(Property(value, Storage), Owner), Domain);

    private static object Property(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static byte[] Resource(string name, string digest)
    {
        using Stream source = typeof(InlineInteger360Checks).Assembly.GetManifestResourceStream(Prefix + name)!;
        using var output = new MemoryStream();
        source.CopyTo(output);
        byte[] value = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(value)) == digest);

        return value;
    }

    private static void Stale(Integer value)
    {
        try
        {
            _ = value.Value;
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
