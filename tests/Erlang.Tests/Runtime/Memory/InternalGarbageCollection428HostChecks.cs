using System.Reflection;

namespace Erlang.Tests;

internal static class InternalGarbageCollection428HostChecks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(InternalGarbageCollection428HostNames.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext context, Term value)
        => context.Runtime.Modules.Call(
            context,
            InternalGarbageCollection428HostNames.Module,
            InternalGarbageCollection428HostNames.Function,
            value
        );

    private static ushort MinorCount(ProcessContext context)
    {
        var property = typeof(ProcessContext).GetProperty(
            InternalGarbageCollection428HostNames.PolicyProperty, BindingFlags.Instance | BindingFlags.NonPublic)!;
        object policy = property.GetValue(context)!;

        return (ushort)policy.GetType().GetField(
            InternalGarbageCollection428HostNames.MinorField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(policy)!;
    }

    internal static async Task Kernel()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            using var witness = new PhysicalCollectionWitness(context);
            long majorBefore = witness.Prepare();
            Require((await Call(context, Term.A(InternalGarbageCollection428HostNames.Major))).Equals(Term.A(InternalGarbageCollection428HostNames.True)));
            witness.Verify(majorBefore);
            Require(MinorCount(context) == 0);
            long youngBefore = witness.Prepare();
            Require((await Call(context, Term.A(InternalGarbageCollection428HostNames.Minor))).Equals(Term.A(InternalGarbageCollection428HostNames.True)));
            witness.Verify(youngBefore);
            Require(MinorCount(context) == 1);
            long finalBefore = witness.Prepare();
            Require((await Call(context, Term.A(InternalGarbageCollection428HostNames.Major))).Equals(Term.A(InternalGarbageCollection428HostNames.True)));
            witness.Verify(finalBefore);
            Require(MinorCount(context) == 0);

            return Nil.Value;
        });
    }

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.HasExport(InternalGarbageCollection428HostNames.Module,
            InternalGarbageCollection428HostNames.Function, 1));
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry);
            RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry);
        });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool refused = false;
        try
        {
            RuntimeBootstrapInternalGarbageCollection428.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (Term value in new Term[] { Term.I(0), Nil.Value, Term.A(InternalGarbageCollection428HostNames.True), Term.Tuple() })
            {
                ushort before = MinorCount(context);
                bool rejected = false;
                try
                {
                    await Call(context, value);
                }
                catch (ErlangException error)
                {
                    rejected = error.Reason.Equals(Term.A(InternalGarbageCollection428HostNames.BadArgument));
                }
                Require(rejected && before == MinorCount(context));
            }

            return Nil.Value;
        });
    }

    // Real registered SubBits/BinRef cells; CLR wrappers deliberately stay live.
    internal sealed class PhysicalCollectionWitness : IDisposable
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly object heap;
        private readonly IDisposable frame;
        private readonly int survivorSlot;
        private readonly int garbageSlot;
        private readonly BitString survivor;
        private BitString? garbage;
        private long garbageBits;
        private long garbageReference;

        internal PhysicalCollectionWitness(ProcessContext context)
        {
            heap = Property(context, InternalGarbageCollection428HostNames.Heap);
            frame = (IDisposable)Call(heap, InternalGarbageCollection428HostNames.Enter)!;
            try
            {
                survivorSlot = (int)Call(frame, InternalGarbageCollection428HostNames.Reserve)!;
                garbageSlot = (int)Call(frame, InternalGarbageCollection428HostNames.Reserve)!;
                Call(
                    frame,
                    InternalGarbageCollection428HostNames.Set,
                    survivorSlot,
                    new BitString(Enumerable.Repeat(InternalGarbageCollection428HostNames.SurvivorByte,
                        InternalGarbageCollection428HostNames.BinaryBytes).ToArray())
                );
                survivor = (BitString)Call(frame, InternalGarbageCollection428HostNames.Read, survivorSlot)!;
                Require(Property(survivor, InternalGarbageCollection428HostNames.OwnedStorage) is not null);
            }
            catch
            {
                frame.Dispose();
                throw;
            }
        }

        internal long Prepare()
        {
            Call(
                frame,
                InternalGarbageCollection428HostNames.Set,
                garbageSlot,
                new BitString(new byte[InternalGarbageCollection428HostNames.BinaryBytes])
            );
            garbage = (BitString)Call(frame, InternalGarbageCollection428HostNames.Read, garbageSlot)!;
            object storage = Property(garbage, InternalGarbageCollection428HostNames.OwnedStorage);
            garbageBits = (long)Property(storage, InternalGarbageCollection428HostNames.Identity);
            lock (Property(heap, InternalGarbageCollection428HostNames.HeapGate))
            {
                var descriptors = (System.Collections.IDictionary)Field(heap, InternalGarbageCollection428HostNames.ResourceBits);
                object descriptor = descriptors[garbageBits]!;
                object reference = Property(descriptor, InternalGarbageCollection428HostNames.BinRef);
                garbageReference = (long)Property(Property(reference, InternalGarbageCollection428HostNames.Block),
                    InternalGarbageCollection428HostNames.Identity);
                var blocks = (System.Collections.IDictionary)Field(heap, InternalGarbageCollection428HostNames.Blocks);
                Require(blocks.Contains(garbageBits) && blocks.Contains(garbageReference));
            }
            Require(garbage.ToArray().Length == InternalGarbageCollection428HostNames.BinaryBytes);
            Call(frame, InternalGarbageCollection428HostNames.Clear, garbageSlot);

            return Completed();
        }

        internal void Verify(long before)
        {
            Require(Completed() == checked(before + 1));
            lock (Property(heap, InternalGarbageCollection428HostNames.HeapGate))
            {
                var blocks = (System.Collections.IDictionary)Field(heap, InternalGarbageCollection428HostNames.Blocks);
                Require(!blocks.Contains(garbageBits) && !blocks.Contains(garbageReference));
            }
            bool retired = false;
            try
            {
                _ = garbage!.ToArray();
            }
            catch (ObjectDisposedException)
            {
                retired = true;
            }
            Require(retired);
            Require(survivor.ToArray().SequenceEqual(Enumerable.Repeat(
                InternalGarbageCollection428HostNames.SurvivorByte, InternalGarbageCollection428HostNames.BinaryBytes)));
        }

        private long Completed() => (long)Property(
            Property(heap,
            InternalGarbageCollection428HostNames.GenerationSnapshot),
            InternalGarbageCollection428HostNames.CompletedCollections
        );

        private static object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

        private static object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;

        private static object? Call(object value, string name, params object[] arguments)
            => value.GetType().GetMethod(name, Members)!.Invoke(value, arguments);

        public void Dispose() => frame.Dispose();
    }
}
