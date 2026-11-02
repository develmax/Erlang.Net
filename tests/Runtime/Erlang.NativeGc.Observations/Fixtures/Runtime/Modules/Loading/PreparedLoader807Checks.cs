using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.PreparedLoader807Names;

namespace Erlang.Tests;

internal static class PreparedLoader807Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Call(object value, string name, params object[] arguments)
    {
        try
        {
            return value.GetType().GetMethod(name, Members)!.Invoke(value, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static long HeapWords(object heap) => (long)Get(Get(heap, Snapshot), Words);

    private static object CapturePlan(ProcessRuntime runtime, Term references)
        => typeof(ProcessRuntime).Assembly.GetType(Plan, true)!.GetMethod(Capture, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [runtime.ReferenceResources, references])!;

    internal static async Task Run()
    {
        await GuestLifetime(false, false);
        await GuestLifetime(true, false);
        await GuestLifetime(true, true);
        await PrivateNotPurged();
        await BuilderCloseWhileMRetained();
        // Preserve the existing direct-M synchronous-close and competing
        // consume witnesses; no timeout increase or test deletion.
        await BeamLoaderAdmissionRaceChecks.Run(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(807))));
    }

    private static async Task GuestLifetime(bool reserveFinish, bool invalidTail)
    {
        await using var runtime = new ProcessRuntime();
        byte[] expected = Enumerable.Range(0, LiteralBytes).Select(value => (byte)value).ToArray();
        byte[] bytes = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(expected)));
        await using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        ResourceReferenceTerm hostReference = runtime.ReferenceResources.CreatePrepared(image);
        object owner = Get(hostReference, Owner);
        Task settled = (Task)Get(owner, Settled);
        object code = Get(image, ModuleLiteral800Names.Code);
        var literals = (IReadOnlyList<Term>)Get(Get(code, ModuleLiteral800Names.Structure), ModuleLiteral800Names.Literals);
        BitString literal = (BitString)literals.Single();
        IDisposable? frame = null;
        object? heap = null;
        ResourceReferenceTerm? actual = null;
        object? plan = null;
        int slot = -1;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(context =>
            {
                heap = Get(context, Heap);
                frame = (IDisposable)Call(heap, Enter)!;
                slot = (int)Call(frame, Reserve)!;
                long before = HeapWords(heap);
                actual = (ResourceReferenceTerm)Call(
                    frame,
                    Adopt,
                    slot,
                    hostReference
                )!;
                Require(HeapWords(heap) - before == MagicWords);
                Require(actual.Words.SequenceEqual(hostReference.Words));
                ((IDisposable)Get(hostReference, Host)).Dispose();
                if (!reserveFinish)
                {
                    nint pointer = (nint)owner.GetType().GetField(Allocation, Members)!.GetValue(owner)!;
                    long beforeExport = Marshal.ReadInt64(pointer + RefcountOffsetWords * IntPtr.Size);
                    var exported = (ResourceReferenceTerm)Call(
                        heap,
                        Export,
                        frame,
                        actual
                    )!;
                    Require(Marshal.ReadInt64(pointer + RefcountOffsetWords * IntPtr.Size) == beforeExport + 1);
                    ((IDisposable)Get(exported, Host)).Dispose();
                    bool unsupported = false;
                    try
                    {
                        _ = Call(
                            heap,
                            Export,
                            frame,
                            Term.Tuple(actual, new UnknownNifGuest516Term())
                        );
                    }
                    catch (NotSupportedException)
                    {
                        unsupported = true;
                    }
                    Require(unsupported && Marshal.ReadInt64(pointer + RefcountOffsetWords * IntPtr.Size) == beforeExport);
                    GC.KeepAlive(exported);
                }
                if (reserveFinish)
                    plan = CapturePlan(runtime, invalidTail ? Term.List(actual, runtime.MakeReference()) : Term.List(actual));
                Call(frame, Clear, slot);
                Call(heap, Collect);
                Require(reserveFinish ? !settled.IsCompleted && (long)Get(owner, Bytes) > 0 : true);

                return Task.FromResult(true);
            });
            if (reserveFinish)
                await actor.InvokeAsync(async context =>
                {
                    bool badarg = false;
                    try
                    {
                        Term result = await (ValueTask<Term>)Call(plan!, RunMethod, context)!;
                        Require(!invalidTail && result.Equals(Term.A(Ok)) && image.IsConsumed);
                    }
                    catch (ErlangException error)
                    {
                        Require(invalidTail && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                        badarg = true;
                    }
                    Require(badarg == invalidTail);

                    return true;
                });
            await settled.WaitAsync(Timeout);
            Require((long)Get(owner, Bytes) == 0);
            if (reserveFinish && !invalidTail)
                Require(literal.ToArray().SequenceEqual(expected)); // Published code owns its actual arena.
            else
            {
                bool rejected = false;
                try
                {
                    _ = literal.ToArray();
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Require(rejected && !image.IsConsumed);
            }
            GC.KeepAlive(image);
            GC.KeepAlive(hostReference);
            GC.KeepAlive(actual);
            GC.KeepAlive(literal);
        }
        finally
        {
            (plan as IDisposable)?.Dispose();
            ((IDisposable)Get(hostReference, Host)).Dispose();
            frame?.Dispose();
        }
    }

    private static async Task BuilderCloseWhileMRetained()
    {
        await using var runtime = new ProcessRuntime();
        await using PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(807))));
        object grant = Call(runtime.Modules, IssueGrant, image)!;
        Type ownerType = typeof(ProcessRuntime).Assembly.GetType(NativeOwnerType, true)!;
        object owner = Activator.CreateInstance(
            ownerType,
            Members,
            null,
            [runtime.Modules, runtime, image, grant],
            null
        )!;
        object moduleGate = typeof(ModuleRegistry).GetField(Gate, Members)!.GetValue(runtime.Modules)!;
        Task cleanup;
        lock (moduleGate)
        {
            cleanup = Task.Run(() =>
            {
                try
                {
                    ((IDisposable)Call(
                        runtime.Modules,
                        CompleteRegistration,
                        grant,
                        owner
                    )!).Dispose();
                    throw new InvalidOperationException(Failure);
                }
                catch (ObjectDisposedException) { }
                finally
                {
                    Call(owner, AbortUnregistered);
                    Call(
                        runtime.Modules,
                        FinishBuilder,
                        grant,
                        null!
                    );
                }
            });
            Task[] physical = (Task[])Call(runtime.Modules, ClosePreparedOwners, true)!;
            Task.WhenAll(physical).WaitAsync(Timeout).GetAwaiter().GetResult();
            Require((long)Get(owner, Bytes) == 0);
            // The actual factory cleanup finished while metadata M was still
            // retained. Its delayed registration must never read freed cells.
            cleanup.WaitAsync(Timeout).GetAwaiter().GetResult();
        }
        await ((Task)Get(Get(grant, BuilderSettled), TaskProperty)).WaitAsync(Timeout);
        Require((int)Get(typeof(ModuleRegistry).GetField(PreparedOwners, Members)!.GetValue(runtime.Modules)!, Count) == 0);
        GC.KeepAlive(owner);
        GC.KeepAlive(image);
    }

    private static int OrdinaryAllocations(ProcessRuntime runtime)
    {
        object owner = typeof(ProcessRuntime).GetField(BinaryOwner, Members)!.GetValue(runtime)!;
        object gate = owner.GetType().GetField(Gate, Members)!.GetValue(owner)!;
        lock (gate)
        {
            object pending = owner.GetType().GetField(Pending, Members)!.GetValue(owner)!;

            return (int)Get(pending, Count);
        }
    }

    private static async Task PrivateNotPurged()
    {
        await using var runtime = new ProcessRuntime();
        byte[] first = Enumerable.Repeat((byte)1, LiteralBytes).ToArray();
        byte[] second = Enumerable.Repeat((byte)2, LiteralBytes).ToArray();
        byte[] third = Enumerable.Repeat((byte)3, LiteralBytes).ToArray();
        await using PreparedBeamImage one = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(first))));
        await using PreparedBeamImage two = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(second))));
        Require(runtime.Modules.PublishPreparedBeam(one).Status == BeamPublicationStatus.Published);
        Require(runtime.Modules.PublishPreparedBeam(two).Status == BeamPublicationStatus.Published);
        int baseline = OrdinaryAllocations(runtime);
        byte[] incoming = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(third)));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term result = await runtime.Modules.Call(
                context,
                PreparedLoader807Names.Erlang,
                Load,
                Term.A(one.Name),
                new BitString(incoming)
            );
            Require(result.Equals(Term.Tuple(Term.A(Error), Term.A(NotPurged))));
            Call(Get(context, Heap), Collect);

            return true;
        });
        Require(OrdinaryAllocations(runtime) == baseline && runtime.ReferenceResources.LiveResources == 0);
        object owners = typeof(ModuleRegistry).GetField(PreparedOwners, Members)!.GetValue(runtime.Modules)!;
        Require((int)Get(owners, Count) == 0);
        GC.KeepAlive(incoming);
    }
}
