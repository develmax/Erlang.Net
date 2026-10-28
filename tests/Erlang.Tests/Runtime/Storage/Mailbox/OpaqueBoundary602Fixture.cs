using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.OpaqueBoundary602Names;

namespace Erlang.Tests;

internal sealed class OpaqueBoundary602Fixture
{
    internal ProcessRuntime Runtime { get; }
    internal OpaqueBoundary602Atomics Atomics { get; } = new();
    internal object? Payload { get; private set; }
    internal RuntimeNodeFoundationHostActor? Sender { get; private set; }
    internal RuntimeNodeFoundationHostActor? Receiver { get; private set; }
    internal Mailbox Mailbox { get; private set; } = null!;
    private readonly NativeMagicReference576Fixture native;
    private GeneratedModuleArtifact? artifact;
    private object? sourceHeap;
    private IDisposable? sourceFrame;
    private int sourceSlot;
    private ResourceReferenceTerm? sourceNative, sourceAtomics;
    private Term packet = Nil.Value;

    internal OpaqueBoundary602Fixture(NativeMagicReference576Fixture native)
    {
        this.native = native;
        Runtime = native.Runtime;
    }

    internal async Task Initialize(bool includeNative, int messages)
    {
        Require((bool)Property(Runtime, GateReady)); // No reflection gate enable or silent skip.
        artifact = GeneratedModuleCompiler.Compile(Source);
        artifact.Definition.Register(Runtime.Modules);
        Sender = new(Runtime);
        Receiver = new(Runtime);
        ProcessContext sending = await Sender.Ready.WaitAsync(Deadline);
        ProcessContext receiving = await Receiver.Ready.WaitAsync(Deadline);
        Mailbox = receiving.Mailbox;
        sourceHeap = Property(sending, Heap);
        sourceFrame = native.EnterRoot(sourceHeap);
        sourceSlot = (int)Call(sourceFrame, Reserve)!;
        Atomics.Initialize(Runtime);
        if (includeNative)
        {
            Payload = native.CreatePayload();
            Marshal.WriteInt32((nint)Property(Payload, NativePointer), Sentinel);
            var identity = (ResourceReferenceTerm)Call(Runtime.ReferenceResources, Make, Payload)!;
            Call(
                sourceFrame,
                Set,
                sourceSlot,
                Term.Tuple(Atomics.Reference, identity)
            );
            packet = (Term)Call(sourceFrame, Read, sourceSlot)!;
            sourceNative = (ResourceReferenceTerm)((TupleTerm)packet).Items[1];
        }
        else
        {
            Call(
                sourceFrame,
                Set,
                sourceSlot,
                Term.Tuple(Atomics.Reference)
            );
            packet = (Term)Call(sourceFrame, Read, sourceSlot)!;
        }
        sourceAtomics = (ResourceReferenceTerm)((TupleTerm)packet).Items[0];
        for (int index = 0; index < messages; index++)
            await Sender.InvokeAsync(async context =>
            {
                using var host = new NativeTermHostScope524(Runtime);

                return await Runtime.Modules.Call(
                    context,
                    GuestModule,
                    Send,
                    Receiver.Process.Pid,
                    packet
                );
            });
        // Drop the command's completed HOST closure before inspecting roots.
        await Sender.Invoke(_ => Nil.Value);
        Require(Mailbox.Count == messages);
        Require(Works().Length == messages); // Proves actual snapshot queue route, not fallback.
        Atomics.ReleaseCreation();
        if (includeNative) native.ReleaseNative();
        Call(sourceFrame, Clear, sourceSlot);
        Call(
            sourceHeap,
            Major,
            true,
            ushort.MaxValue
        );
        sourceFrame.Dispose();
        sourceFrame = null;
        foreach (ResourceReferenceTerm? source in new[] { sourceAtomics, sourceNative })
            if (source is not null)
            {
                bool stale = false;
                try
                {
                    _ = source.Words;
                }
                catch (ObjectDisposedException)
                {
                    stale = true;
                }
                Require(stale);
            }
        CollectHostTransients(); // Host-finalizer authority only; source guestGC was explicit above.
        Require(Atomics.Count(GuestRoots) == messages);
    }

    internal object[] Works()
    {
        object gate = OpaqueBoundary602Atomics.Field(Mailbox, Gate)!;
        lock (gate)
            return ((IEnumerable)OpaqueBoundary602Atomics.Field(Mailbox, Messages)!).Cast<object>()
                .Select(entry => entry.GetType().GetProperty(Custody, Members)!.GetValue(entry))
                .Where(custody => custody is not null)
                .Select(custody => OpaqueBoundary602Atomics.Field(custody!, Work)!).Distinct(ReferenceEqualityComparer.Instance).ToArray();
    }

    internal long NativeRootCount()
    {
        if (Payload is null) return 0;
        lock (OpaqueBoundary602Atomics.Field(Payload, Gate)!)
            return (long)OpaqueBoundary602Atomics.Field(Payload, NativeRoots)!;
    }

    internal void CheckEscaped(Term captured)
    {
        Require(captured is TupleTerm);
        var tuple = (TupleTerm)captured;
        var atomics = (ResourceReferenceTerm)tuple.Items[0];
        Require(Property(atomics, Storage) is null && ReferenceEquals(Property(atomics, AtomicsBacking), Atomics.Root));
        object carrier = Property(atomics, AtomicsHost);
        Require((bool)Call(carrier, HostOwns, Atomics.Root)!);
        Atomics.WithCell(pointer => Require(Marshal.ReadInt64(pointer) == AtomicsSentinel));
        if (Payload is not null)
        {
            var resource = (ResourceReferenceTerm)tuple.Items[1];
            Require(Property(resource, Storage) is null);
            object actual = typeof(ProcessRuntime).Assembly.GetType(NativeRootType, true)!.GetMethod(DirectNative, Members)!.Invoke(null, [resource, Runtime])!;
            Require(ReferenceEquals(actual, Payload));
            Require(Marshal.ReadInt32((nint)Property(actual, NativePointer)) == Sentinel);
        }
    }

    internal static void ReleaseHost(Term captured)
    {
        var atomics = (ResourceReferenceTerm)((TupleTerm)captured).Items[0];
        ((IDisposable)Property(atomics, AtomicsHost)).Dispose();
    }

    internal async Task Retired()
    {
        await ((Task)Call(Mailbox, Drain)!).WaitAsync(Deadline);
        CollectHostTransients();
        await ((Task)Property(Atomics.Array, AtomicsRetired)).WaitAsync(Deadline);
        Require(!(bool)Property(Atomics.Array, Allocated));
        if (Payload is not null) await ((Task)Property(Payload, NativeRetired)).WaitAsync(Deadline);
    }

    internal async Task Cleanup(List<Exception> errors)
    {
        bool hadFrame = sourceFrame is not null;
        try
        {
            sourceFrame?.Dispose();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        sourceFrame = null;
        if (hadFrame && sourceHeap is not null)
            try
            {
                Call(
                    sourceHeap,
                    Major,
                    true,
                    ushort.MaxValue
                );
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        Atomics.Cleanup(errors);
        if (Receiver is not null) try
            {
                await Receiver.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        if (Sender is not null) try
            {
                await Sender.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        try
        {
            artifact?.Dispose();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
    }

    internal static Task Run(bool native, int messages, Func<OpaqueBoundary602Fixture, Task> body)
        => NativeMagicReference576Fixture.Run(async outer =>
        {
            var fixture = new OpaqueBoundary602Fixture(outer);
            var errors = new List<Exception>();
            try
            {
                await fixture.Initialize(native, messages);
                await body(fixture);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            finally
            {
                await fixture.Cleanup(errors);
            }
            if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
            if (errors.Count != 0) throw new AggregateException(CleanupFailure, errors);
        });

    internal static void CollectHostTransients()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
