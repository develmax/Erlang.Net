using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.DictionaryKeys627Names;

namespace Erlang.Tests;

internal static class DictionaryKeys627Checks
{
    internal static async Task Counters()
    {
        var errors = new List<Exception>();
        var fixture = new CountersCustody596Fixture();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(CountersCustody596Names.ProducerKind);
            actor = new(fixture.Runtime);
            await Exercise(
                fixture.Runtime,
                actor,
                fixture.Reference,
                () => { fixture.ReleaseProducer(); producer.Dispose(); producer = null; },
                fixture.Array,
                CountersCustody596Names.Retired
            );
            Require(!(bool)Property(fixture.Array, CountersCustody596Names.Allocated));
        }
        catch (Exception error) { errors.Add(error); }
        finally
        {
            try { producer?.Dispose(); } catch (Exception error) { errors.Add(error); }
            if (actor is not null)
                try { await actor.DisposeAsync().AsTask().WaitAsync(DictionaryKeys605Names.Deadline); }
                catch (Exception error) { errors.Add(error); }
            await fixture.Drain(errors);
        }
        Finish(errors);
    }

    internal static Task Regex() => RegexCustody597Fixture.Run(async fixture =>
    {
        await Exercise(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.Backing!,
            RegexCustody597Names.Retired
        );
        Require((bool)Property(fixture.Backing!, RegexCustody597Names.Freed));
    });

    internal static Task Binary() => BinaryArena598Fixture.Run(new BitString([120]), async fixture =>
    {
        await Exercise(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.BackingOwner,
            BinaryArena598Names.Retired
        );
        Require((bool)Property(fixture.BackingOwner, BinaryArena598Names.Freed));
    });

    private static async Task Exercise(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        ReferenceTerm identity,
        Action releaseProducer,
        object backing,
        string retiredName
    )
    {
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        using var compiled = GeneratedModuleCompiler.Compile(DictionaryKeys605Names.Source, SourceFile);
        compiled.Definition.Register(runtime.Modules);
        ProcessContext owner = await actor.Ready.WaitAsync(DictionaryKeys605Names.Deadline);
        object heap = Property(owner, DictionaryKeys605Names.Heap);
        IDisposable? sourceFrame = null;
        try
        {
            sourceFrame = (IDisposable)Call(heap, DictionaryKeys605Names.Enter)!;
            int slot = (int)Call(sourceFrame, DictionaryKeys605Names.Reserve)!;
            Call(sourceFrame, DictionaryKeys605Names.Set, slot, identity);
            var source = (ReferenceTerm)Call(sourceFrame, DictionaryKeys605Names.Read, slot)!;
            await actor.Invoke(context =>
            {
                context.Dictionary[Term.A(DictionaryKeys605Names.Stored)] = source;

                return true;
            });
            releaseProducer();
            Call(sourceFrame, DictionaryKeys605Names.Clear, slot);
            await actor.Invoke(_ => { Major(heap); return true; });
            bool stale = false;
            try { _ = source.Words; } catch (ObjectDisposedException) { stale = true; }
            Require(stale);
            sourceFrame.Dispose();
            sourceFrame = null;
            Require(!((Task)Property(backing, retiredName)).IsCompleted);
            // Genuine Source get_keys0/1 only returns scalar keys to the Host.
            // No Host resource scope or detached value snapshot is involved.
            Term result = await actor.InvokeAsync(async context =>
                await runtime.Modules.Call(context, DictionaryKeys605Names.Module, DictionaryKeys605Names.KeysEntry));
            Require(result.Equals(Term.I(DictionaryKeys605Names.KeyChecks)));
            Require(!((Task)Property(backing, retiredName)).IsCompleted);
            await actor.Invoke(context =>
            {
                context.Dictionary.Clear();
                Major(heap);

                return true;
            });
            await ((Task)Property(backing, retiredName)).WaitAsync(DictionaryKeys605Names.Deadline);
            GC.KeepAlive(source);
            GC.KeepAlive(identity);
        }
        finally
        {
            // Never hold a temporary frame across actor physical retirement.
            sourceFrame?.Dispose();
        }
    }

    private static void Major(object heap)
        => Call(heap, DictionaryKeys605Names.Major, true, ushort.MaxValue);

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static void Finish(List<Exception> errors)
    {
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1) throw new AggregateException(Failure, errors);
    }
}
